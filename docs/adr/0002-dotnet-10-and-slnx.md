# ADR-0002: .NET 10 and the `slnx` solution format

**Status:** Accepted
**Date:** 2026-11-10
**Card:** UC-001

## Context

The skeleton needed a target framework and a solution file. .NET 8 was out of support at the time. The .NET 10 SDK writes `slnx` by default from `dotnet new sln`.

## Decision

- Every project targets `net10.0`, pinned once in `Directory.Build.props` together with nullable reference types and implicit usings.
- `global.json` asks for SDK `10.0.100` with `rollForward: latestFeature`, so any 10.0 SDK works and CI uses the same major version as developers.
- The solution is `Sme.Platform.slnx`. There is no classic `.sln`.

## Consequences

- Tooling must understand `slnx`. Visual Studio 2022 17.13 and later, Rider, and the `dotnet` CLI on SDK 9 and later do. A tool that cannot read it is the subject of ADR-0010.
- When .NET 11 ships, upgrading is one line in `Directory.Build.props` and one in `global.json`.
