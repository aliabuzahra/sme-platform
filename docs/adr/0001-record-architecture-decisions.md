# ADR-0001: Record architecture decisions in the repository

**Status:** Accepted
**Date:** 2026-11-12
**Card:** UC-003

## Context

Implementation on this platform is done largely by coding agents working one card at a time. An agent sees the repository, not the meetings or the Software Architecture Document (SAD) kept outside it. Decisions that live only in people's heads or in the SAD get re-decided, differently, on every card.

## Decision

Architecture decisions are recorded as numbered files in `docs/adr/`, in the format of `template.md`. `docs/architecture.md` summarizes the current system and links to them. `CLAUDE.md` tells every contributor to read them first.

A decision that the owner has not yet made is still written down, with status **Proposed**, so the options and the card that needs the answer are visible. Code must not depend on a proposed decision.

## Consequences

- Every card that settles a design question adds or updates an ADR in the same pull request.
- The SAD remains the source for requirements and the wider design. When this folder and the SAD disagree, the pull request says so and one of them is fixed.
- Reviewers can reject a change that contradicts an accepted ADR without re-arguing it.
