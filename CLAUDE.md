# Polaris

Windows-native desktop automation library replacing MacroScheduler for Vertu's RPA bots. Primary target: Autoline (Keyloop/Kerridge), a Win32 app with custom `KCML*` window classes.

## Read first

- `docs/SPEC.md` — what we're building and why. Architecture in §5, locators §6, strategies §7, registry §9.
- `docs/MILESTONES.md` — what's in scope *now*. Check which milestone is current before starting work; don't pull later work forward.
- `docs/CONVENTIONS.md` — what the tooling enforces and the ten judgement rules.
- `docs/adr/` — decisions that constrain you. Read 0001–0005 before touching `Native`, adding a dependency, or adding a strategy.
- `docs/runbooks/` — step-by-step procedures. If a task matches a runbook, follow the runbook.

## Layer rules (enforced by architecture tests — don't fight them)

```
Polaris.Autoline → Polaris.Driver → Polaris.Native
```
- `Native`: P/Invoke via CsWin32 only; no waits, no retries, no policy. Add APIs to `NativeMethods.txt`.
- `Driver`: knows nothing about Autoline. Everything auto-waits. Locators are serialisable data.
- `Autoline/Generated/`: **never edit** — regenerate from `registry/` with `dotnet build`. Hand code goes in `Autoline/Quirks/`.
- Bots (outside this repo) contain no locators and no waits.

## Hard rules (tooling will stop you; this is why)

- No `Thread.Sleep`; use `WaitUntil` with a condition.
- No raw `SendMessage` to another process; use the `SendMessageTimeout` wrappers in `Native`.
- Default actions are message-based. `ClickWithMouse` / `TypeWithKeyboard` are explicit fallbacks, chosen in the registry, never silently.
- Files ≤ 300 lines. Split before you hit it.
- New dependency, new native API, new strategy → ADR first (`docs/adr/0000-template.md`).
- Test names are scenarios: `Edit_SetValue_triggers_focusLeave_validation`.

## Working with the fake

`Polaris.FakeDms` is the development target. It registers real `KCML*` window classes and has behaviour switches (see `FakeDms/README.md`). If you learn something new about Autoline, encode it as a switch + test, not a comment.

## Commands

- Build + regenerate: `dotnet build`
- Fast tests: `dotnet test --filter Category!=Integration`
- All tests (needs a desktop): `dotnet test`
- Validate registry: `dotnet run --project src/Polaris.Registry -- validate`
- Probe (work only): `dotnet run --project src/Polaris.Probe -- --help`

## Unknowns

Until M1 is done, SPEC §16 lists what we don't know about Autoline's controls. Don't guess at those in code; stub with `NotSupportedException` and a reference to the question number.
