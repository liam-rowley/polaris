# PostToolUse: format the edited C# file. Non-blocking; failures are reported, not fatal.
$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$path = $payload.tool_input.file_path
if (-not $path -or -not ($path -match '\.cs$') -or -not (Test-Path $path)) { exit 0 }
$root = $env:CLAUDE_PROJECT_DIR
if (-not $root) { $root = (Get-Location).Path }
$sln = Get-ChildItem -Path $root -Filter *.sln -File | Select-Object -First 1
if (-not $sln) { exit 0 }
& dotnet format $sln.FullName --include $path --no-restore 2>&1 | Out-Null
exit 0
