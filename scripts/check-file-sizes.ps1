# Fails if any .cs file outside Generated/ or obj/ exceeds 300 lines.
param([int]$Max = 300)
$root = Split-Path -Parent $PSScriptRoot
$bad = Get-ChildItem -Path $root -Recurse -Filter *.cs -File |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin|Generated)[\\/]' } |
    Where-Object { (Get-Content $_.FullName | Measure-Object -Line).Lines -gt $Max }
if ($bad) {
    $bad | ForEach-Object { Write-Output ("{0} lines: {1}" -f (Get-Content $_.FullName | Measure-Object -Line).Lines, $_.FullName) }
    Write-Output "Files over $Max lines. Split them (docs/CONVENTIONS.md)."
    exit 1
}
exit 0
