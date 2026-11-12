# ADR-0012: Platform access tokens are HS256 JWTs issued by Identity

**Status:** Accepted (settled in UC-007; the Gateway card may revisit the key type)
**Date:** 2026-11-12
**Card:** UC-007

## Context

Phone and OTP sign-in must hand the client something that every service, and later the Gateway (UC-011), can check without calling Identity on each request. The cards require the token to carry the tenant and the modules the tenant is entitled to.

## Decision

- Identity issues a JSON Web Token after a one-time code is verified. Claims: `sub` (user id), `tenant_id`, `phone`, `role` (`owner` or `staff`), and one `modules` claim per entitled module. Issuer `sme-identity`, audience `sme-platform`, lifetime 12 hours by default.
- Tokens are signed with HMAC-SHA256 and a single shared key (`Jwt:SigningKey`, at least 32 bytes). Every service validates with the same key through `AddPlatformJwt()` in `Sme.BuildingBlocks`. Development ships a placeholder key in `appsettings.Development.json`; Staging and Production set it from their secret store.
- A valid token's `tenant_id` becomes the request's tenant (`UseTenantFromToken()`). The `X-Tenant-Id` header stand-in is honoured only when `Tenancy:AllowHeader` is true, which only Development sets.
- One-time codes are 6 digits, live 5 minutes, allow 5 wrong attempts, at most 3 requests per phone per 10 minutes, and are stored hashed with the row id and the signing key as pepper. A phone that belongs to several tenants names the tenant when verifying.

## Consequences

- A symmetric key means any service holding it could mint tokens. That is acceptable while all services are ours and sit behind one Gateway; moving to RS256 with a public key per service is a key swap in `AddPlatformJwt()` and the issuer, nothing else. UC-011 decides.
- There is no refresh token and no revocation list. Twelve hours bounds the damage of a stolen token; shorter lifetimes are a configuration change.
- Module entitlements live on the tenant row (`modules`) until Billing owns them. Changing entitlements takes effect at the next sign-in.
