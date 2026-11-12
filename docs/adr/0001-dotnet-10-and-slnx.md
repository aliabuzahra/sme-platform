# 0001. .NET 10 and the `slnx` solution format

**Status:** Accepted
**Date:** 2026-11-12
**Card:** UC-001 · **SAD:** §9
**Owner:** علي

## Context

The skeleton needed a target framework and a solution file. .NET 8 reached end of support; .NET 10 is the current LTS. The .NET 10 SDK writes `slnx` (XML) by default and `dotnet build`/`dotnet test` read it.

## Decision

All projects target `net10.0` (pinned in `Directory.Build.props`). The solution is `Sme.Platform.slnx`. `global.json` accepts any 10.0.x SDK (`rollForward: latestFeature`). Nullable reference types and implicit usings are on everywhere. Test package versions are centralized in `Directory.Packages.props`.

## Options considered

- **.NET 8** — out of support; would force an upgrade before launch.
- **Classic `.sln`** — only needed for tools that cannot read `slnx`. None is in use. If one appears, generate `.sln` from the `slnx` rather than maintaining both.

## Consequences

- Contributors need .NET SDK 10. CI uses `global.json` through `actions/setup-dotnet`.
- Any tool choice (IDE, analyzers) must support `slnx`.
