# Conventions

Principle: **if a rule can be checked by a tool, it is checked by a tool and not written here.** This file lists what is mechanised (so you know what will stop you) and the small set of judgement rules that can't be.

## Mechanised — these will fail the build, the hook, or CI

| Rule | Enforced by |
|---|---|
| Formatting | `dotnet format` on every edit (hook) and in CI |
| Layer dependencies point downward only (`Autoline → Driver → Native`); nothing outside `Native` references `Windows.Win32.*` | Architecture tests (`Polaris.Driver.Tests/Architecture/`) |
| No `Thread.Sleep`, `Task.Delay` outside `WaitUntil`, raw `SendMessage` to foreign HWNDs | `BannedSymbols.txt` + BannedApiAnalyzers |
| No `DllImport` / `LibraryImport` outside `Native` | `BannedSymbols.OutsideNative.txt` (not applied to `Native`, which hosts CsWin32 output) |
| `unsafe` only in `Native` | `Directory.Build.targets` fails the build |
| Nullable enabled, warnings as errors, NetAnalyzers + StyleCop | `Directory.Build.props` |
| Public API changes are explicit | PublicApiAnalyzers (`PublicAPI.Shipped.txt` / `Unshipped.txt`) |
| `.cs` files ≤ 300 lines | `scripts/check-file-sizes.ps1` in CI and the Stop hook |
| `Generated/` is never hand-edited | Pre-edit hook blocks it; CI regenerates and fails on diff; checksum header |
| Curated registry entries validate (schema, cross-refs, unique resolution) | `polaris-registry validate` in build |
| Test names describe a scenario | Architecture test: test method names must contain `_` (`Subject_Action_Outcome`) |
| Every ADR has status, context, decision, consequences | `scripts/check-adrs.ps1` |

## Judgement rules — short, because the list above does the heavy lifting

1. **One concept per file, one reason to change per class.** If you're adding a second `switch` over control types, you probably need a strategy object.
2. **Locators are data.** Never build a locator from string concatenation at runtime; never hide one inside a helper method. If it isn't serialisable, it isn't a locator.
3. **Actions declare outcomes.** A method that clicks and returns `void` with no `Expect` is a smell unless the registry says the action has no observable effect.
4. **Quirks are quarantined.** Autoline-specific weirdness goes in `Polaris.Autoline/Quirks/`, references the registry entry it patches, and links an ADR or issue. The driver stays Autoline-agnostic.
5. **Native is boring.** No retries, no waits, no policy in `Polaris.Native`. It converts Win32 to typed .NET and throws on failure. Nothing else.
6. **Name after the user's intent at the Autoline layer** (`EnterRegNumber`, `OpenSelectedVehicle`), after the mechanism at the driver layer (`SetValue`, `ClickWithMouse`), after the API at the native layer (`SendMessageTimeout`).
7. **Prefer a failing test to a comment.** If you learn something about Autoline's behaviour, encode it as a FakeDms switch + test, or a contract test. Comments rot; tests fail.
8. **ADR before dependency.** Any new NuGet package, any new native API outside the approved list in `NativeMethods.txt`, any new strategy: ADR first.
9. **Bots contain no locators and no waits.** If a bot needs one, the registry or a page object is missing something.
10. **Write for the next reader.** Assume the next person to touch the file is a human who has never seen it and a model with no memory of this session. Both need the same thing: small files, honest names, and a test that shows the intended behaviour.

## Naming

- Projects: `Polaris.<Layer>`; tests: `Polaris.<Layer>.Tests`.
- Namespaces mirror folders.
- Registry screen ids: `<area>.<screen>` kebab-case (`vehicle-stock.edit`). Control names: PascalCase, after the label (`StockNumber`), never after the class.
- Strategies: enums with the mechanism in the name (`WriteStrategy.SetText`, `WriteStrategy.PostChars`, `WriteStrategy.SendInput`).
- Exceptions: `<What>Exception`, all derive from `PolarisException`.

## Commits and PRs

- One milestone task per PR. PR description links the milestone item and any ADR.
- Generated files are committed and regenerated in the same PR as the registry change that caused them.
- Probe captures are committed under `registry/autoline/<screen>/observed.<version>.json`; never squash two Autoline versions into one file.
