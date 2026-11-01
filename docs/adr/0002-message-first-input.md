# ADR-0002: Message-based actions by default, hardware input explicit

- **Status:** Accepted
- **Date:** 2026-10-30
- **Deciders:** Liam Rowley

## Context

Historically bots used hardware (SendInput-style) input, which depends on focus, mouse position and an unlocked foreground session, and forces one bot per machine. Most Autoline interactions do not need it. Some may (field validation triggered by keystrokes; controls that ignore `BM_CLICK`).

## Decision

Every action defaults to a `SendMessage`/`PostMessage` strategy targeting a specific HWND (`BM_CLICK`, `WM_SETTEXT`+`EN_CHANGE`+focus-leave, posted `WM_CHAR`, posted `WM_KEYDOWN`/`UP`). Hardware input exists only behind explicitly named methods (`ClickWithMouse`, `TypeWithKeyboard`) and as a declared fallback strategy in the registry. Input-based actions take a session-wide lock and emit an `InputBasedActionUsed` log event. All cross-process sends use `SendMessageTimeout`, never `SendMessage`.

## Consequences

- Bots stop depending on focus and mouse state; most flake from that class disappears.
- Multi-bot-per-desktop becomes possible later (cross-process lock is a contained change).
- Each control type needs its message sequence verified against real Autoline (M1), and the registry records which strategy a screen accepts.
- A hung Autoline cannot hang the bot: timeouts surface as `TargetUnresponsiveException`.

## Alternatives considered

- **Input-first (SendInput everywhere)** — faithful to a user but focus-dependent, serialises bots per desktop, and slower.
- **UIA Invoke/Value patterns** — depends on Autoline exposing them; deferred to ADR after M1.
