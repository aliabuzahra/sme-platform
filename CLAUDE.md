# Working in this repository

Read, in this order:

1. This file — definition of done and boundaries.
2. [`docs/architecture.md`](docs/architecture.md) — services, request and event flow, tenancy, environments, and which section each card maps to.
3. The ADRs the card names, under [`docs/adr/`](docs/adr/README.md). A **Proposed** ADR is not in force: prepare for it, do not lock it in. If a card needs a decision with no ADR, add one as Proposed in the same pull request and name its owner.

`README.md` covers how to build, run and test.

# Definition of done

A change is done only when all three are true:

1. **Build passes.** `dotnet build Sme.Platform.slnx` completes with no errors.
2. **Tests pass.** `dotnet test Sme.Platform.slnx` completes with no errors. Add tests for new behavior. Do not delete or weaken a test to make the run green.
3. **No secrets are committed.** No passwords, API keys, tokens, connection strings with real credentials, or `.env` files. `.env.example` stays placeholders only (`change-me` is a local Compose default, not a credential to reuse). If a secret is committed, rotate it; deleting it in a later commit does not make the history safe.

Run those two `dotnet` commands locally before opening a pull request. CI (UC-002) runs those same `dotnet` commands on GitHub Actions for every pull request and every push to `main` (workflow `.github/workflows/ci.yml`, job name **Build and test**). When Compose or `.env.example` changes, also run `docker compose config`. The workflow also runs `docker compose config`.

## Boundaries

- One service, one project under `src/<Service>`, with its empty-until-needed test project under `tests/<Service>`.
- A service project must not reference another service project. Shared code goes in `src/BuildingBlocks`, and that project must not reference a service.
- Do not add product behavior (tenants, OTP, row-level security, gateway checks, outbox, telemetry) on a skeleton change. Those belong to their own cards.
- CI is `.github/workflows/ci.yml` (UC-002). Do not add a second workflow on an unrelated change.
