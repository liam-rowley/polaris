# PostToolUse: format the edited C# file. Non-blocking; failures are reported, not fatal.
$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$path = $payload.tool_input.file_path
if (-not $path -or -not ($path -match '\.cs$') -or -not (Test-Path $path)) { exit 0 }
$root = $env:CLAUDE_PROJECT_DIR
if (-not $root) { $root = (Get-Location).Path }
$sln = Get-ChildItem -Path $root -Filter *.sln -File | Select-Object -First 1
if (-not $sln) { exit 0 }
# dotnet format --include silently matches nothing for absolute paths, and Claude Code always sends absolute ones.
Set-Location $root
$relative = Resolve-Path -LiteralPath $path -Relative
# Whitespace only. Full `dotnet format` applies analyzer code fixes (e.g. IDE0051 deletes a private member that is
# declared in one edit and used in the next). Style and analyzer rules are enforced by the build and CI instead.
# --folder skips loading the MSBuild workspace, so this takes about a second.
& dotnet format whitespace --folder --include $relative 2>&1 | Out-Null
exit 0
