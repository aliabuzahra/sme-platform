# ADR-0004: xUnit with central package versions

**Status:** Accepted
**Date:** 2026-11-10
**Card:** UC-001

## Context

Six services need test projects that stay on the same versions. Package versions scattered across twelve project files drift.

## Decision

- Test framework is xUnit (`xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`).
- `Directory.Packages.props` turns on central package management and is the only place a package version is written. Project files reference packages without a version.
- Each test project references its service project and declares `Xunit` as a global using.

## Consequences

- `dotnet test Sme.Platform.slnx` runs every test project in one command and passes with zero tests, which is the state of the skeleton.
- Any new package, in any project, is added to `Directory.Packages.props` first.
