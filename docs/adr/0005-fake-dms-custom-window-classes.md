# ADR-0005: FakeDms registers real custom window classes

- **Status:** Accepted
- **Date:** 2026-10-30
- **Deciders:** Liam Rowley

## Context

Autoline is unavailable outside work. The driver needs a target for development and CI. WinForms controls report their own class names (`WindowsForms10.BUTTON...`), so a plain WinForms app can't exercise class-name locators or custom message handling.

## Decision

`Polaris.FakeDms` registers custom window classes via `RegisterClassEx` and overrides `CreateParams.ClassName` on each control, so at the HWND level its controls are `KCMLButton32`, `KCMLEdit32`, `KCMLStatic32`, `KCMLGrid32` (names to be corrected from M1 captures). Each class has its own `WndProc`, so behaviour switches (ignore `BM_CLICK`, ignore `WM_SETTEXT`, validate on keystroke/focus-leave/save, stop pumping) are implemented by changing what the `WndProc` does. Screens are calibrated to probe captures so the same curated registry entries resolve against both the fake and the real app.

## Consequences

- The driver is tested against the same class names, message handling and failure modes it will meet in production.
- Every switch is a named test scenario; the fake doubles as executable documentation of what we know about Autoline.
- More P/Invoke in the fake than a plain WinForms app; acceptable, it reuses `Polaris.Native`.
- Fidelity is bounded by what the probe has captured; the fake must be updated when captures change.

## Alternatives considered

- **Plain WinForms with configurable class names in the driver** — leaves the real class strings untested and gives no control over message handling.
- **Native C/C++ fake** — maximal fidelity, but a second toolchain for the team; not justified.
