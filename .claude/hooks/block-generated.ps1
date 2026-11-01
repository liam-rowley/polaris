# PreToolUse: refuse edits to generated files. Exit 2 blocks the tool call; stderr is shown to Claude.
$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$path = $payload.tool_input.file_path
if (-not $path) { exit 0 }
$norm = $path -replace '\\', '/'
if ($norm -match '/Generated/') {
    [Console]::Error.WriteLine("Blocked: '$path' is generated from registry/. Edit the registry entry and run 'dotnet build' to regenerate. See docs/runbooks/add-a-screen.md.")
    exit 2
}
exit 0
