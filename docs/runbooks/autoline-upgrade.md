# Runbook: Autoline version upgrade

1. On an upgraded test machine, re-capture every screen in the registry: `polaris-probe snapshot ...` → `observed.<newversion>.json` (keep the old file).
2. `polaris-registry diff registry/autoline/<screen>/observed.<old>.json registry/autoline/<screen>/observed.<new>.json` for each screen. Read the diffs; they tell you what moved, appeared or disappeared.
3. `polaris-registry validate`. Fix every curated entry it rejects (ambiguous or unresolved locators). Expect `labelledBy` entries to survive and `nth` entries to break.
4. Regenerate; review the generated diff — it should be empty unless control names changed.
5. Run the contract suite. Fix actions whose outcomes changed.
6. Bump `Polaris.Autoline` version; set the Autoline version in package metadata; release.
7. Record the elapsed time and what broke in `docs/upgrade-log.md`. If `nth` locators broke, replace them rather than re-numbering.
