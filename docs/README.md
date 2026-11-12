# Documentation

Start here when you implement a card. Read `architecture.md` first, then the ADRs that touch the area you are changing. `CLAUDE.md` at the repository root is the definition of done and points back here.

| File | What it holds |
| --- | --- |
| [`architecture.md`](architecture.md) | The system in one page: services, boundaries, local infrastructure, CI, and what later cards add. |
| [`adr/`](adr/README.md) | Architecture decision records. One file per decision, numbered, never edited after acceptance except to change status. |
| [`branch-protection-for-owner.md`](branch-protection-for-owner.md) | The GitHub setting the owner applies so a failing pull request cannot merge (UC-002). |

## How to use these docs on a card

1. Check `architecture.md` for the boundary your change sits in. A service never references another service. Shared code goes in `src/BuildingBlocks`.
2. Check `adr/README.md` for an accepted decision that already settles your question. Follow it.
3. If your card needs a decision that is not recorded, or that is still **Proposed**, write or update an ADR in the same pull request. Do not settle an open decision silently in code.
4. Keep `architecture.md` current when a card changes a port, a project, a dependency between projects, or the local infrastructure.

The platform's Software Architecture Document (SAD) referenced on the Trello cards lives outside this repository. These docs are the part of it that the code must follow. When they disagree with the SAD, say so in the pull request and fix one of them.
