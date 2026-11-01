# Polaris — Milestones

Each milestone has an **exit criterion** that is mechanically checkable where possible. Milestones are ordered so that the single biggest unknown (how KCML controls actually behave) is resolved before the driver's defaults are written down. Estimates assume one engineer with Claude Code, part-time alongside other work; treat them as relative sizes.

Milestones that need a work machine with Autoline are marked **[work]**. Everything else can be done anywhere.

---

## M0 — Repo, toolchain, guardrails  (~2–3 days)

Goal: an empty but fully fenced repository. Nothing here is Autoline-specific.

Deliverables:
- Solution with `Polaris.Native`, `Polaris.Driver`, `Polaris.Registry`, `Polaris.Probe`, `Polaris.FakeDms`, `Polaris.Driver.Tests` — all compiling, all empty.
- `Directory.Build.props`: `net10.0-windows`, nullable, warnings-as-errors, analyzers (§15 of SPEC), `.editorconfig`, banned-API list.
- Architecture tests asserting layer direction (will be trivially true; they exist so they're never forgotten).
- CsWin32 wired into `Native` with a `NativeMethods.txt` containing the first ten APIs (`FindWindowEx`, `EnumChildWindows`, `GetClassName`, `GetWindowText`, `GetWindowRect`, `SendMessageTimeout`, `PostMessage`, `IsWindowVisible`, `IsWindowEnabled`, `GetDlgCtrlID`).
- CI (GitHub Actions, Windows runner): format check, build, test. **Prove** the runner can show a WinForms window and that `SendInput` reaches it (a throwaway test). If not, document the self-hosted runner requirement now.
- `CLAUDE.md`, `.claude/settings.json` hooks (format on edit, build+test on stop, block edits under `Generated/`), ADR template, `CONVENTIONS.md`, runbook skeletons.

Exit: CI green on a PR that adds one trivial test; the Stop hook runs `dotnet build` and the fast test set locally; an edit to a file under `Generated/` is blocked by the hook.

---

## M1 — Probe + first real captures  **[work]**  (~3–4 days build, then an afternoon at work)

Goal: replace guesses about KCML with facts.

Deliverables:
- `Polaris.Native` helpers needed by the probe (§5.1) — snapshot enumeration, text, rect, class, control ID, send/post with timeout, MSAA `IAccessible` read-only (`accName`, `accRole`, `accValue`) via `AccessibleObjectFromWindow`.
- `Polaris.Probe` commands: `list`, `snapshot`, `try-read`, `try-write`, `try-click`, `watch` (§5.5). Output = observed-tier JSON (§9.1), schema committed in `Polaris.Registry`.
- A **probe session checklist** (`runbooks/probe-session.md`) listing exactly what to capture at work: login, main menu, one list/grid screen, one edit screen, one modal confirm, one error popup; for each, snapshot + `try-*` results for every control class present.
- Captures committed under `registry/autoline/<screen>/observed.<version>.json` with a `probe-findings.md` answering the open questions in SPEC §16 (1–7).

Exit: every open question in SPEC §16 items 1–7 has a recorded answer backed by a capture file. Findings written as an ADR (`0006-kcml-control-behaviour.md`) that fixes the default strategies in §7 and the grid strategy in §8.

---

## M2 — Driver core against FakeDms  (~2 weeks)

Goal: the driver works end-to-end on the fake, with the defaults M1 established.

Deliverables:
- `Polaris.FakeDms` with custom window classes, `Login`, `MainMenu`, `VehicleStockEdit`, `ConfirmDialog`, `SlowScreen`, `InterruptPopup`; behaviour switches for validation trigger, `BM_CLICK` honoured, `WM_SETTEXT` honoured, open delay, interrupt timing, hung mode. Calibrated to M1 captures (class names, title formats, control layout).
- `Polaris.Driver`: `AutomationSession`, `WindowLocator`/`Window`, `Locator` + pure resolver + `LabelledBy` geometry, `Control`/`Button`/`Edit`/`Label`, all write strategies and fallbacks, actionability checks, `WaitUntil`, `Expect` outcomes, input lock, interrupt registry, exceptions, diagnostics sink with failure bundles.
- `Polaris.Driver.Tests`: pure unit tests for resolver/geometry/wait; integration tests per behaviour switch scenario.

Exit: a scripted "bot" in the test project logs in to FakeDms, opens `VehicleStockEdit`, enters a reg number, waits for derived fields, saves, and handles all three Save outcomes — under every combination of the validation-trigger and `BM_CLICK` switches — with no sleeps. Failure bundles are produced for a deliberately failing test and contain screenshot + snapshot + locator.

---

## M3 — Grids  **[work for verification]**  (~1–2 weeks, highly uncertain)

Goal: `Grid` works on the strategy M1 proved.

Deliverables:
- `FakeDms` grid control supporting the strategies the real one supports (and switches to disable each, so fallbacks are tested).
- `Grid` implementation (§8) with `RowCount`, `Cell`, `Row`, `FindRow`, `SelectRow`, `ReadAll`.
- If M1 showed none of strategies 1–3 work: ADR evaluating UIA3; implement behind `Polaris.Native.Accessibility`; otherwise ADR records "not needed".

Exit: `VehicleStockList` scenario — find a row by stock number, select it, open it — passes against FakeDms; verified once against real Autoline at work with the probe.

---

## M4 — Registry, validator, generator  (~1 week)

Goal: page objects come from JSON.

Deliverables:
- Curated-tier schema (§9.2) and model types.
- `polaris-registry validate` (all rules in §9.3, including "each locator resolves to exactly one node in the observed snapshot").
- `polaris-registry generate` → `Polaris.Autoline/Generated/*.cs` with checksum headers; build target regenerates; CI fails on stale output; golden-file tests for the generator.
- `polaris-registry diff` for observed snapshots.
- `Polaris.Autoline` project with `AutolineSession`, `NavigateTo(screenId)` via graph search over `navigation`, `Interrupts/` registry, `Quirks/` convention.
- Curated entries for the screens captured in M1, generated page objects, and the same entries resolving against FakeDms snapshots (calibration test).

Exit: the M2 scripted bot is rewritten to use only generated page objects and `NavigateTo`; it is shorter and contains no locators. `validate` fails loudly when a curated locator is made ambiguous.

---

## M5 — Registry contract tests + first real bot  **[work]**  (~1–2 weeks)

Goal: Polaris drives real Autoline under Hal.

Deliverables:
- `Polaris.Autoline.Contract` test project (work only): one test per registry action, against designated safe test records; a runbook for running it and for choosing/reset of test data.
- Hal integration: Hal launches/attaches via `AutolineSession`, polls `IsHealthy()`, and registers an `ILogger` sink + `IDiagnosticsSink` that forwards failure bundles to wherever the website expects. (Hal-side work; Polaris exposes only interfaces.)
- First new production bot, form-driven, built *only* from the runbook `add-a-bot.md`. Record every point where the runbook was insufficient and fix the runbook, not the bot.

Exit: bot runs in production under Hal for two weeks; failures (if any) all produce usable diagnostic bundles; contract suite green against current Autoline version.

---

## M6 — Second bot (grid-driven), hardening, v1.0  (~2 weeks)

Deliverables:
- Grid-driven production bot.
- Upgrade drill: simulate an Autoline patch by changing a FakeDms screen; follow `runbooks/autoline-upgrade.md` (capture → diff → fix curated → validate → contract tests); time it and record.
- Performance pass: snapshot enumeration cost on large screens, poll intervals, `SendMessageTimeout` tuning from M1 findings.
- Public API baseline frozen; packages published to the internal feed at 1.0.0.

Exit: SPEC §17 success criteria met.

---

## Deferred (not scheduled)

- `Polaris.Compat` facade and migration of existing bots.
- Cross-process input lock for multiple bots per desktop.
- WinEvent-hook based waiting (replace polling).
- UIA3 beyond grids, if ever.
- OCR/image fallback — only with a specific bot that needs it and an ADR.
