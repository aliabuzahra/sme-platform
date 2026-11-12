# 0006. Phone number and one-time code sign-in, no passwords

**Status:** Accepted
**Date:** 2026-11-12
**Card:** UC-007, UC-016 · **SAD:** §7 · **CR:** CR-05, CR-04
**Owner:** علي

## Context

Users are small-business owners and staff in Jordan who manage their work from a phone. Passwords are forgotten and reused; phone numbers are already how staff are invited (CR-04).

## Decision

Identity signs users in with a phone number and a one-time code sent through Notifications. There are no passwords. Codes expire and are single-use; wrong and expired codes are rejected and rate-limited. On success Identity issues a token carrying `tenant_id`, roles and the modules enabled for the tenant (ADR-0007). Tests use a fake sender so sign-in works end to end without an SMS provider.

## Options considered

- **Email and password** — weak fit for staff without work email; adds reset flows.
- **Social sign-in** — not universal among the target users; ties identity to a third party.
- **WhatsApp OTP** — attractive locally; kept as a delivery channel option inside Notifications, not a different sign-in model.

## Consequences

- SMS cost per sign-in; sessions should be long-lived and refreshable to keep it low.
- A phone number identifies one person across tenants; the model must allow one user in several tenants.
- Brute-force protection on the code endpoint is mandatory from the first implementation.
