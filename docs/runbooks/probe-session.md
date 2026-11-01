# Runbook: probe session at work (M1)

Goal: leave with observed captures and answers to SPEC §16 questions 1–7. Budget an afternoon. Use a test company/branch and test records only.

## Before you go

- [ ] `Polaris.Probe` built as a single-file exe; copy to the bot machine.
- [ ] Inspect.exe or Accessibility Insights available on the machine (Windows SDK) for a second opinion on MSAA.
- [ ] Know which test records are safe to edit and how to reset them.

## Capture list (do all; each is a folder under `registry/autoline/`)

| Screen | What to capture | Why |
|---|---|---|
| Login | `snapshot` before and after entering credentials (redact values) | login flow, edit write strategy |
| Main menu | `snapshot`; `try-click` on a menu item with each strategy | navigation mechanism, top-level vs owned windows |
| A list screen with a grid | `snapshot`; grid-specific probes (below) | SPEC §8 |
| An edit screen with derived fields | `snapshot`; `watch` while typing a reg number by each write strategy | effects, validation trigger |
| A modal confirm | `snapshot`; `try-click` OK/Cancel | owned window handling |
| An error/validation popup | `snapshot` | interrupt registry |

## Per control class present (`KCMLButton32`, `KCMLEdit32`, `KCMLStatic32`, grid, anything else)

- [ ] `controlId` — is it 0, sequential, or stable across reopening the screen? Reopen the screen and `snapshot` again; diff.
- [ ] `try-read` — which of `WM_GETTEXT`, MSAA `accValue`, clipboard returns the text?
- [ ] `try-write` with `setText`, then `watch`: did derived fields update? Did a validation fire? Then `postChars`; then `sendInput`. Record which the screen accepted.
- [ ] `try-click` with `bmClick`, `postMouse`, `sendInput`. Record which worked.
- [ ] Hot keys: post `WM_KEYDOWN`/`UP` for F-keys to the main window and to the focused control; record which target works.

## Grid probes

- [ ] `snapshot` with `--include-hidden` — are there child HWNDs under the grid at all?
- [ ] Inspect.exe on a cell — role? `ROLE_SYSTEM_TABLE`/`CELL`? Does `accValue` give the cell text? Does `accSelect` select a row?
- [ ] Click a cell, post `WM_COPY` (and try `Ctrl+C` posted) — what lands on the clipboard? Whole row? Tab-separated?
- [ ] Posted `WM_LBUTTONDOWN/UP` at cell coordinates — does the selection move?
- [ ] Keyboard navigation posted (`VK_HOME`, `VK_DOWN`) — does the selection move?
- [ ] Scroll: `WM_VSCROLL` or posted `VK_NEXT`; does the HWND tree change when scrolling (virtualised rows)?

## Hung-app behaviour

- [ ] Trigger a long operation (a report); while it runs, `probe snapshot` — does `SendMessageTimeout` elapse? How long?

## Leaving

- [ ] All captures committed under `registry/autoline/<screen>/observed.<version>.json`.
- [ ] `probe-findings.md` written with an answer per §16 question, citing the capture file.
- [ ] Draft `docs/adr/0006-kcml-control-behaviour.md` from the findings.
