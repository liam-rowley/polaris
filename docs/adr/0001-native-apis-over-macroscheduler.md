# ADR-0001: Build on Windows-native APIs instead of MacroScheduler

- **Status:** Accepted
- **Date:** 2026-10-30
- **Deciders:** Liam Rowley

## Context

The current platform wraps the unmanaged MacroScheduler DLL in C#, requiring a lifetime/GC harness. The DLL's command set bounds what bots can do; locating controls by class+index is flaky; the harness is hard to maintain. Autoline runs locally in an interactive session, so Win32 HWNDs are directly reachable.

## Decision

Replace MacroScheduler with an in-house library on Win32 (via CsWin32-generated P/Invoke) with MSAA/UIA held in reserve. No unmanaged third-party automation DLLs.

## Consequences

- We own the whole stack; every capability is ours to add and ours to maintain.
- The native layer must stay small and reviewed; `unsafe` is confined to `Polaris.Native`.
- No vendor support; probing Autoline's control behaviour is our job (M1).
- Lifetime management becomes ordinary .NET (SafeHandles, COM RCWs) — the harness goes away.

## Alternatives considered

- **Keep MacroScheduler, improve the wrapper** — leaves the root problems (closed DLL, unmanaged lifetime) in place.
- **FlaUI / UIA-first** — adds a dependency and a whole second object model before we know Autoline exposes UIA patterns; Win32 messaging is already proven to work for buttons and text via the existing `PushButton`/`UIClick` usage. Revisit for grids (ADR after M1).
- **WinAppDriver / Appium** — Microsoft's project is effectively dormant; adds a server process and WebDriver protocol for no gain in an in-process library.
- **pywinauto** — Python; the team's stack is .NET.
