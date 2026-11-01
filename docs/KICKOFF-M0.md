# Kickoff prompt for M0 (paste into Claude Code in the repo root)

```
Read CLAUDE.md, docs/SPEC.md §5 and §15, docs/MILESTONES.md (M0 only), docs/CONVENTIONS.md, and docs/adr/0001–0005.

We are doing milestone M0 only. Do not create any Autoline-specific code, any driver logic, or any FakeDms screens — those are M1/M2.

Produce, in this order, committing after each step:

1. `Polaris.sln` with projects under `src/` (Polaris.Native, Polaris.Driver, Polaris.Registry, Polaris.Probe, Polaris.FakeDms) and `tests/` (Polaris.Driver.Tests). All `net10.0-windows`. Empty except for an `AssemblyInfo`-level placeholder so they compile.
2. `Directory.Build.props`: nullable, TreatWarningsAsErrors, NetAnalyzers (AnalysisLevel latest-all), StyleCop.Analyzers, BannedApiAnalyzers wired to `BannedSymbols.txt`, PublicApiAnalyzers on Native/Driver/Registry/Autoline, `AllowUnsafeBlocks` only in Polaris.Native. Honour `.editorconfig`.
3. CsWin32 in Polaris.Native with `NativeMethods.txt` containing exactly: FindWindowEx, EnumChildWindows, EnumWindows, GetClassName, GetWindowText, GetWindowTextLength, GetWindowRect, SendMessageTimeout, PostMessage, IsWindowVisible, IsWindowEnabled, GetDlgCtrlID, GetWindowThreadProcessId. One smoke test proving the generated code compiles and `EnumWindows` returns at least one window.
4. Architecture tests in tests/Polaris.Driver.Tests/Architecture using NetArchTest.Rules: Driver does not depend on Autoline; Native does not depend on Driver; nothing outside Native references the `Windows.Win32` namespace; test method names contain an underscore.
5. GitHub Actions workflow `.github/workflows/ci.yml` on windows-latest: dotnet format --verify-no-changes, build, test, scripts/check-file-sizes.ps1, scripts/check-adrs.ps1. Add one throwaway integration test (Category=Integration) that shows a WinForms Form and sends it a keystroke with SendInput and asserts the KeyDown fired — this proves the runner has an interactive desktop. If it fails on the hosted runner, record that in docs/adr/0006-ci-runner.md as a decision to use a self-hosted runner, and keep the test marked Integration.
6. Verify the hooks: edit a file under a throwaway `src/Polaris.Autoline/Generated/` folder and confirm the PreToolUse hook blocks it; confirm the Stop hook runs build + fast tests.

Exit criteria are in MILESTONES.md M0. When they are met, write a short summary of what exists, what was verified, and anything that deviated from the spec, in docs/m0-report.md.
```
