# Architecture

One repository. One .NET project per service. Services build and run separately and share local infrastructure through Docker Compose. This page describes the system as it exists after UC-001 and UC-002, and names the cards that add the rest.

## Services

| Project | Port | Responsibility (planned) | Card that adds behavior |
| --- | --- | --- | --- |
| `src/Gateway` | 5080 | Single HTTP entry point. Validates the token, checks the tenant and module entitlements, forwards to a service. | UC-011 |
| `src/Identity` | 5081 | Tenants, branches, users, roles, invitations, phone and OTP sign-in. | UC-006, UC-007, UC-008, UC-016 |
| `src/Notifications` | 5082 | Outbound messages: OTP delivery, later SMS and WhatsApp. | UC-007 (fake sender), later cards |
| `src/Audit` | 5083 | Audit trail of who did what, per tenant. | later card |
| `src/Billing` | 5084 | Plans, module entitlements, invoices. | later card |
| `src/Templates` | 5085 | Sector templates: services, labels, messages that a new tenant starts with. | UC-017, UC-020 |
| `src/BuildingBlocks` | n/a | Shared library. Cross-cutting code only: tenant context, outbox, messaging contracts, common results. | UC-009, UC-012 |

Each service has an `xUnit` test project under `tests/<Service>`. Every service today exposes `GET /health` and nothing else. Ports are set in each project's `Properties/launchSettings.json`.

## Boundaries

These rules are enforced by review and, where a later card adds it, by an architecture test.

- A service project must not reference another service project. Services talk over HTTP through the Gateway or over events (UC-012). Never by project reference.
- `Sme.BuildingBlocks` must not reference a service.
- Product behavior belongs to the card that owns it. A skeleton or infrastructure change does not add tenants, OTP, row-level security, gateway checks, the outbox, or telemetry.
- One workflow file, `.github/workflows/ci.yml`. Do not add a second workflow on an unrelated change.

## Request flow (target)

```
client (web UC-018, mobile UC-019)
   │  HTTPS, bearer token with tenant and module claims
   ▼
Gateway  ── 401 without a valid token, 403 for a disabled module (UC-011)
   │
   ▼
service (Identity, Billing, ...)  ── reads and writes only its own tenant's rows (UC-006, UC-009)
   │
   ▼
outbox ──► Service Bus ──► consumers in other services (UC-012)
```

Today the Gateway forwards nothing. The flow above is the target that UC-006 to UC-012 build toward, in that order.

## Data

Local development uses one Postgres 18 instance started by `docker-compose.yml`, with the placeholder credentials listed in `README.md`. Services do not connect to it yet.

How data is split between services once they store it (one database per service, one schema per service, or one shared database) is an open decision. See [ADR-0008](adr/0008-database-ownership-per-service.md). UC-006 is the first card that needs the answer, so it must be accepted before that card starts.

Tenant isolation is row-level: every tenant-owned table carries a `tenant_id`, and Postgres row-level security blocks a session from reading another tenant's rows. UC-006 adds the first table and the first isolation test. UC-009 adds the harness that runs those tests in CI.

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
