# 0004. Azure Service Bus as the event broker

**Status:** Accepted
**Date:** 2026-11-12
**Card:** UC-012 · **SAD:** §5 (referenced there as ADR-04)
**Owner:** علي

## Context

The outbox relay (ADR-0003) needs a broker. Staging and Production run on Azure (ADR-0008). The team is small and wants a managed service with topics, subscriptions, dead-lettering and duplicate detection.

## Decision

Azure Service Bus, Standard tier, with one topic per event stream and one subscription per consuming service. Locally and in CI, the relay and consumers run against a lightweight substitute (the Service Bus emulator or an in-memory transport behind the same abstraction in `Sme.BuildingBlocks`); the abstraction is the only thing service code touches.

## Options considered

- **Kafka / Event Hubs** — more throughput than needed; heavier to operate and no native dead-letter per subscriber at this scale.
- **RabbitMQ** — self-hosted or third-party hosting on Azure; adds operations.
- **Postgres as a queue (`LISTEN/NOTIFY`, polling)** — fine for one service, not for fan-out across services.

## Consequences

- Broker configuration is infrastructure-as-code with the Azure environment (UC-013).
- Message size, ordering and sessions follow Service Bus limits; design events to be small and self-describing.
- Dead-letter queues must be monitored (UC-014).
