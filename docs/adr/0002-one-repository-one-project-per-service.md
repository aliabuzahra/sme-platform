# 0002. One repository, one project per service, shared code in BuildingBlocks

**Status:** Accepted
**Date:** 2026-11-12
**Card:** UC-001 · **SAD:** §9
**Owner:** علي

## Context

The platform is a set of services (Gateway, Identity, Notifications, Audit, Billing, Templates) built by a small team with one CI pipeline. Separate repositories would multiply pipelines and version drift; a single project would make service boundaries invisible.

## Decision

One repository. Each service is its own project under `src/<Service>` with its own `tests/<Service>` xUnit project. Cross-cutting code lives in `src/BuildingBlocks` (`Sme.BuildingBlocks`). A service project never references another service project; `Sme.BuildingBlocks` never references a service. Each service runs and deploys on its own.

## Options considered

- **Repository per service** — more pipelines, harder atomic changes to shared contracts, overkill for the team size.
- **Single project with folders** — no enforced boundaries; the compiler would allow any cross-service call.

## Consequences

- Boundaries are enforced by project references, which CI builds. A reviewer can see a boundary break in the `.csproj` diff.
- Event contracts and tenant primitives go in BuildingBlocks and must stay free of service logic.
- The whole platform builds and tests with two commands (`CLAUDE.md`).
