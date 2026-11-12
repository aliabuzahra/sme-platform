# 0007. One API gateway enforcing module entitlements

**Status:** Accepted
**Date:** 2026-11-12
**Card:** UC-011 · **SAD:** §6 · **CR:** CR-03
**Owner:** علي

## Context

Modules are sold and enabled per tenant. If each service checked entitlements itself, a missed check in one service would expose a module a tenant has not paid for, or another tenant's data.

## Decision

`src/Gateway` is the only public HTTP entry point. It validates the token (401 when missing or invalid) and checks that the route's module is enabled for the tenant and permitted for the user's role (403 otherwise). Services are reachable only from the Gateway on the network and still enforce tenant isolation at the database (ADR-0005). Entitlements come from token claims so the Gateway needs no database call per request.

## Options considered

- **Check in every service** — duplicated logic, easy to forget.
- **Service mesh / Azure API Management policies** — stronger platform tooling, more cost and setup than the launch needs; can front the Gateway later.

## Consequences

- Token claims must change when a tenant's plan changes; tokens are short-lived or re-issued on plan change.
- The Gateway is a single point of failure and must be the first service with a health check and scale rules on Azure.
- Route-to-module mapping is configuration the Gateway owns and tests.
