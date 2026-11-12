# Architecture decision records

One file per decision, numbered in the order they were written. A record is short: the situation, the decision, and what it costs. Write a new record to change a decision; do not rewrite an accepted one. The old record gets status **Superseded by ADR-nnnn**.

| ADR | Decision | Status |
| --- | --- | --- |
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions in the repository | Accepted |
| [0002](0002-dotnet-10-and-slnx.md) | .NET 10 and the `slnx` solution format | Accepted |
| [0003](0003-one-repository-one-project-per-service.md) | One repository, one project per service, no project references between services | Accepted |
| [0004](0004-xunit-with-central-package-versions.md) | xUnit with central package versions | Accepted |
| [0005](0005-one-local-postgres-through-compose.md) | One local Postgres through Compose; services run with `dotnet run` | Accepted |
| [0006](0006-http-only-on-localhost.md) | HTTP only on localhost for development | Accepted |
| [0007](0007-row-level-tenant-isolation.md) | Row-level tenant isolation in Postgres | Accepted |
| [0008](0008-database-ownership-per-service.md) | Database ownership per service | Proposed |
| [0009](0009-service-images-before-staging.md) | Container images for services before Staging | Proposed |
| [0010](0010-classic-sln-alongside-slnx.md) | Classic `.sln` alongside `slnx` | Proposed |
| [0011](0011-repository-license.md) | Repository license | Proposed |

## Statuses

- **Proposed**: written, not yet agreed by the owner (علي). Code must not depend on it. The card that needs it is named in the record.
- **Accepted**: in force. Follow it.
- **Superseded**: replaced by a later record, which is named.

## Writing one

Copy `template.md` to the next number. Keep it under a page. Name the Trello card that caused it. Put the record in the same pull request as the first code that depends on it.
