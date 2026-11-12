# ADR-0007: Row-level tenant isolation in Postgres

**Status:** Accepted
**Date:** 2026-11-12
**Card:** UC-003 (needed by UC-006, UC-009)

## Context

The platform is multi-tenant. Each business that registers must be unable to read or write another business's rows, and that must hold even when application code has a bug. The Trello cards state this directly: UC-006 "RLS blocks reading another tenant's rows (test)" and UC-009 "harness fails on a deliberately leaking endpoint". This record writes down what the cards already require so UC-006 can start without re-deciding it.

## Decision

- Every tenant-owned table has a non-null `tenant_id` column.
- Postgres row-level security is enabled on every tenant-owned table, with a policy that compares `tenant_id` to a session setting (for example `app.tenant_id`) that the service sets at the start of each request from the authenticated token.
- The database role the services use is not the table owner and does not have `BYPASSRLS`, so the policy applies to the application.
- The tenant context lives in `Sme.BuildingBlocks` so every service sets it the same way.
- Every card that adds a tenant-owned table adds a test that one tenant cannot read another tenant's row in that table. UC-009 runs those tests in CI.

## Consequences

- Isolation does not depend on every query remembering a `WHERE tenant_id = ...`.
- Platform-level operations (support, migrations) need a separate role or an explicit policy. That is designed when the first such operation is needed, not before.
- The choice between schemas and databases per service (ADR-0008) does not change this record. Row-level security applies within whichever layout is chosen.
