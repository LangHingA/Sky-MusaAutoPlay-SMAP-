param([string]$OutputRoot = "", [string]$Dotnet = "C:\Program Files\dotnet\dotnet.exe")
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
function Clear-BuildDirectory([string]$path, [string]$leaf) {
    $resolved = [IO.Path]::GetFullPath($path)
    $expected = [IO.Path]::GetFullPath((Join-Path $buildRoot $leaf))
    if ($resolved -ne $expected -or $resolved -eq [IO.Path]::GetPathRoot($resolved)) { throw "Unsafe build output path" }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}

Write-Host "[1/4] Publishing SMAP (self-contained win-x64)..." -ForegroundColor Cyan
Clear-BuildDirectory $pubDir "_smap_publish"
& $dotnet publish $smapCsproj -c Release -r win-x64 --self-contained true -o $pubDir /p:DebugType=none /p:DebugSymbols=false --nologo
if ($LASTEXITCODE -ne 0) { throw "SMAP publish failed" }

Write-Host "[2/4] Publishing the update helper..." -ForegroundColor Cyan
$helperDir = Join-Path $buildRoot "_updater_publish"
Clear-BuildDirectory $helperDir "_updater_publish"
& $dotnet publish (Join-Path $repo "Updater\SMAP.Updater.csproj") -c Release -r win-x64 --self-contained true -o $helperDir /p:DebugType=none /p:DebugSymbols=false --nologo
if ($LASTEXITCODE -ne 0) { throw "Updater publish failed" }
Copy-Item -LiteralPath (Join-Path $helperDir "SMAP.Updater.exe") -Destination $pubDir -Force

# Immutable samples are seeded into user storage on first use, never installed over personal songs.
$sampleDir = Join-Path $inst "sample-songs"
if (Test-Path $sampleDir) {
    $songsOut = Join-Path $pubDir "Assets\Samples"
    New-Item -ItemType Directory -Force $songsOut | Out-Null
    Copy-Item (Join-Path $sampleDir "*") $songsOut -Force
    "  {0} sample songs bundled" -f (Get-ChildItem $songsOut).Count | Write-Host
}

Write-Host "[3/4] Zipping into Payload\app.zip..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force (Split-Path $payload) | Out-Null
if (Test-Path $payload) { Remove-Item $payload -Force }
Compress-Archive -Path (Join-Path $pubDir "*") -DestinationPath $payload -CompressionLevel Optimal
"{0:N1} MB payload" -f ((Get-Item $payload).Length / 1MB) | Write-Host

Write-Host "[4/4] Publishing installer (embed payload, single-file)..." -ForegroundColor Cyan
Clear-BuildDirectory $outDir "_setup_out"
# WPF single-file MUST include native libs for self-extract, else DllNotFoundException at startup.
& $dotnet publish (Join-Path $inst "SMAP-Installer.csproj") -c Release -r win-x64 --self-contained true -o $outDir /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=none /p:DebugSymbols=false "/p:PayloadPath=$payload" --nologo
if ($LASTEXITCODE -ne 0) { throw "Installer publish failed" }

$setup = Join-Path $outDir "SMAP-Setup.exe"
"{0:N1} MB installer" -f ((Get-Item $setup).Length / 1MB) | Write-Host
Write-Host "DONE: $setup" -ForegroundColor Green
