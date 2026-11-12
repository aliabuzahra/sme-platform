# ADR-0008: Database ownership per service

**Status:** Proposed. Needs the owner (علي) before UC-006 starts.
**Date:** 2026-11-12
**Card:** UC-003 (blocks UC-006)

## Context

Services share one Postgres instance locally (ADR-0005). The first card that stores data, UC-006, must know where its tables go. The `README.md` from UC-001 lists this as an open decision. Three layouts are possible.

| Option | What it means | For | Against |
| --- | --- | --- | --- |
| A. One shared database, one schema | All services write to `public`. | Simplest migrations and local setup. | No boundary at all. A service can query another service's tables. Hard to split later. |
| B. One database, one schema per service | `identity.*`, `billing.*`, and so on. Each service's role has rights only on its schema. | Keeps one Compose instance and one connection string per service. Boundary enforced by grants. Can move a schema to its own database later. | Cross-service joins are possible for a superuser, so reviews must still watch for them. |
| C. One database per service | Separate databases, separate roles. | Hardest boundary. Matches separate deployment. | Six databases locally and in Staging. Cross-service reporting needs events or a separate store from day one. |

## Decision (proposed)

Option B: one database, one schema per service, one Postgres role per service with rights only on its own schema. Row-level security (ADR-0007) is applied within each schema. Migrations are owned by the service and live in its project.

## Consequences

- UC-006 creates the `identity` schema and role, and the first migration tooling. Later services copy that pattern.
- Staging (UC-013) provisions one database and six roles.
- Moving a service to its own database later is a schema move, not a rewrite.
- UC-006 was implemented on this proposed option so the Identity work could proceed: the `identity` schema and the `sme_app` role are created by `src/Identity/Migrations/0001_tenants.sql`. The record stays **Proposed** until the owner confirms. If the owner picks A or C, that migration and the SQL in `TenantRepository` are the only places the schema name appears.
