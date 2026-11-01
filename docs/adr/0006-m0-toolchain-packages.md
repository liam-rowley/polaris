# ADR-0006: M0 toolchain packages and test framework

- **Status:** Accepted
- **Date:** 2026-11-01
- **Deciders:** Liam Rowley

## Context

CONVENTIONS rule 8 requires an ADR before any NuGet package is added. M0 needs the packages SPEC §5.1 and §15 already name (CsWin32, StyleCop, banned-API and public-API analyzers, an architecture-test library), plus a test framework, which the spec does not name. Recording them together gives one place that says what the baseline is and why, so later ADRs only have to cover additions.

## Decision

The baseline packages are listed below. All versions are pinned once in `Directory.Packages.props` (central package management); analyzers that apply to every project are `GlobalPackageReference`s.

| Package | Where | Why |
|---|---|---|
| `Microsoft.Windows.CsWin32` | `Polaris.Native` | SPEC §5.1: all P/Invoke is generated from `NativeMethods.txt` |
| `StyleCop.Analyzers` | all projects | SPEC §15: ordering and doc-comment rules |
| `Microsoft.CodeAnalysis.BannedApiAnalyzers` | all projects | SPEC §15: enforces `BannedSymbols.txt` |
| `Microsoft.CodeAnalysis.PublicApiAnalyzers` | `Native`, `Driver`, `Registry`, `Autoline` | SPEC §15: public API changes are explicit diffs |
| `NetArchTest.Rules` | `Polaris.Driver.Tests` | SPEC §15: layer-direction tests |
| `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` | test projects | test framework (not named in the spec) |

`Microsoft.CodeAnalysis.NetAnalyzers` ships with the SDK and is turned on with `AnalysisLevel=latest-all`, so it is not a package reference.

The test framework is **xUnit v3**, run through the VSTest adapter so that `dotnet test --filter Category!=Integration` (hooks, CI) works with `[Trait("Category", "Integration")]`. xUnit v3 4.x defaults to Microsoft.Testing.Platform (MTP), and on the .NET 10 SDK MTP refuses to run under VSTest. Test projects therefore set `<IsTestingPlatformApplication>false</IsTestingPlatformApplication>`. Moving to MTP later means changing the filter syntax in `.claude/hooks/verify.ps1`, CI and `CLAUDE.md` together.

## Consequences

- Adding or upgrading a package is a one-line change in `Directory.Packages.props`. A *new* package still needs its own ADR.
- xUnit runs test classes in parallel by default. Desktop integration tests (M2) that share the foreground or FakeDms state must opt out through a non-parallel collection.
- xUnit has no built-in STA attribute. In-process WinForms tests create their own STA thread. M2 drives FakeDms as a separate process, so this only affects the throwaway M0 desktop test.
- StyleCop is used at its current prerelease (`1.3.x-beta`). The project has not shipped a stable release since 1.1.118, and the beta line is what the ecosystem uses.

## Alternatives considered

- **NUnit**: built-in `[Apartment(STA)]` and `[Category]` would suit UI tests slightly better, but most .NET teams know xUnit, and M2 runs its UI in a separate process anyway.
- **MSTest**: works, but has no advantage here and its attribute model is noisier.
- **ArchUnitNET instead of NetArchTest**: more expressive, but heavier; the rules we need are simple dependency checks.
- **Per-project package versions**: version drift between projects, and nowhere to review upgrades.
