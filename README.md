# Polaris spec package

Drop the contents of this folder into a fresh repository root. Then open Claude Code in it and paste `docs/KICKOFF-M0.md`.

```
CLAUDE.md                      short context for Claude Code; points at docs
.claude/settings.json          hooks: block Generated/ edits, format on edit, verify on stop
.claude/hooks/*.ps1            the hook scripts (PowerShell; Claude Code runs them on Windows)
scripts/*.ps1                  mechanical checks shared by hooks and CI
docs/SPEC.md                   the specification
docs/MILESTONES.md             M0–M6 with exit criteria
docs/CONVENTIONS.md            what tooling enforces + ten judgement rules
docs/KICKOFF-M0.md             the prompt to start M0
docs/adr/                      ADR template + 0001–0005
docs/registry/                 registry README, schema reference, example JSON
docs/runbooks/                 probe-session, add-a-screen, add-a-bot, autoline-upgrade
src/Polaris.FakeDms/README.md  behaviour-switch reference (to be kept current)
BannedSymbols.txt              starter banned-API list
.editorconfig                  starter
```

Nothing in here is code yet; M0 produces the solution. The hooks are inert until a `.sln` exists.

Things to decide on day one that this package leaves open on purpose:
- The name (`Polaris` is a placeholder throughout — find/replace).
- .NET 10 vs 8 (spec says 10 LTS; change `Directory.Build.props` in M0 if the fleet isn't ready).
- Internal NuGet feed location.
- Whether CI runs on GitHub-hosted Windows runners or a self-hosted one — M0 proves this.
