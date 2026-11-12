# 0005. Postgres with row-level security for tenant isolation; data layout per service

**Status:** Accepted — علي chose schema-per-service on 2026-11-12
**Date:** 2026-11-12
**Card:** UC-006, UC-009 · **SAD:** §4, §7 · **NFR:** NFR-05
**Owner:** علي

## Context

Every tenant must be unable to read another tenant's data (NFR-05). The skeleton runs one Postgres 18 instance with one database and no schemas. `README.md` lists as open whether services get a database each, a schema each, or share one database once they store data.

## Decision (the part already settled)

Postgres is the system of record. Every tenant-owned table has a `tenant_id` column and a **row-level security** policy that compares it with the current tenant set on the connection by the service from the token. Services never query without a tenant context. A cross-tenant test harness (UC-009) runs in CI and fails on any endpoint that leaks.

## Decision (data layout)

**Schema-per-service in one database.** Each service owns one schema (`identity`, `billing`, `templates`, …) and a database role that can see only its schema. One instance, one backup, one connection string per service.

## Options considered

- **One shared schema** — simplest now; service boundaries erode through cross-schema joins and shared migrations.
- **Schema-per-service (chosen)** — boundaries enforced by roles, still one instance to run; joins across services are impossible by construction, which is what the event design wants.
- **Database-per-service** — strongest isolation; more instances and backups than the team needs before launch. Can be adopted later per service since schemas never share tables.

## How a request runs

Each request opens a transaction, switches to the service's unprivileged role (`SET LOCAL ROLE identity_app`) and sets `app.tenant_id` with `set_config(..., true)`. Both last only for that transaction, so a pooled connection never carries one tenant into the next request. Tables use `FORCE ROW LEVEL SECURITY` so the policy also binds the table owner. In deployed environments the service's login role is a member of the unprivileged role and is never a superuser, because superusers bypass row-level security. UC-006 is the first implementation.

## Consequences

- Each service ships its own migrations for its schema.
- RLS policies are part of every migration that adds a tenant-owned table; reviewers check for them.
- Reporting across services goes through events, not SQL joins.
