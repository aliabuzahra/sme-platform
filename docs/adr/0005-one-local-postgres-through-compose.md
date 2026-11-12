# ADR-0005: One local Postgres through Compose; services run with `dotnet run`

**Status:** Accepted
**Date:** 2026-11-10
**Card:** UC-001

## Context

Developers and CI need a database that starts on a clean machine without setup. Containerizing the .NET services as well would add image builds and Dockerfiles to the skeleton before any service has behavior.

## Decision

- `docker-compose.yml` starts Postgres 18 (`postgres:18-alpine`) only, with placeholder credentials applied by default so `.env` is optional. The data volume is mounted at `/var/lib/postgresql`, where Postgres 18 keeps its files.
- Services are started with `dotnet run --project src/<Service>` and listen on fixed ports 5080 to 5085.
- `.env.example` holds placeholders only. `change-me` is a local default, never a credential to reuse.
- CI runs `docker compose config` to keep the Compose file valid.

## Consequences

- No image build step exists for services. Whether to add one before Staging is ADR-0009.
- One Postgres instance and one database today. How services own their data inside it is ADR-0008.
