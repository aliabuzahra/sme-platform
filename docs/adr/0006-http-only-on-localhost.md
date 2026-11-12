# ADR-0006: HTTP only on localhost for development

**Status:** Accepted
**Date:** 2026-11-10
**Card:** UC-001

## Context

ASP.NET Core templates set up HTTPS with a developer certificate that must be trusted on each machine and in CI. Nothing in the skeleton carries a secret over the wire.

## Decision

Services listen on plain HTTP on `localhost` in development. Each `launchSettings.json` has one `http` profile. TLS terminates in front of the services in Staging and Production (UC-013), not inside them.

## Consequences

- No certificate setup for developers or CI.
- Phone and OTP sign-in (UC-007) sends codes over this channel locally. That is acceptable only with the fake sender and only on localhost. Staging must be HTTPS before a real sender is wired.
