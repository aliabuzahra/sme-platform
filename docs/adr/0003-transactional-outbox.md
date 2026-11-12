# 0003. Transactional outbox for publishing events

**Status:** Accepted
**Date:** 2026-11-12
**Card:** UC-012 · **SAD:** §5 (referenced there as ADR-03)
**Owner:** علي

## Context

Services must not call each other for business flows (ADR-0002); they communicate by events. Publishing straight to a broker inside a request loses events when the broker is down or the request fails after the publish. UC-012 requires that an event survives a broker restart and that a duplicate is processed once.

## Decision

A service writes its state change and the event into an **outbox table in the same database transaction**. A relay reads the outbox and publishes to the broker, marking rows as sent. Consumers are idempotent: they record processed event ids and skip repeats. Outbox and inbox primitives live in `Sme.BuildingBlocks`.

## Options considered

- **Publish directly from the request** — dual-write problem; events lost on broker outages.
- **Change data capture (Debezium)** — robust, but adds infrastructure the team does not run yet.
- **Synchronous HTTP between services** — couples deployments and availability.

## Consequences

- Every service that emits events needs an outbox table and a relay; migrations include them.
- At-least-once delivery is the contract; every consumer must be idempotent and tested as such.
- Events are durable the moment the business transaction commits.
