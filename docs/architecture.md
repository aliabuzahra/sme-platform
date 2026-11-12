# Architecture

One repository. One .NET project per service. Services build and run separately and share local infrastructure through Docker Compose. This page describes the system as it exists after UC-001 and UC-002, and names the cards that add the rest.

## Services

| Project | Port | Responsibility (planned) | Card that adds behavior |
| --- | --- | --- | --- |
| `src/Gateway` | 5080 | Single HTTP entry point. Validates the token, checks the tenant and module entitlements, forwards to a service. | UC-011 |
| `src/Identity` | 5081 | Tenants (UC-006), branches with opening hours (UC-008), phone and OTP sign-in with tokens (UC-007); roles and invitations to come. | UC-016 |
| `src/Notifications` | 5082 | Outbound messages: OTP delivery, later SMS and WhatsApp. | UC-007 (fake sender), later cards |
| `src/Audit` | 5083 | Audit trail of who did what, per tenant. | later card |
| `src/Billing` | 5084 | Plans, module entitlements, invoices. | later card |
| `src/Templates` | 5085 | Sector templates: services, labels, messages that a new tenant starts with. | UC-017, UC-020 |
| `src/BuildingBlocks` | n/a | Shared library. Cross-cutting code only: tenant context and tenant-aware connections (done, UC-006), SQL migrator (done), outbox, messaging contracts. | UC-009, UC-012 |

Each service has an `xUnit` test project under `tests/<Service>`. Every service exposes `GET /health`. Identity also exposes `POST /tenants/` (registration with the owner's phone), `GET /tenants/{id}`, `POST`, `GET /branches/` and `GET /branches/{id}` for the current tenant, and sign-in: `POST /auth/otp/request`, `POST /auth/otp/verify`, `GET /auth/me`. Ports are set in each project's `Properties/launchSettings.json`.

## Boundaries

These rules are enforced by review and, where a later card adds it, by an architecture test.

- A service project must not reference another service project. Services talk over HTTP through the Gateway or over events (UC-012). Never by project reference.
- `Sme.BuildingBlocks` must not reference a service.
- Product behavior belongs to the card that owns it. A skeleton or infrastructure change does not add tenants, OTP, row-level security, gateway checks, the outbox, or telemetry.
- One workflow file, `.github/workflows/ci.yml`. Do not add a second workflow on an unrelated change.

## Request flow (target)

```
client (web UC-018, mobile UC-019)
   │  HTTPS, bearer token with tenant and module claims (issued by Identity, UC-007, ADR-0012)
   ▼
Gateway  ── 401 without a valid token, 403 for a disabled module (UC-011)
   │
   ▼
service (Identity, Billing, ...)  ── reads and writes only its own tenant's rows (UC-006, UC-009)
   │
   ▼
outbox ──► Service Bus ──► consumers in other services (UC-012)
```

Today the Gateway forwards nothing, and each service validates the token itself with `AddPlatformJwt()` from `Sme.BuildingBlocks`. The flow above is the target that UC-011 and UC-012 complete.

## Data

Local development uses one Postgres 18 instance started by `docker-compose.yml`, with the placeholder credentials listed in `README.md`. Identity is the first service that stores data (UC-006).

How data is split between services (one database per service, one schema per service, or one shared database) is still [ADR-0008](adr/0008-database-ownership-per-service.md), status Proposed. UC-006 was built on its proposed option, one schema per service: Identity owns the `identity` schema, created by its own migrations. If the owner chooses another option, the schema prefix in Identity's migration and SQL is the only thing that moves.

Tenant isolation is row-level (ADR-0007). The pieces in `Sme.BuildingBlocks`:

- `TenantContext`, scoped per request, holds the current tenant. `UseTenantFromToken()` fills it from the `tenant_id` claim of a valid bearer token. `UseTenantHeader()` fills it from `X-Tenant-Id` only when `Tenancy:AllowHeader` is set, which only Development does.
- `TenantConnectionFactory` opens connections in two modes. Platform mode is the owning login role with no tenant, used only where no tenant exists yet (registration). Tenant mode does `SET ROLE sme_app` and sets `app.tenant_id`, so every policy filters to that tenant. Npgsql resets both when the connection returns to the pool.
- `SqlMigrator` applies a service's embedded `Migrations/NNNN_name.sql` scripts once each, recorded in `<schema>.schema_migrations`, at service start.

The `sme_app` role has no login, no BYPASSRLS and owns nothing, so policies apply to it. Every tenant-owned table enables row-level security with a policy on `app.tenant_id`; the tenants table itself filters on its `id`. UC-009 adds the harness that fails CI on a deliberately leaking endpoint.

## Build, test, CI

```bash
dotnet build Sme.Platform.slnx
dotnet test Sme.Platform.slnx
docker compose config
```

`Directory.Build.props` pins `net10.0`, nullable, and implicit usings for every project. `Directory.Packages.props` holds package versions centrally. `global.json` accepts any 10.0 SDK.

GitHub Actions runs the same three commands on every pull request and every push to `main` (job **Build and test**, capped at 8 minutes). The owner makes that job a required check so a failing pull request cannot merge; see `branch-protection-for-owner.md`.

## Environments

| Environment | State | Card |
| --- | --- | --- |
| Local | Compose Postgres plus `dotnet run` per service. | UC-001 |
| Staging on Azure | Not built. Merge to `main` will deploy there. | UC-013 |
| Observability | Not built. Traces, logs and metrics per tenant in App Insights. | UC-014 |
| Production | Not planned until the launch sector is chosen (UC-015). | |

## Decisions

Accepted and proposed decisions are in [`adr/`](adr/README.md). The short list:

- ADR-0002 .NET 10 and the `slnx` solution format.
- ADR-0003 One repository, one project per service, no project references between services.
- ADR-0004 xUnit with central package versions.
- ADR-0005 One local Postgres through Compose; services run with `dotnet run`.
- ADR-0006 HTTP only on localhost for development.
- ADR-0007 Row-level tenant isolation in Postgres.
- ADR-0008 Database ownership per service (proposed, needs the owner).
- ADR-0009 Container images for services before Staging (proposed, needs the owner).
- ADR-0010 Classic `.sln` alongside `slnx` (proposed, default is no).
- ADR-0011 Repository license (proposed, needs the owner).
- ADR-0012 Platform access tokens are HS256 JWTs issued by Identity.
