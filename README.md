# SME Platform

One repository, one .NET project per service. Services build and run separately, and share local infrastructure through Docker Compose.

This is the repository skeleton (UC-001). It does not implement tenants, sign-in, billing, or other product behavior.

## Layout

```
src/BuildingBlocks     Shared library. Cross-cutting types only.
src/Gateway            HTTP entry point (port 5080)
src/Identity           Tenants, branches, users, and roles (port 5081)
src/Notifications      Outbound messages, including later OTP delivery (port 5082)
src/Audit              Audit trail (port 5083)
src/Billing            Billing (port 5084)
src/Templates          Sector templates (port 5085)
tests/<Service>        Empty xUnit project for that service
```

A service project does not reference another service project. Shared code goes in `src/BuildingBlocks`, and `Sme.BuildingBlocks` does not reference a service.

`Directory.Build.props` pins the target framework (`net10.0`) and turns on nullable reference types. `Directory.Packages.props` holds test package versions. `Sme.Platform.slnx` is the solution.

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) (LTS). `global.json` accepts any 10.0 SDK.
- Docker with Compose v2.

## Build and test

```bash
dotnet build Sme.Platform.slnx
dotnet test Sme.Platform.slnx
```

`dotnet test` succeeds with no tests. Each `tests/<Service>` project is intentionally empty until a later card adds behavior.

## CI

GitHub Actions runs on every pull request and every push to `main`. The job is named **Build and test**. It restores, builds, and tests `Sme.Platform.slnx` with the .NET SDK from `global.json`, then runs `docker compose config`. The job is limited to 8 minutes.

A pull request whose tests fail cannot be merged once the repository owner has required the **Build and test** check on the target branch. That setting is documented in `docs/branch-protection-for-owner.md` and is applied by the owner, not by this change.

## Run Postgres

Compose starts Postgres only. The defaults below are local placeholders, already applied when `.env` is absent, so a clean machine does not need extra setup:

| Variable | Default |
| --- | --- |
| `POSTGRES_USER` | `sme` |
| `POSTGRES_PASSWORD` | `change-me` |
| `POSTGRES_DB` | `sme_platform` |
| `POSTGRES_PORT` | `5432` |

```bash
docker compose up -d
docker compose ps
```

Copy `.env.example` to `.env` only to override those values. Do not commit `.env`. Do not reuse `change-me` anywhere except this local database.

Postgres 18 keeps its data files under `/var/lib/postgresql/18/docker`. The Compose volume is mounted at `/var/lib/postgresql`.

Stop it with `docker compose down`. Add `-v` to drop the data volume.

## Run a service

```bash
dotnet run --project src/Identity
```

`GET http://localhost:5081/health` returns the service name. The ports are listed in the layout above and in each project's `Properties/launchSettings.json`. Services do not connect to Postgres yet.

## Not in this skeleton

- Product behavior: tenants, row-level security, phone OTP, the gateway auth check, the outbox, App Insights
- Container images for the .NET services
- The web and mobile clients (UC-018, UC-019)

## Decisions

Decisions are recorded in [`docs/adr/`](docs/adr/README.md) and summarized in [`docs/architecture.md`](docs/architecture.md). The ones the skeleton already follows are .NET 10 with `slnx` (ADR-0002), one project per service with no references between services (ADR-0003), xUnit with central package versions (ADR-0004), one local Postgres through Compose (ADR-0005), and HTTP only on localhost (ADR-0006).

## Open decisions

These are written as **Proposed** ADRs and need the owner before the card that depends on them starts:

- Database ownership per service, before UC-006 (ADR-0008).
- Container images for services before Staging, before UC-013 (ADR-0009).
- Whether a classic `.sln` is needed next to `slnx` (ADR-0010).
- The repository license (ADR-0011).
