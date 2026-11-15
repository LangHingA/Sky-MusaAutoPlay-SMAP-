param([string]$OutputRoot = "", [string]$Dotnet = "C:\Program Files\dotnet\dotnet.exe",
    [string]$SigningKeyFile = "", [string]$SigningKeyId = "")
# Build SMAP installer. Run from the Installer folder (script is pure ASCII to avoid PS5.1 GBK parse issues).
$ErrorActionPreference = "Stop"
$inst = $PSScriptRoot
if (-not $inst -and $PSCommandPath) { $inst = Split-Path -Parent $PSCommandPath }
if (-not $inst) { $inst = (Get-Location).Path }
$repo       = Split-Path $inst                    # SMAP-WPF repo root (parent of Installer)
$smapCsproj = Join-Path $repo "src\SMAP.WPF\SMAP-WPF.csproj"
$buildRoot  = if ($OutputRoot) { [IO.Path]::GetFullPath($OutputRoot) } else { $inst }
$pubDir     = Join-Path $buildRoot "_smap_publish"
$payload    = Join-Path $buildRoot "Payload\app.zip"
$outDir     = Join-Path $buildRoot "_setup_out"
$dotnet     = $Dotnet
$SigningChannel = 'stable' # Installer release proofs use the dedicated Stable identity.
if (($SigningKeyFile -eq '') -ne ($SigningKeyId -eq '')) { throw 'SigningKeyFile and SigningKeyId must be supplied together.' }
if ($SigningKeyFile -and -not (Test-Path -LiteralPath $SigningKeyFile -PathType Leaf)) { throw 'Signing key file not found.' }
$budgetRoots = @($repo, $buildRoot)
if ($env:SMAP_BUILD_SCRATCH_ROOT) { $budgetRoots += $env:SMAP_BUILD_SCRATCH_ROOT }
function Check-BuildStorage([long]$reserve = 0) {
    & (Join-Path $repo "Tools\CheckBuildStorage.ps1") -Roots $budgetRoots -ReserveBytes $reserve | Out-Host
}
function Clear-BuildDirectory([string]$path, [string]$leaf) {
    $resolved = [IO.Path]::GetFullPath($path)
    $expected = [IO.Path]::GetFullPath((Join-Path $buildRoot $leaf))
    if ($resolved -ne $expected -or $resolved -eq [IO.Path]::GetPathRoot($resolved)) { throw "Unsafe build output path" }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}

Check-BuildStorage 900000000
Write-Host "[1/4] Publishing SMAP (self-contained win-x64)..." -ForegroundColor Cyan
Clear-BuildDirectory $pubDir "_smap_publish"
$mainArtifacts = Join-Path $buildRoot '_artifacts-main'
Clear-BuildDirectory $mainArtifacts '_artifacts-main'
& $dotnet publish $smapCsproj -c Release -r win-x64 --self-contained true --artifacts-path $mainArtifacts -o $pubDir /p:DebugType=none /p:DebugSymbols=false --nologo
if ($LASTEXITCODE -ne 0) { throw "SMAP publish failed" }
Clear-BuildDirectory $mainArtifacts '_artifacts-main'

Check-BuildStorage 250000000
Write-Host "[2/4] Publishing the update helper..." -ForegroundColor Cyan
$helperDir = Join-Path $buildRoot "_updater_publish"
Clear-BuildDirectory $helperDir "_updater_publish"
$helperArtifacts = Join-Path $buildRoot '_artifacts-updater'
Clear-BuildDirectory $helperArtifacts '_artifacts-updater'
& $dotnet publish (Join-Path $repo "Updater\SMAP.Updater.csproj") -c Release -r win-x64 --self-contained true --artifacts-path $helperArtifacts -o $helperDir /p:DebugType=none /p:DebugSymbols=false --nologo
if ($LASTEXITCODE -ne 0) { throw "Updater publish failed" }
Copy-Item -LiteralPath (Join-Path $helperDir "SMAP.Updater.exe") -Destination $pubDir -Force
Clear-BuildDirectory $helperArtifacts '_artifacts-updater'
Clear-BuildDirectory $helperDir '_updater_publish'

# Immutable samples are seeded into user storage on first use, never installed over personal songs.
$sampleDir = Join-Path $inst "sample-songs"
if (Test-Path $sampleDir) {
    $songsOut = Join-Path $pubDir "Assets\Samples"
    New-Item -ItemType Directory -Force $songsOut | Out-Null
    Copy-Item (Join-Path $sampleDir "*") $songsOut -Force
    "  {0} sample songs bundled" -f (Get-ChildItem $songsOut).Count | Write-Host
}

$publishedVersion = (Get-Item -LiteralPath (Join-Path $pubDir 'SMAP.exe')).VersionInfo.ProductVersion.Split('+')[0]
if ($SigningKeyFile) {
    $publishedBytes = (Get-ChildItem -LiteralPath $pubDir -File -Recurse | Measure-Object Length -Sum).Sum
    Check-BuildStorage ([long]$publishedBytes + 50000000)
    $packs = Join-Path $buildRoot '_update_packs'
    Clear-BuildDirectory $packs '_update_packs'
    & $dotnet run --project (Join-Path $repo 'Tools\UpdatePack\UpdatePack.csproj') -c Release -- $pubDir $packs $SigningChannel $publishedVersion $SigningKeyId $SigningKeyFile
    if ($LASTEXITCODE -ne 0) { throw 'Signed update inventory generation failed.' }
    $signedInventory = Join-Path $packs ("SMAP-{0}-manifest.json" -f $SigningChannel)
    & $dotnet run --project (Join-Path $repo 'Tools\ReleaseSigning\ReleaseSigning.csproj') -c Release -- verify-update $pubDir $signedInventory $SigningChannel ("v" + $publishedVersion)
    if ($LASTEXITCODE -ne 0) { throw 'Signed update inventory verification failed.' }
    Copy-Item -LiteralPath $signedInventory -Destination (Join-Path $pubDir '.smap-installed.json')
}
Check-BuildStorage 300000000
Write-Host "[3/4] Zipping into Payload\app.zip..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force (Split-Path $payload) | Out-Null
if (Test-Path $payload) { Remove-Item $payload -Force }
# Include the signed dot-file inventory, too.
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($pubDir, $payload, [IO.Compression.CompressionLevel]::Optimal, $false)
"{0:N1} MB payload" -f ((Get-Item $payload).Length / 1MB) | Write-Host

Check-BuildStorage ([long](Get-Item -LiteralPath $payload).Length * 3 + 300000000)
Write-Host "[4/4] Publishing installer (embed payload, single-file)..." -ForegroundColor Cyan
Clear-BuildDirectory $outDir "_setup_out"
$installerArtifacts = Join-Path $buildRoot '_artifacts-installer'
Clear-BuildDirectory $installerArtifacts '_artifacts-installer'
# WPF single-file MUST include native libs for self-extract, else DllNotFoundException at startup.
& $dotnet publish (Join-Path $inst "SMAP-Installer.csproj") -c Release -r win-x64 --self-contained true --artifacts-path $installerArtifacts -o $outDir /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=none /p:DebugSymbols=false "/p:PayloadPath=$payload" --nologo
if ($LASTEXITCODE -ne 0) { throw "Installer publish failed" }
Clear-BuildDirectory $installerArtifacts '_artifacts-installer'

$setup = Join-Path $outDir "SMAP-Setup.exe"
if ($SigningKeyFile) {
    $proof = Join-Path $outDir 'SMAP-installer-manifest.json'
    & $dotnet run --project (Join-Path $repo 'Tools\ReleaseSigning\ReleaseSigning.csproj') -c Release -- sign-installer $setup $publishedVersion $SigningKeyId $SigningKeyFile $proof
    if ($LASTEXITCODE -ne 0) { throw 'Installer proof generation failed.' }
    & $dotnet run --project (Join-Path $repo 'Tools\ReleaseSigning\ReleaseSigning.csproj') -c Release --no-build -- verify-installer $setup $publishedVersion $proof
    if ($LASTEXITCODE -ne 0) { throw 'Installer proof verification failed.' }
}
"{0:N1} MB installer" -f ((Get-Item $setup).Length / 1MB) | Write-Host
Check-BuildStorage
Write-Host "DONE: $setup" -ForegroundColor Green
