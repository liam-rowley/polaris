# ADR-0003: Application registry is the source of truth; page objects are generated

- **Status:** Accepted
- **Date:** 2026-10-30
- **Deciders:** Liam Rowley

## Context

The team wants a structured JSON description of each Autoline window: its controls, available actions, and what each action causes (navigation to another screen; derived fields populating). The risk with any such registry is drift: if page objects are also hand-written, there are two descriptions of Autoline and one rots.

## Decision

Page objects in `Polaris.Autoline/Generated/` are generated from the curated registry and never hand-edited (hook-blocked, CI-checked). The registry has two tiers: an observed tier produced by the probe (facts about HWNDs, overwritten on capture) and a curated tier authored by people/models and validated against the observed tier (every locator must resolve to exactly one node). Hand-written code is confined to `Quirks/` partial classes that reference the entry they patch. Every curated action has a contract test against real Autoline.

## Consequences

- Adding a screen is: capture → curate → validate → generate → contract test. Mechanical, documented in a runbook, suitable for a model to execute.
- Autoline upgrades become: capture → diff → fix curated entries the validator rejects → re-run contract tests.
- Bots gain `NavigateTo(screenId)` via graph search over `navigation` edges.
- The generator is a real piece of software with golden-file tests; it must be kept simple (templates, no cleverness).
- Effects/outcomes in the registry are what let actions auto-wait on the right condition; bots contain no waits.

## Alternatives considered

- **Hand-written page objects, registry as documentation** — guaranteed drift.
- **Registry consumed at runtime (no generation)** — loses compile-time names and IDE discoverability, which matter for human maintainers; harder to review a bot.
- **Record-and-replay tooling** — produces brittle, index-based locators; the opposite of the goal.
