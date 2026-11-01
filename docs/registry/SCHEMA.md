# Registry schema reference

JSON Schema files are generated from the C# model types in `Polaris.Registry` (`polaris-registry schema --out registry/schema/`). This document is the human-readable reference; the schema files are the enforced contract.

## Observed tier — `observed.<version>.json`

```jsonc
{
  "schemaVersion": 1,
  "capturedAt": "2026-11-03T14:12:00Z",
  "capturedBy": "polaris-probe 0.1.0",
  "autolineVersion": "8.x.y",            // free text until we know the real versioning scheme
  "window": {
    "title": "Vehicle Stock - Edit [ABC123]",
    "className": "KCMLFrame32",
    "processName": "autoline.exe",
    "rect": { "left": 100, "top": 80, "width": 1200, "height": 800 },
    "style": 349110272, "exStyle": 256
  },
  "controls": [                            // flat, document order, index = position in array
    {
      "index": 0, "parentIndex": -1,       // -1 = direct child of window
      "className": "KCMLStatic32",
      "caption": "Stock No",
      "controlId": 0,
      "rect": { "left": 20, "top": 40, "width": 80, "height": 18 },   // window-relative
      "visible": true, "enabled": true,
      "style": 1342177280, "exStyle": 0,
      "msaa": { "role": "ROLE_SYSTEM_STATICTEXT", "name": "Stock No", "value": null }
    },
    {
      "index": 1, "parentIndex": -1,
      "className": "KCMLEdit32",
      "caption": "",
      "controlId": 0,
      "rect": { "left": 110, "top": 38, "width": 160, "height": 22 },
      "visible": true, "enabled": true,
      "style": 1342177408, "exStyle": 512,
      "msaa": { "role": "ROLE_SYSTEM_TEXT", "name": "Stock No", "value": "ABC123" }
    }
  ]
}
```

Rules: never hand-edited; one file per Autoline version; `controls` is the complete child tree (all depths), so indices are stable within a capture.

## Curated tier — `screen.json`

| Field | Required | Meaning |
|---|---|---|
| `id` | yes | `<area>.<screen>` kebab-case; unique across the registry |
| `description` | no | one line for humans |
| `identify.window` | yes | `WindowLocator` constraints: `titleMatches`, `className` |
| `identify.anchors[]` | yes | 1–3 locators that must all resolve; used to confirm "we are on this screen" |
| `controls` | yes | map of semantic name → control entry |
| `controls.<name>.type` | yes | `Button`, `Edit`, `Label`, `CheckBox`, `ComboBox`, `Grid` |
| `controls.<name>.locator` | yes | locator object (below) |
| `controls.<name>.write` | no | `setText` (default), `postChars`, `sendInput` — Edit only |
| `controls.<name>.click` | no | `bmClick` (default), `postMouse`, `sendInput` — Button only |
| `controls.<name>.readOnly` | no | generator omits setters |
| `controls.<name>.columns[]` | Grid | column names in display order |
| `actions` | no | map of action name → action entry |
| `actions.<name>.do` | yes | `{ control, action }` or `{ hotKey: "F10" }` |
| `actions.<name>.effects[]` | no | `{ control, becomes }` where `becomes` ∈ `nonEmpty`, `empty`, `enabled`, `disabled`, `visible`, `hidden`, `{ "equals": "x" }`, `{ "matches": "regex" }` |
| `actions.<name>.outcomes[]` | no | `{ name, window | screen | effects[], isFailure? }` — any-of; generated method returns the name |
| `actions.<name>.timeoutMs` | no | default from options |
| `navigation.from[]` | no | `{ screen, via }` — edges into this screen, used for `NavigateTo` graph search |
| `navigation.hotKeys` | no | map of action name → key chord, informational and used by `do.hotKey` |
| `quirks` | no | path to a partial-class file that must exist |
| `legacy` | no | `true` permits global `nth` locators; validator warns |

### Locator object

Any combination; `nth` applied last.

```jsonc
{ "caption": "Save" }
{ "captionMatches": "^Save" }
{ "labelledBy": "Stock No" }
{ "labelledBy": "Stock No", "direction": "right", "maxDistancePx": 200, "className": "KCMLEdit32" }
{ "className": "KCMLEdit32", "within": { "caption": "Vehicle Details" }, "nth": 2 }
{ "controlId": 1043 }
{ "className": "KCMLEdit32", "nth": 7 }          // requires "legacy": true on the screen
```

Resolution is a pure function; the same object is what the driver logs on failure.

## Interrupts — `interrupts.json`

```jsonc
{
  "interrupts": [
    { "id": "session-expired", "window": { "titleMatches": "^Session Expired" },
      "policy": "fail", "reason": "Hal must restart the session" },
    { "id": "record-locked", "window": { "titleMatches": "^Record in use" },
      "policy": "dismiss", "dismiss": { "control": { "caption": "OK" }, "action": "Click" },
      "thenThrow": "RecordLockedException" },
    { "id": "print-dialog", "window": { "className": "#32770", "titleMatches": "^Print" },
      "policy": "dismiss", "dismiss": { "control": { "caption": "Cancel" }, "action": "Click" } }
  ]
}
```

Policies: `fail` (throw `UnexpectedWindowException` immediately), `dismiss` (perform the action, then continue, or throw `thenThrow` if set), `ignore` (log and continue waiting).

## Validation rules (enforced by `polaris-registry validate`)

1. Schema-valid.
2. `id` unique; folder name equals `id`.
3. Every locator in `controls` and `identify.anchors` resolves to **exactly one** node in the newest `observed.*.json` for the screen.
4. Every `effects[].control`, `do.control`, `outcomes[].screen`, `navigation.from[].screen` references something declared.
5. `quirks` path exists if present.
6. `nth` without `within` only when `legacy: true` (warning, not error).
7. `write`/`click` strategies valid for the control type.
8. Grid `columns` present and non-empty.
9. Generated output for the screen matches committed `Generated/` (run by CI, not `validate`).
