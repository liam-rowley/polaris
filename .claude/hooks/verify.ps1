# Stop: build, fast tests, file-size check. Exit 2 makes Claude keep working on the failure.
$root = $env:CLAUDE_PROJECT_DIR
if (-not $root) { $root = (Get-Location).Path }
Set-Location $root
$sln = Get-ChildItem -Path $root -Filter *.sln -File | Select-Object -First 1
if (-not $sln) { exit 0 }   # M0 not done yet

$out = & dotnet build $sln.FullName --nologo -clp:ErrorsOnly 2>&1
if ($LASTEXITCODE -ne 0) { [Console]::Error.WriteLine("Build failed:`n$($out -join "`n")"); exit 2 }

$out = & dotnet test $sln.FullName --no-build --nologo --filter "Category!=Integration" 2>&1
if ($LASTEXITCODE -ne 0) { [Console]::Error.WriteLine("Fast tests failed:`n$($out | Select-Object -Last 40 | Out-String)"); exit 2 }

$out = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$root/scripts/check-file-sizes.ps1" 2>&1
if ($LASTEXITCODE -ne 0) { [Console]::Error.WriteLine("$($out -join "`n")"); exit 2 }

$changed = & git -C $root status --porcelain -- '*/Generated/*'
if ($changed) { [Console]::Error.WriteLine("Generated files are stale or modified; run 'dotnet build' and commit the regenerated output:`n$changed"); exit 2 }
exit 0
