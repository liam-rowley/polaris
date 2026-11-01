# Fails if any ADR (other than the template) lacks the required sections.
$root = Split-Path -Parent $PSScriptRoot
$required = @('**Status:**', '## Context', '## Decision', '## Consequences', '## Alternatives considered')
$fail = $false
Get-ChildItem "$root/docs/adr" -Filter *.md | Where-Object { $_.Name -ne '0000-template.md' } | ForEach-Object {
    $text = Get-Content $_.FullName -Raw
    foreach ($r in $required) {
        if (-not $text.Contains($r)) { Write-Output "$($_.Name): missing '$r'"; $fail = $true }
    }
}
if ($fail) { exit 1 } else { exit 0 }
