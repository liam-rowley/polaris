# ADR-0004: No UI Automation (UIA/FlaUI) in v1 unless M1 proves it necessary

- **Status:** Accepted (revisit after M1)
- **Date:** 2026-10-30
- **Deciders:** Liam Rowley

## Context

Existing bots prove KCML buttons answer by caption and `UIClick` (MSAA) works for some clicks. Nothing proves Autoline implements UIA control patterns. UIA brings a second object model, threading constraints (STA/COM), and a dependency (FlaUI) or substantial interop.

## Decision

v1 uses Win32 messaging plus read-only MSAA (`IAccessible` name/role/value) for probing. UIA is evaluated only if M1/M3 show grids (or another required control) cannot be driven by strategies 1–3 in SPEC §8. That evaluation is its own ADR and, if adopted, UIA lives behind `Polaris.Native.Accessibility`.

## Consequences

- Smaller native surface, single threading model, no third-party automation dependency.
- If grids force UIA later, the `Grid` API is already fixed so the change is internal.

## Alternatives considered

- **FlaUI from day one** — well-maintained and would save interop work *if* Autoline exposes patterns; unknown until probed. Deferring costs little.
