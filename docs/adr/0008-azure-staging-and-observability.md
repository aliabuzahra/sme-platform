# 0008. Azure Staging built from code, Application Insights for observability

**Status:** Accepted
**Date:** 2026-11-12
**Card:** UC-013, UC-014 · **SAD:** §8 · **NFR:** NFR-13
**Owner:** علي

## Context

Every merge to `main` should be testable like Production. The team runs on Azure. Secrets must not live in the repository or in pipeline variables (`CLAUDE.md`).

## Decision

Staging is defined as infrastructure-as-code in this repository and deployed by GitHub Actions on every merge to `main`. Production uses the same definition with separate resources and a manual promotion. Secrets are stored in Azure Key Vault and reached through managed identity. Application Insights receives traces, logs and metrics from every service, tagged with `tenant_id`, so a request can be followed from the Gateway to an event consumer.

## Options considered

- **Click-ops Staging** — not reproducible; drifts from Production.
- **Other clouds** — no advantage for the team's Azure experience and existing accounts.
- **Self-hosted observability stack** — more to run; App Insights is enough at launch scale.

## Consequences

- UC-013 adds the IaC and the deploy workflow; UC-014 adds the telemetry package to BuildingBlocks.
- Per-tenant tagging is a rule for every log line and span from the first service.
- Dead-letter queues (ADR-0004) and health endpoints feed alerts.
