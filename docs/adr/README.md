# Architecture decision records

One file per decision, numbered in order of creation. A record is never edited after it is **Accepted**; a later record supersedes it and both link to each other.

| # | Title | Status |
| --- | --- | --- |
| [0001](0001-dotnet-10-and-slnx.md) | .NET 10 and the `slnx` solution format | Accepted |
| [0002](0002-one-repository-one-project-per-service.md) | One repository, one project per service, shared code in BuildingBlocks | Accepted |
| [0003](0003-transactional-outbox.md) | Transactional outbox for publishing events | Accepted |
| [0004](0004-azure-service-bus.md) | Azure Service Bus as the event broker | Accepted |
| [0005](0005-postgres-tenant-isolation.md) | Postgres with row-level security for tenant isolation; data layout per service | Accepted |
| [0006](0006-phone-otp-sign-in.md) | Phone number and one-time code sign-in, no passwords | Accepted |
| [0007](0007-single-gateway-with-module-entitlements.md) | One API gateway enforcing module entitlements | Accepted |
| [0008](0008-azure-staging-and-observability.md) | Azure Staging built from code, Application Insights for observability | Accepted |
| [0009](0009-repository-license.md) | Repository license | Proposed |

## Statuses

- **Proposed** — written, waiting for the owner named in the record. Code may prepare for it but must not lock it in.
- **Accepted** — in force. Implement against it.
- **Superseded by NNNN** — replaced; kept for history.

## Writing one

Copy [`template.md`](template.md). Keep it to one page: the context that forced the decision, the decision in one or two sentences, the options that were considered, and what the decision costs. Name the card that triggered it and the SAD section it comes from when there is one.
