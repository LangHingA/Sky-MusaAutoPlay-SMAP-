param(
    [Parameter(Mandatory=$true)][string[]]$Roots,
    [ValidateRange(0,5000000000)][long]$ReserveBytes = 0,
    [ValidateRange(1,5000000000)][long]$LimitBytes = 5000000000
)
$ErrorActionPreference = "Stop"
# Count overlapping roots only once. Conservatively include source bytes when source shares a scratch root.
$selected = @()
foreach ($root in ($Roots | ForEach-Object { [IO.Path]::GetFullPath($_).TrimEnd('\') } | Sort-Object Length)) {
    if (-not ($selected | Where-Object { $root.Equals($_, [StringComparison]::OrdinalIgnoreCase) -or $root.StartsWith($_ + '\', [StringComparison]::OrdinalIgnoreCase) })) { $selected += $root }
}
[long]$used = 0
$pending = [Collections.Generic.Stack[string]]::new()
foreach ($root in $selected) { if ([IO.Directory]::Exists($root)) { $pending.Push($root) } }
while ($pending.Count -gt 0) {
    $directory = $pending.Pop()
    Write-Verbose $directory
    if (([IO.File]::GetAttributes($directory) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Build storage must not hide data behind a reparse point: $directory" }
    foreach ($path in [IO.Directory]::EnumerateFileSystemEntries($directory)) {
        $attributes = [IO.File]::GetAttributes($path)
        if (($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Build storage contains a reparse point: $path" }
        if (($attributes -band [IO.FileAttributes]::Directory) -ne 0) { $pending.Push($path) }
        else {
            $used += ([IO.FileInfo]::new($path)).Length
            if ($used -gt $LimitBytes - $ReserveBytes) { throw "SMAP build storage budget exceeded during scan. Clean verified reproducible outputs before continuing." }
        }
    }
}
if ($used + $ReserveBytes -gt $LimitBytes) { throw "SMAP build storage budget exceeded: used=$used reserve=$ReserveBytes limit=$LimitBytes. Remove verified reproducible outputs before continuing; never remove songs or recovery data." }
[pscustomobject]@{ UsedBytes=$used; ReserveBytes=$ReserveBytes; LimitBytes=$LimitBytes; Roots=$selected }
