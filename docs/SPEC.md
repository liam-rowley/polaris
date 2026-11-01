# Polaris — Windows-native desktop automation library

**Status:** Draft v0.1 — 2026-10-30
**Owner:** Liam Rowley (Vertu Motors, RPA/automation team)
**Working name:** Polaris. Rename freely; the namespaces below assume `Polaris.*`.

---

## 1. Problem

The team runs ~50 production bots on a bespoke C# platform whose automation core is a set of wrappers over the **MacroScheduler** DLL. The DLL is unmanaged, so the wrappers include a harness for lifetime and garbage-collection management. The result is:

- Hard to maintain and extend — new capabilities mean more wrapper plumbing around a closed DLL.
- Inflexible — the DLL's command set defines what bots can do.
- Flaky — bots locate controls by **window class name + enumeration index** (`KCMLEdit32[7]`), which shifts whenever a screen gains a control or Autoline is patched.
- Timing-dependent — bots rely on `WaitWindowOpen`/`WaitWindowClosed` and sleeps rather than on observable outcomes.
- Input-dependent — historically bots used hardware (SendInput-style) input, forcing one bot per machine.

The primary automation target is **Autoline (Keyloop), a.k.a. Kerridge**: a Win32 client with custom window classes (`KCMLButton32`, `KCMLEdit32`, …). Autoline runs **locally** on each bot machine, in an **interactive session** identical to a user's. Bots are started, monitored and restarted by **Hal**, an in-house watchdog that also reports to the team website.

Autoline is not reachable outside work, so development must be possible against a **fake DMS**.

## 2. Goals

1. Replace MacroScheduler with a library built on **Windows-native APIs** (Win32 via managed P/Invoke; MSAA/UIA only if proven necessary). No unmanaged third-party DLLs, no lifetime harness.
2. **Robust locators** — controls are found by caption, by the label next to them, or by scoped hierarchy; global class+index is a logged last resort.
3. **Auto-waiting actions** — every action waits for actionability and for its declared outcome. Bots never sleep.
4. **Message-first input** — actions default to `SendMessage`/`PostMessage` to a specific HWND, so they don't depend on focus, mouse position, or a single bot per desktop. Hardware input is explicit and gated.
5. **An application registry** — a structured, versioned JSON description of every Autoline screen: its controls, the actions available, and what each action causes (transition to another screen, or effects on the same one). Page objects are *generated* from it; it is the single source of truth about Autoline.
6. **Built with frontier AI, maintainable by humans** — small layers with hard boundaries, deterministic tooling (formatters, analyzers, generators, hooks) instead of prose rules wherever a rule can be mechanised, and a documented workflow for adding a screen or a bot.
7. **Developable off-site** — a fake Autoline with the same window classes and configurable behaviours, so the driver can be developed and tested in CI without a licence.

## 3. Non-goals (v1)

- Migrating existing bots. Polaris is for **new bots only** initially. A compatibility facade is deferred (see §13).
- Web automation (Playwright stack owns it).
- OCR, image matching, pixel-based detection.
- Cross-platform. Windows-only TFM.
- UI Automation (UIA/FlaUI) unless M1 probing shows Win32 messaging is insufficient for a required control (expected candidate: grids).
- Office/Outlook/Excel automation.
- Process supervision, scheduling, restart policy, reporting to the website — **Hal** owns these. Polaris exposes attach/launch/health primitives Hal can call.
- Credential storage.

## 4. Constraints and assumptions

| Constraint | Consequence |
|---|---|
| Autoline runs locally, interactive session, same as a user | HWNDs are reachable; message-based actions are viable; SendInput is available as fallback |
| KCML buttons answer `PushButton` by caption today | Buttons have own HWNDs and respond to `WM_GETTEXT`; likely to `BM_CLICK` |
| `UIClick` works today | Some MSAA (`IAccessible`) support exists; kept in reserve |
| Grids appear as one control with cell coordinates (per MacroScheduler's window viewer) | Grid strategy is unknown; M1 must probe for hidden child HWNDs, MSAA table support, clipboard behaviour, and posted-mouse-message behaviour |
| Hot keys exist and are used but never required | Posted key messages are a first-class action; nothing depends on them |
| One bot per machine today, for historical reasons | Not a design constraint; message-first makes consolidation possible later |
| No access to Autoline off-site | Fake DMS is a first-class project; probe captures from work calibrate it |
| Team conventions: `IOptions`, DI, ADRs, behaviour-over-implementation tests | Adopt all four |

## 5. Architecture

Five projects, dependencies strictly downward. Nothing in a lower layer knows about a higher one.

```
Polaris.Bots.*            (bot projects — consumers, out of this repo)
        │
Polaris.Autoline          page objects GENERATED from the registry + hand-written quirks
        │
Polaris.Driver            Window / Control / Grid, Locator, waits, actions, diagnostics
        │
Polaris.Native            CsWin32-generated P/Invoke + thin typed helpers. No waiting, no retries.
```

Supporting projects:

```
Polaris.Registry          JSON schema, model types, validator, page-object generator (CLI)
Polaris.Probe             console tool: attach to a live app, dump control trees, try strategies
Polaris.FakeDms           WinForms app with KCML-style window classes and behaviour switches
Polaris.Driver.Tests      driver tests against FakeDms (CI)
Polaris.Autoline.Contract (work only) registry contract tests against real Autoline
```

### 5.1 `Polaris.Native`

- **CsWin32** (`Microsoft.Windows.CsWin32`) source generator for all P/Invoke. `NativeMethods.txt` lists exactly what's used. No hand-written `DllImport` except where CsWin32 cannot generate.
- Typed helpers only: `WindowHandle` (struct over `HWND`), `GetClassName`, `GetWindowText` (via `WM_GETTEXT`, with length negotiation), `GetWindowRect`, `IsWindowVisible`, `IsWindowEnabled`, `GetDlgCtrlID`, `EnumerateChildren(hwnd)` (returns a snapshot list, never a live enumeration), `EnumerateTopLevel(processId)`, `SendMessage`/`PostMessage` wrappers with timeouts (`SendMessageTimeout` — never raw `SendMessage` to a foreign process; a hung Autoline must not hang the bot), `SendInput` keyboard/mouse, `GetForegroundWindow`/`SetForegroundWindow`, screen capture of a window rect, clipboard read.
- Everything is synchronous and fails fast. The native layer is the only place that touches `unsafe` or raw handles. Target: small enough that one engineer can review every line.
- Optional (behind `Polaris.Native.Accessibility`, built only if M1 requires): `IAccessible` via `AccessibleObjectFromWindow`, and/or UIA3 COM interop.

### 5.2 `Polaris.Driver`

Public surface the rest of the system depends on. Everything here auto-waits.

**Session**
- `AutomationSession` — root object. Holds `Process` handle, options, logger, the diagnostics sink, and the session-wide input lock. `Attach(processId | processName)`, `Launch(path, args)`, `IsHealthy()` (process alive, main window responds to `WM_NULL` within timeout). Hal calls these.

**Windows**
- `WindowLocator` — `TitleMatches(regex)`, `ClassName`, `OwnedBy(window)`, `InProcess`. Resolved by enumeration.
- `Window` — `WaitUntilOpen`, `WaitUntilClosed`, `Children()`, `Find(Locator)`, `FindAll(Locator)`, `Snapshot()`, `Capture()`.

**Locators** (immutable, composable, serialisable — see §6)
- `Locator` with constraints: `Caption`, `CaptionMatches`, `LabelledBy`, `ClassName`, `ControlId`, `Within(parentLocator)`, `Nth(n)`, `Visible`, `Enabled`.
- Resolution is a pure function `Resolve(ControlTreeSnapshot, Locator) → ControlNode[]`. Testable without a window.

**Controls** — thin wrappers over an HWND + Locator, created lazily; re-resolved on every action (no stale handles).
- `Control` — `WaitUntil(condition)`, `Text`, `Bounds`, `IsVisible`, `IsEnabled`, `Click()` (message), `ClickWithMouse()` (input), `Focus()`.
- `Button : Control` — `Click()` = `BM_CLICK`; fallback posted `WM_LBUTTONDOWN/UP` at centre; fallback `ClickWithMouse()`.
- `Edit : Control` — `SetValue(text, WriteStrategy?)`, `Type(text)`, `TypeWithKeyboard(text)`, `Clear()`, `Value`.
- `Label : Control`.
- `CheckBox`, `ComboBox` — strategies determined in M1; stubs with `NotSupportedException` until then.
- `Grid : Control` — API fixed, implementation deferred to M3: `RowCount`, `Cell(row, col)`, `Row(n)`, `FindRow(predicate)`, `SelectRow(n)`, `ReadAll()`. Internally uses whichever strategy M1/M3 proves (see §8).

**Actions and outcomes**
- Every action accepts an optional `Expect` (an outcome): `WindowOpens(locator)`, `WindowCloses`, `ControlBecomes(locator, condition)`, `AnyOf(...)`. Action returns the outcome that occurred. Generated page objects supply `Expect` from the registry, so bot authors rarely do.
- `WaitUntil(Func<bool>, timeout, pollInterval)` is the single primitive under every wait. Polling, not events, in v1 (WinEvents hooks considered later as an optimisation).

**Write strategies** (per control type, overridable per registry entry — see §7)

**Input lock** — `ClickWithMouse`, `TypeWithKeyboard`, and `Focus` acquire a session-wide lock and record that input-based actions occurred. Logged at Info. This is the seam for a later cross-process lock.

**Interrupt handling** — a registry of known unexpected windows (session expired, record locked, print dialog) each with a policy: `Dismiss(action)`, `Fail`, `Ignore`. Checked on every wait timeout before throwing, and optionally on every poll.

**Diagnostics** — see §10.

### 5.3 `Polaris.Autoline`

- `Generated/` — page objects emitted by `Polaris.Registry` from the curated registry. **Never hand-edited.** Regenerated by a build step; CI fails if generated output is stale.
- `Quirks/` — partial classes for behaviour the registry can't express (odd message sequences, a control that needs `Type` instead of `SetValue`). Each quirk references a registry entry and an ADR or issue.
- `Interrupts/` — the Autoline interrupt registry (known popups).
- `AutolineSession : AutomationSession` — login flow, main-menu navigation (`NavigateTo(screenId)` via registry graph search).

### 5.4 `Polaris.Registry`

- `schema/` — JSON Schema for both tiers (§9).
- Model types + `System.Text.Json` source-generated serialisation.
- `polaris-registry validate` — schema + cross-reference checks.
- `polaris-registry generate` — emits page objects.
- `polaris-registry diff <observed-a> <observed-b>` — control tree diff for upgrades.

### 5.5 `Polaris.Probe`

Console app using only `Polaris.Native` + the snapshot model. Commands:

- `probe list` — top-level windows per process.
- `probe snapshot --window <title-regex> [--out file.json]` — observed-tier dump.
- `probe try-read <locator>` — `WM_GETTEXT`, MSAA name/value, clipboard-after-select; reports what returned.
- `probe try-write <locator> <text> --strategy <setText|postChars|sendInput>` then `probe snapshot` to see what changed.
- `probe try-click <locator> --strategy <bmClick|postMouse|sendInput>`.
- `probe watch --window <regex>` — polls and prints tree diffs, for discovering effects ("I typed a reg number; which fields changed?").

Its output format is the observed-tier registry JSON, so probe captures feed the registry directly.

### 5.6 `Polaris.FakeDms`

See §11.

## 6. Locator model

A `Locator` is an immutable record of constraints. Resolution: take a `ControlTreeSnapshot` (a flat list of `ControlNode` with parent indices, produced by one `EnumerateChildren` pass), filter by each constraint, apply `Nth` last.

```csharp
// Examples — all serialise to/from JSON so they can live in the registry and in logs
Locator.Caption("Save")                                  // WM_GETTEXT equals
Locator.CaptionMatches(@"^Save( Changes)?$")
Locator.LabelledBy("Stock No")                           // nearest KCML edit to the right/below the label
Locator.LabelledBy("Stock No", Direction.Right, maxDistancePx: 200)
Locator.ClassName("KCMLEdit32").Within(Locator.Caption("Vehicle Details")).Nth(2)
Locator.ControlId(1043)                                  // only if M1 shows KCML assigns stable IDs
Locator.ClassName("KCMLEdit32").Nth(7)                   // legacy; resolver logs a Warning
```

**`LabelledBy` algorithm (v1):** find label nodes whose caption matches; for each, find candidate controls of the requested class whose rect is to the right (or below) with vertical (or horizontal) overlap ≥ 50% of the label's height (width); pick nearest by edge distance; tie-break by document order. Parameters are tunable per registry entry. The algorithm is a pure function with its own tests.

**Failure reporting:** a failed resolution throws `ElementNotFoundException` carrying the locator JSON, the snapshot it resolved against, and the partial match counts per constraint ("Caption matched 0; after Within: n/a"). The diagnostics sink attaches a screenshot.

## 7. Actions and write strategies

Defaults, in order of preference. Each strategy is explicit, named, and selectable per control in the registry.

| Action | Default | Fallback 1 | Fallback 2 |
|---|---|---|---|
| Button click | `BM_CLICK` | `WM_LBUTTONDOWN`+`WM_LBUTTONUP` posted at centre | `SendInput` mouse click |
| Edit set | `WM_SETTEXT` + `EN_CHANGE` to parent + focus-leave (`WM_KILLFOCUS` or posted `VK_TAB`) | `WM_CHAR` per character (posted) | `SendInput` keystrokes |
| Key / hot key | `WM_KEYDOWN`/`WM_KEYUP` posted to target HWND | `SendInput` |
| Read text | `WM_GETTEXT` | MSAA `accValue`/`accName` | select + `WM_COPY` + clipboard |

**Why focus-leave matters:** Autoline is expected to validate fields on keystroke or field exit. `WM_SETTEXT` alone fills the buffer without firing those handlers. M1 determines, per screen, which strategy Autoline accepts; the registry records it; the generated page object uses it.

**Actionability** (checked before any action, with auto-wait): control exists, is visible, is enabled, is not covered by a window from the same process (point-hit test via `WindowFromPoint` on the centre), and — for input-based actions only — its top-level window is foreground.

## 8. Grids

Known: a grid presents as a single control; cells are addressed by coordinates. Unknown: whether cells have hidden HWNDs, whether MSAA exposes a table, whether keyboard navigation + copy yields cell text, whether posted mouse messages at cell coordinates select cells.

M1 probes all four. The `Grid` API (§5.2) is fixed now; the implementation picks the first strategy that works, in this order of preference:

1. Hidden per-cell/per-row HWNDs → ordinary locators.
2. MSAA table (`IAccessible` with `ROLE_SYSTEM_TABLE`/`CELL`) → read via `accValue`, select via `accSelect`.
3. Keyboard navigation (posted `VK_HOME`, `VK_DOWN`, …) + `WM_COPY` → parse clipboard; select via posted mouse messages at computed cell rects.
4. Posted mouse messages at coordinates only (select) with no read path → `Grid.ReadAll()` throws `NotSupportedException`; reading is pushed to a report/export elsewhere in Autoline.

Only if 1–3 all fail is UIA3 evaluated (ADR required). OCR remains a non-goal.

## 9. Application registry

Two tiers, different provenance, same folder: `registry/autoline/<screenId>/`.

### 9.1 Observed tier — `observed.<version>.json`

Machine-generated by `probe snapshot`. Overwritten on capture. **Never hand-edited.**

Per control: `index`, `parentIndex`, `className`, `caption`, `controlId`, `rect` (window-relative), `visible`, `enabled`, `style`, `exStyle`, optional `msaa { role, name, value }`. Plus window metadata: `title`, `className`, `processName`, `autolineVersion`, `capturedAt`.

### 9.2 Curated tier — `screen.json`

Hand- or AI-authored, reviewed like code.

```jsonc
{
  "id": "vehicle-stock.edit",
  "identify": {                       // how the driver knows it's on this screen
    "window": { "titleMatches": "^Vehicle Stock - Edit" },
    "anchors": [ { "caption": "Stock No" }, { "caption": "Save" } ]
  },
  "controls": {
    "StockNumber": { "type": "Edit", "locator": { "labelledBy": "Stock No" }, "write": "setText" },
    "RegNumber":   { "type": "Edit", "locator": { "labelledBy": "Reg No" },   "write": "postChars" },
    "Make":        { "type": "Edit", "locator": { "labelledBy": "Make" }, "readOnly": true },
    "Model":       { "type": "Edit", "locator": { "labelledBy": "Model" }, "readOnly": true },
    "Save":        { "type": "Button", "locator": { "caption": "Save" } },
    "Lines":       { "type": "Grid", "locator": { "className": "KCMLGrid32", "nth": 0 },
                     "columns": ["Line", "Description", "Qty", "Price"] }
  },
  "actions": {
    "EnterRegNumber": {
      "do":     { "control": "RegNumber", "action": "SetValue" },
      "effects": [ { "control": "Make", "becomes": "nonEmpty" },
                   { "control": "Model", "becomes": "nonEmpty" } ],
      "timeoutMs": 5000
    },
    "Save": {
      "do":      { "control": "Save", "action": "Click" },
      "outcomes": [
        { "name": "Saved",        "window": { "titleMatches": "^Vehicle Stock$" } },
        { "name": "ConfirmShown", "window": { "titleMatches": "^Confirm" } },
        { "name": "Validation",   "window": { "titleMatches": "^Error" }, "isFailure": true }
      ]
    }
  },
  "navigation": {
    "from": [ { "screen": "vehicle-stock.list", "via": "OpenSelected" } ],
    "hotKeys": { "Save": "F10" }
  },
  "quirks": "Quirks/VehicleStockEdit.cs"     // optional, must exist if named
}
```

**Generated page object** (sketch of what `polaris-registry generate` emits):

```csharp
public sealed partial class VehicleStockEdit : AutolineScreen
{
    public Edit StockNumber => Edit(Locator.LabelledBy("Stock No"), WriteStrategy.SetText);
    public Edit RegNumber   => Edit(Locator.LabelledBy("Reg No"),   WriteStrategy.PostChars);
    public Button Save      => Button(Locator.Caption("Save"));
    public Grid Lines       => Grid(Locator.ClassName("KCMLGrid32").Nth(0), columns: [...]);

    /// Sets RegNumber and waits for Make and Model to populate.
    public void EnterRegNumber(string value) =>
        Do(RegNumber.SetValue, value, Expect.All(Make.Becomes(NonEmpty), Model.Becomes(NonEmpty)), 5000);

    /// Clicks Save. Returns which outcome occurred; throws on Validation.
    public SaveOutcome Save() => Do(Save.Click, Expect.AnyOf(...));
}
```

### 9.3 Rules

- Page objects are generated. Hand code lives only in `Quirks/` partials.
- Every `action` in the curated tier has a **contract test** (work-only project) that performs it against a designated safe test record and asserts the declared outcome. The registry cannot lie for long.
- Every curated locator must resolve to exactly one node against the screen's current observed snapshot. `validate` enforces this offline.
- `validate` also enforces: every `effects`/`outcomes` reference names a declared control or screen; `quirks` file exists; no `nth` without `within` unless annotated `"legacy": true`.
- Registry is versioned by Autoline version. Upgrades: capture new observed tier, `diff`, fix curated entries that fail `validate`, re-run contract tests.

## 10. Diagnostics and error model

**Logging:** `Microsoft.Extensions.Logging` abstractions only; no sink in the library. Structured events with stable event IDs: `ActionStarted`, `ActionCompleted(outcome)`, `WaitTimedOut`, `LocatorResolved(count)`, `LegacyLocatorUsed`, `InputBasedActionUsed`, `InterruptHandled`. Hal/website integration is a sink the bot host registers — out of scope here, but the events are designed so that integration is a mapping, not a rewrite.

**Run artefacts:** an `IDiagnosticsSink` receives, on every failure, a bundle: screenshot of the target window (and desktop), observed snapshot of the window, the locator/action JSON, and the last N log events. Default sink writes to `%ProgramData%/Polaris/runs/<runId>/<timestamp>/`. Opt-in "trace" mode captures a bundle per action for debugging a new bot.

**Exceptions** (all derive from `PolarisException`, all carry the diagnostic bundle reference):

- `ElementNotFoundException` (locator, snapshot, per-constraint counts)
- `ActionTimeoutException` (action, expected outcome, observed state)
- `UnexpectedWindowException` (the window found, whether an interrupt policy matched)
- `ScreenMismatchException` (expected screen id, identification failures)
- `NotActionableException` (which actionability check failed)
- `TargetUnresponsiveException` (`SendMessageTimeout` elapsed — the app is hung)

Bots catch by type. Messages are for humans; fields are for code.

## 11. Fake DMS

`Polaris.FakeDms` is a WinForms app that:

- Registers **custom window classes** (`RegisterClassEx` + `CreateParams.ClassName` override) so its controls report `KCMLButton32`, `KCMLEdit32`, `KCMLStatic32`, `KCMLGrid32` (names adjusted to match probe captures once available). Each custom class owns its `WndProc`, so message handling is fully controllable.
- Ships the screens needed to exercise every driver feature: `Login`, `MainMenu`, `VehicleStockList` (grid), `VehicleStockEdit` (labelled edits, derived fields, Save with three outcomes), `ConfirmDialog` (modal), `SlowScreen` (configurable open delay), `InterruptPopup` (appears on a timer).
- Exposes **behaviour switches** via command line / a control-pipe so tests can configure: validation trigger (`keystroke | focusLeave | save`), whether buttons honour `BM_CLICK`, whether edits honour `WM_SETTEXT`, window open delay, grid strategy supported (`childHwnds | msaa | clipboardOnly | none`), interrupt popup timing, hung mode (stops pumping messages).
- Is **calibrated** from probe captures: once real observed snapshots exist, FakeDms's screens are adjusted so the same curated registry entries resolve against both. A test asserts this (`registry resolves against fake snapshot`).

Fidelity is not the goal; coverage of driver behaviours is. Every switch corresponds to a named test scenario.

## 12. Testing strategy

| Level | Project | Runs where | Against |
|---|---|---|---|
| Pure unit | `Driver.Tests` | anywhere | locator resolution, `LabelledBy` geometry, wait primitive, registry validation, generator output (golden files) |
| Driver integration | `Driver.Tests` | Windows CI (interactive desktop verified in M0) | `FakeDms`, one process per test class, behaviour switches per scenario |
| Registry contract | `Autoline.Contract` | work machine only, manual or nightly | real Autoline, safe test records |
| Generated-code freshness | CI | anywhere | `generate` output == committed `Generated/` |

Test naming states the scenario, not the method: `Edit_SetValue_triggers_focusLeave_validation`, `Button_Click_falls_back_to_posted_mouse_when_BM_CLICK_ignored`.

## 13. Migration

Out of scope for v1: new bots only. When the team is ready, a `Polaris.Compat` facade mirroring the existing wrapper API (`PushButton`, `WaitWindowOpen`, `GetObject(class, index)`) over the driver is the strangler path — existing bots swap a reference, index-based calls log warnings, and MacroScheduler can be removed before bots are rewritten. Not designed in detail now; noted so the driver's public API doesn't preclude it (it doesn't: every compat call maps to a locator + action).

## 14. Packaging and versioning

- .NET 10 (LTS), `net10.0-windows`, `<AllowUnsafeBlocks>` only in `Polaris.Native`.
- NuGet packages: `Polaris.Native`, `Polaris.Driver`, `Polaris.Registry` (incl. CLI as a `dotnet tool`), `Polaris.Autoline`. Internal feed (GitHub Packages under the VertuDevelopment org, or whatever the team uses).
- SemVer. `Polaris.Autoline` versions independently and carries the Autoline version it was captured against in package metadata; `Driver`/`Native` change rarely.
- `Polaris.Probe` and `Polaris.FakeDms` ship as tools/zip artefacts, not packages.

## 15. Engineering conventions (AI-maintainability)

Principle: **anything a machine can check, a machine checks.** Prose rules exist only for judgement calls. See `CONVENTIONS.md` for the full list; the mechanised set:

- `dotnet format` + `.editorconfig` (enforced; CI fails on diff).
- Analyzers: `Microsoft.CodeAnalysis.NetAnalyzers` at warning-as-error, `StyleCop.Analyzers` for ordering/doc comments, a banned-API list (`Microsoft.CodeAnalysis.BannedApiAnalyzers`) forbidding `Thread.Sleep`, raw `SendMessage` without timeout, `DllImport` outside `Polaris.Native`.
- Architecture tests (`NetArchTest` or `ArchUnitNET`): layer dependency direction; `Generated/` has no hand edits (checksum header); nothing outside `Native` references `Windows.Win32`.
- Public API baseline (`Microsoft.CodeAnalysis.PublicApiAnalyzers`): API changes are explicit diffs.
- File size cap: 300 lines per `.cs` (analyzer or script) — large files are the main thing that makes AI-written code unreviewable.
- Generated code regenerated in build; stale output fails CI.
- Claude Code hooks (`.claude/settings.json`): format on edit; build + fast tests on stop; block edits to `Generated/`.
- ADRs in `docs/adr/`, one per decision that constrains future work. Required for: adding a dependency, adding a strategy, touching `Native`.
- `CLAUDE.md` is short and points at the docs; it does not duplicate them.

## 16. Open questions (resolved by M1 probing)

1. Do KCML controls assign stable control IDs (`GetDlgCtrlID`)?
2. Which write strategy does each screen type accept (`setText` vs `postChars`)? Does Autoline validate on keystroke, focus-leave, or save?
3. Grid internals — any of the four strategies in §8?
4. Does `BM_CLICK` work on `KCMLButton32`, or do they need posted mouse messages?
5. Are Autoline screens top-level windows, owned popups, or MDI children? How are titles formatted?
6. What does MSAA expose (role/name/value) per control class?
7. Does Autoline stop pumping messages during long operations (affects `SendMessageTimeout` tuning)?
8. Does the GitHub-hosted Windows runner (or self-hosted) provide an interactive desktop sufficient for `SendInput` tests? (M0.)

## 17. Success criteria for v1

- Two new production bots (one form-driven, one grid-driven) run on Polaris under Hal for 30 days with no locator-related failures.
- Zero `Thread.Sleep` and zero global `class+index` locators in those bots.
- Registry contract tests pass against the current Autoline version; an Autoline patch produces a `diff` and a bounded fix.
- Driver test suite runs green in CI against FakeDms on every PR.
- A new engineer (or a fresh Claude Code session) can add a screen end-to-end by following `runbooks/add-a-screen.md` without asking a human.
