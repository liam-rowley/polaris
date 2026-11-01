# Runbook: add an Autoline screen

Follow in order. Every step has a check. Suitable for a Claude Code session with no prior context.

1. **Capture** (work machine): `polaris-probe snapshot --window "<title regex>" --out registry/autoline/<area>.<screen>/observed.<version>.json`. Also save a screenshot next to it for reviewers. *Check:* file validates against `observed.schema.json`.
2. **Draft `screen.json`.** Start from `docs/registry/screen.example.json`. Name controls after their labels. For every button and every field whose change causes something, write an `action` with `effects` or `outcomes`. *Check:* `polaris-registry validate` passes — in particular every locator resolves to exactly one node.
3. **Prefer `labelledBy` and `caption` locators.** Use `within` + `nth` only inside a named container. Never a global `nth` without `"legacy": true` and a comment explaining why.
4. **Generate:** `dotnet build` regenerates `Polaris.Autoline/Generated/<Screen>.cs`. *Check:* `git status` shows exactly one generated file changed/added; do not edit it.
5. **Quirks (only if needed):** if a control needs behaviour the registry can't express, add `Quirks/<Screen>.cs` as a partial class, set `"quirks"` in `screen.json`, and link an ADR or issue in the file header.
6. **Fake calibration:** add or update the matching `Polaris.FakeDms` screen so `screen.json` also resolves against the fake's snapshot. *Check:* the calibration test for the screen passes.
7. **Contract tests** (work machine): one test per `action` in `Polaris.Autoline.Contract`, using the designated safe record. *Check:* all pass against the current Autoline version.
8. **Navigation:** add the `navigation.from` edges so `NavigateTo("<area>.<screen>")` can reach it. *Check:* the navigation test for the screen passes against the fake.
9. **PR:** capture, screenshot, `screen.json`, generated file, fake changes, tests. Title `registry: add <area>.<screen>`.
