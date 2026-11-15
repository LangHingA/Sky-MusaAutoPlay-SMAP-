param([Parameter(Mandatory=$true)][string]$ScratchRoot,[Parameter(Mandatory=$true)][string]$ResultPath)
$ErrorActionPreference = "Stop"
$guard = Join-Path (Split-Path $PSScriptRoot) "Tools\CheckBuildStorage.ps1"
$base = Join-Path ([IO.Path]::GetFullPath($ScratchRoot)) ("budget-test-" + [guid]::NewGuid().ToString('N'))
$nested = Join-Path $base 'nested'
New-Item -ItemType Directory -Path $nested | Out-Null
$a = Join-Path $base 'a'; $b = Join-Path $nested 'b'
[IO.File]::WriteAllBytes($a,[byte[]](1,2,3))
[IO.File]::WriteAllBytes($b,[byte[]](4,5))
$checks = @()
function Check([string]$name,[bool]$ok) { if(-not $ok){throw $name}; $script:checks += [pscustomobject]@{Name=$name;Status='PASS'} }
function Reject([scriptblock]$action) { try { & $action | Out-Null; return $false } catch { return $true } }
try {
    $result = & $guard -Roots @($base,$nested) -LimitBytes 5
    Check 'overlapping roots count once at exact boundary' ($result.UsedBytes -eq 5 -and $result.Roots.Count -eq 1)
    Check 'above limit fails closed' (Reject { & $guard -Roots $base -LimitBytes 4 })
    Check 'planned growth is reserved before work' (Reject { & $guard -Roots $base -LimitBytes 5 -ReserveBytes 1 })
    Check 'negative reservation is rejected' (Reject { & $guard -Roots $base -ReserveBytes -1 })
    Check 'five gigabyte cap cannot be raised' (Reject { & $guard -Roots $base -LimitBytes 5000000001 })
    Check 'guard never removes data' (([IO.File]::ReadAllBytes($a)).Length -eq 3 -and ([IO.File]::ReadAllBytes($b)).Length -eq 2)
    $checks | ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    Write-Output ("PASS {0}" -f $checks.Count)
} finally {
    # Remove only the two files created here; unexpected contents prevent directory removal.
    [IO.File]::Delete($a); [IO.File]::Delete($b)
    [IO.Directory]::Delete($nested); [IO.Directory]::Delete($base)
}
