# ADR-0003: One repository, one project per service, no project references between services

**Status:** Accepted
**Date:** 2026-11-10
**Card:** UC-001

## Context

The platform is split into services (Gateway, Identity, Notifications, Audit, Billing, Templates) that must deploy separately but are built by one small team. Separate repositories would multiply CI, versioning and review overhead. One project would let service boundaries erode.

## Decision

- One repository. One project under `src/<Service>` per service, with its test project under `tests/<Service>`. Services are web projects; `Platform` is not a service.
- A service project does not reference another service project. The build is the first check; an architecture test will enforce it when `BuildingBlocks` has enough surface to need one.
- Shared code goes in `src/BuildingBlocks` (`Sme.BuildingBlocks`). It holds cross-cutting types only and does not reference a service.
- Services communicate over HTTP through the Gateway or over events (UC-012). Never by in-process calls across projects.

## Consequences

- Adding a service means adding two projects and two lines in `Sme.Platform.slnx`.
- A type that two services need is a sign that it belongs in `BuildingBlocks` or that the boundary is wrong. Decide which before copying it.
- A change can touch several services in one pull request, which is simpler for the team than coordinating repositories.
