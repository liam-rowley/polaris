# M0 report: repo, toolchain, guardrails

- **Date:** 2026-11-01
- **Branch / PR:** `m0/toolchain`, [#1](https://github.com/liam-rowley/polaris/pull/1)
- **Brief:** `docs/KICKOFF-M0.md`; exit criteria in `docs/MILESTONES.md` M0

## What exists

| Area | Where |
|---|---|
| Solution | `Polaris.sln` (classic format; the hooks look for `*.sln`, and .NET 10 defaults to `.slnx`) |
| Projects | `src/Polaris.{Native,Driver,Registry,Probe,FakeDms}`, `tests/Polaris.Driver.Tests`. All `net10.0-windows`. Only `Driver → Native`, `Probe → Native`, `FakeDms → Native` references exist. |
| Build fence | `Directory.Build.props` / `.targets`, `Directory.Packages.props` (central versions), `stylecop.json`, `.gitattributes` (CRLF, matching `.editorconfig`), `global.json` |
| Banned APIs | `BannedSymbols.txt` (everywhere), `BannedSymbols.OutsideNative.txt` (`DllImport`, `LibraryImport`) |
| Public API baseline | `PublicAPI.{Shipped,Unshipped}.txt` in Native, Driver, Registry (Autoline is pre-wired by project name) |
| Native | CsWin32 + `NativeMethods.txt` (13 APIs), `WindowHandle`, `TopLevelWindows.Enumerate()` |
| Tests | Smoke test (`EnumWindows`), 4 architecture rules, 1 throwaway `Integration` desktop probe |
| CI | `.github/workflows/ci.yml` on `windows-latest`: restore, format verify, build, fast tests, integration tests, file sizes, ADRs |
| Decisions | ADR-0006: toolchain packages and test framework (xUnit v3 under VSTest) |

## What was verified, and how

Every fence was checked by planting a violation and watching it fail, not just by a clean build.

| Check | Planted violation | Result |
|---|---|---|
| Banned APIs | `Thread.Sleep`, `[DllImport]` in Driver | RS0030 errors |
| Raw `SendMessage` ban | Added `SendMessage` to `NativeMethods.txt` and called it | RS0030: the docID matches CsWin32's real signature |
| Public API baseline | Undocumented public type in Driver | RS0016 + CS1591 + SA1600 |
| `.editorconfig` in build | Block-scoped namespace | IDE0161 |
| `unsafe` confinement | `-p:AllowUnsafeBlocks=true` on Driver | Build error from `Directory.Build.targets` |
| Layer / `Windows.Win32` rule | A type in Driver depending on a `Windows.Win32.*` type | Architecture test fails, naming `Polaris.Driver.ArchLeak` |
| Test-name rule | `[Fact] NoUnderscores()` | Architecture test fails, naming the method |
| PreToolUse hook | Write to `src/Polaris.Autoline/Generated/HookProbe.cs` | Blocked; file never created |
| PostToolUse hook | LF-only `.cs` file plus an unused private member | Converted to CRLF; member kept; ~1.3 s |
| Stop hook | Clean tree / planted failing fast test | Exit 0 in ~4.5 s / exit 2 |
| Format check | LF files vs `end_of_line = crlf` | `--verify-no-changes` fails until formatted |

## SPEC §16 Q8: interactive desktop

**Answer: yes, the GitHub-hosted `windows-latest` runner works.** On [run 41323888590](https://github.com/liam-rowley/polaris/actions/runs/41323888590), the probe form became the foreground window, and a `SendInput` keystroke (via `SendKeys`) raised `KeyDown`: 1 passed in 227 ms. No self-hosted runner is needed, so the conditional `ci-runner` ADR from the kickoff was not written.

Caveats:
- One run proves the capability but not its stability. If integration tests turn flaky in M2, check foreground and session state before blaming the driver.
- A dev box is *harder* than CI here: the foreground lock usually stops the test window taking focus (see below).

## Deviations from the spec and kickoff

1. **Thirteen native APIs, not ten.** MILESTONES lists ten; KICKOFF lists thirteen "exactly" (adds `EnumWindows`, `GetWindowTextLength`, `GetWindowThreadProcessId`). The kickoff is more specific and the smoke test needs `EnumWindows`, so `NativeMethods.txt` has the thirteen.
2. **Native has a tiny public API in M0.** The smoke test can't call CsWin32 from the test project: the generated types are internal, and the architecture rule bans `Windows.Win32` outside Native. So Native exposes `WindowHandle` and `TopLevelWindows.Enumerate()`, both named in SPEC §5.1, as the smallest real seam. M1 builds the rest of §5.1 on top.
3. **Banned list split in two.** CsWin32 emits `[DllImport]`, so a global ban breaks Native. CONVENTIONS always said "`DllImport` *outside* `Native`", and the split makes the tool match the rule. `LibraryImport` was added because it is the same loophole.
4. **xUnit v3 runs under VSTest, not Microsoft.Testing.Platform.** xUnit v3 4.x defaults to MTP, which on the .NET 10 SDK refuses `dotnet test`'s VSTest path and changes the filter syntax. Opting out kept every documented command (`--filter Category!=Integration`) unchanged. ADR-0006 says how to move later.
5. **ADR numbering.** The toolchain ADR took 0006. The CI-runner ADR wasn't needed, and MILESTONES M1 now names `0007-kcml-control-behaviour.md`.
6. **The desktop probe uses `SendKeys`, not a direct `SendInput` call.** P/Invoke is banned outside Native, and `SendInput` isn't on the approved API list yet. WinForms `SendKeys` is implemented with `SendInput` on modern .NET, so it proves the same thing. The probe also refuses to send input unless its window is foreground (see below).
7. **Implicit usings are off.** Explicit `using`s keep each file's dependencies visible to a reviewer. This is a style choice, not a spec item.
8. **StyleCop documents exposed elements only** (`documentInternalElements: false`), and usings go outside the namespace. Tests relax a short, commented list of rules in `tests/.editorconfig`.

## Problems found in the supplied tooling (fixed)

- **The format-on-edit hook never formatted anything.** `dotnet format --include` silently matches nothing for absolute paths, and Claude Code always sends absolute paths. Fixed by resolving a repo-relative path.
- **Once working, the format hook deleted code.** Full `dotnet format` applies analyzer code fixes, so IDE0051 removed a private `const` that was declared in one edit and used in the next. The hook now runs `dotnet format whitespace --folder` only. Style and analyzer rules are still enforced by the build and CI.
- **The throwaway probe typed into the developer's foreground window** during development. On a dev box the foreground lock stops the test window taking focus, so the first versions' keystrokes landed in whatever had focus. The probe now checks `Form.ActiveForm` (backed by `GetForegroundWindow`) and sends nothing unless it's in front. **Lesson for M2:** any input-based test must assert foreground before sending input.

## Notes for M1

- `dotnet format` prints "Warnings were encountered while loading the workspace" but exits 0. Diagnostic verbosity shows no warning detail, so it is treated as benign.
- On a dev box, input-based integration tests fail fast with "could not become foreground" unless the test window can take focus. Message-based tests (the default per ADR-0002) are unaffected.
