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

- CI workflows (UC-002)
- Architecture docs and ADRs (UC-003)
- Product behavior: tenants, row-level security, phone OTP, the gateway auth check, the outbox, App Insights
- Container images for the .NET services
- The web and mobile clients (UC-018, UC-019)

## Decisions

- **.NET 10 (`net10.0`).** Latest LTS at the time of the skeleton. .NET 8 is out of support.
- **Solution format is `slnx`.** That is what `dotnet new sln` writes on the .NET 10 SDK.
- **xUnit** for test projects, with versions centralized in `Directory.Packages.props`.
- **One local Postgres 18 instance and one database.** No schemas or roles yet. Services are not containerized; Compose is the shared database, and `dotnet run` starts a service.
- **Platform is not an HTTP service.** Gateway, Identity, Notifications, Audit, Billing, and Templates are the service projects. Platform cross-cutting code belongs in `Sme.BuildingBlocks`.
- **HTTP only on localhost.** Dev HTTPS certificates are not required for the skeleton.

## Open decisions

These need the owner before later cards lock them in. UC-003 is the place to record the answers as ADRs.

- Database-per-service, schema-per-service, or one shared database once services start storing data.
- Whether each service gets its own image in Compose before the Azure staging environment (UC-013).
- Whether the classic `.sln` format is required for a tool that cannot read `slnx`.
- The repository license. There is no `LICENSE` file yet.
