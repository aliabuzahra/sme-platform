# SME Platform — architecture

This page is the working summary of the Software Architecture Document (SAD) kept outside the repository. It is written for whoever implements a card, human or Claude Code, so that each change follows the same structure and the same decisions. When the SAD and this page disagree, fix this page and reference the SAD section.

Decisions with a lasting effect are recorded as ADRs under [`adr/`](adr/README.md). This page points at them; it does not restate them.

## 1. What the platform is

A modular, multi-tenant B2B platform for small businesses in Jordan. One shared core (tenants, sign-in, users and roles, branches, notifications, audit, billing) plus **sector templates** that turn the core into a product for one sector (services, labels, messages) without new code. The first launch sector is chosen at Gate G1 (UC-015) after customer interviews; the core is built first.

Arabic and right-to-left are first-class. Every client surface is built RTL-first and checked in both directions (UC-018, UC-019).

## 2. Services

One repository, one .NET project per service, each deployable on its own. Services share local infrastructure through Docker Compose and, later, Azure.

| Service | Project | Port | Owns |
| --- | --- | --- | --- |
| Gateway | `src/Gateway` | 5080 | The single HTTP entry point. Authenticates every request and enforces module entitlements per tenant (UC-011). |
| Identity | `src/Identity` | 5081 | Tenants (UC-006), branches (UC-008), users, roles and invitations (UC-016), phone + OTP sign-in (UC-007). Issues tokens that carry tenant and module claims. |
| Notifications | `src/Notifications` | 5082 | Outbound messages: OTP delivery, order and status messages. Has a fake sender for tests. |
| Audit | `src/Audit` | 5083 | Append-only audit trail, per tenant. |
| Billing | `src/Billing` | 5084 | Subscriptions and module entitlements per tenant. |
| Templates | `src/Templates` | 5085 | Sector templates: services, labels and messages a new tenant starts with (UC-017, UC-020). |
| BuildingBlocks | `src/BuildingBlocks` | — | Shared library: cross-cutting types only (tenant context, event contracts, outbox primitives, telemetry helpers). Not a service. |

Each service has a matching `tests/<Service>` xUnit project. See [ADR-0002](adr/0002-one-repository-one-project-per-service.md).

## 3. Boundaries

- A service project never references another service project. Anything two services need goes in `Sme.BuildingBlocks`, and `Sme.BuildingBlocks` never references a service.
- Services do not call each other over HTTP for business flows. They publish and consume **events** ([ADR-0003](adr/0003-transactional-outbox.md), [ADR-0004](adr/0004-azure-service-bus.md)). The Gateway is the only component that fans a client request into a service call.
- Every row that belongs to a tenant carries `tenant_id`, and the database enforces isolation with row-level security ([ADR-0005](adr/0005-postgres-tenant-isolation.md)). A cross-tenant test harness runs in CI and must fail on a deliberately leaking endpoint (UC-009).
- No secrets in the repository or in pipeline variables; configuration comes from the environment and, on Azure, from Key Vault ([ADR-0008](adr/0008-azure-staging-and-observability.md)).

## 4. Request flow

```
Client (web / mobile)
   │  HTTPS, bearer token
   ▼
Gateway ─── 401 if the token is missing or invalid
   │    ─── 403 if the tenant's plan does not include the module,
   │        or the user's role lacks the module permission
   ▼
Service (Identity, Billing, Templates, …)
   │  reads/writes only rows where tenant_id = token.tenant_id (RLS)
   ▼
Postgres
```

Tokens are issued by Identity after phone + OTP sign-in ([ADR-0006](adr/0006-phone-otp-sign-in.md)). A token carries `tenant_id`, the user's roles, and the modules enabled for the tenant, so the Gateway can decide without a database call. See [ADR-0007](adr/0007-single-gateway-with-module-entitlements.md).

## 5. Event flow

```
Service A ──(same DB transaction)──▶ outbox table
                                         │ relay
                                         ▼
                                  Azure Service Bus topic
                                         │ subscription
                                         ▼
Service B consumer ── idempotent by event id ──▶ its own tables
```

- A business change and its event are written in one transaction; a relay publishes the outbox to the broker. An event survives a broker restart because it is already durable in the outbox.
- Consumers record processed event ids, so a duplicate delivery is processed once.
- Event contracts live in `Sme.BuildingBlocks` and are versioned additively.

This backbone is UC-012. Nothing in the skeleton uses it yet.

## 6. Data

- One Postgres 18 instance locally (Compose), one database.
- One schema per service in that database (`identity`, `billing`, …), each with its own unprivileged role ([ADR-0005](adr/0005-postgres-tenant-isolation.md)). UC-006 creates the first one, `identity`.
- Migrations belong to the owning service and run on deploy.

## 7. Environments

| Environment | Where | How it is built | Who deploys |
| --- | --- | --- | --- |
| Local | Developer machine | `docker compose up` (Postgres) + `dotnet run` per service | Developer |
| Staging | Azure | From code (UC-013). Every merge to `main` deploys automatically. | GitHub Actions |
| Production | Azure | Same definition as Staging, separate resources. | Manual promotion after Staging checks. |

Observability baseline (UC-014): traces, logs and metrics per tenant in Application Insights, so a request can be followed from the Gateway to a consumer.

## 8. Clients

- **Web app** (UC-018): the owner's dashboard. Arabic, RTL design system, phone sign-in against Staging.
- **Mobile app** (UC-019): staff on the shop floor. Android and iOS, phone sign-in against Staging.

Clients are not in this repository yet; the cards decide where they live.

## 9. How cards map to this document

| Card | Area | Section |
| --- | --- | --- |
| UC-001 | Repository skeleton | §2, ADR-0001, ADR-0002 |
| UC-002 | CI | `README.md` → CI |
| UC-003 | This document and the ADRs | — |
| UC-006, UC-009 | Tenants and isolation | §3, §6, ADR-0005 |
| UC-007, UC-016 | Sign-in, users and roles | §4, ADR-0006 |
| UC-008 | Branches | §2 (Identity) |
| UC-011 | Gateway and entitlements | §4, ADR-0007 |
| UC-012 | Outbox and Service Bus | §5, ADR-0003, ADR-0004 |
| UC-013, UC-014 | Staging and observability | §7, ADR-0008 |
| UC-017, UC-020 | Sector templates | §1, §2 (Templates) |
| UC-018, UC-019 | Web and mobile shells | §8 |

## 10. Working rules for implementers

1. Read `CLAUDE.md` first: build, test, no secrets.
2. Find the card's section here and the ADRs it names. If the card needs a decision that has no ADR, write one as **Proposed** in the same pull request and name the owner who must accept it; do not decide silently in code.
3. Keep a change inside its card. Product behavior on a skeleton card, or a second CI workflow on an unrelated change, is out of scope.
4. Branch names are the card id followed by the card title, lower-case, hyphenated: `uc-006-tenant-registration-and-tenant-model`.
