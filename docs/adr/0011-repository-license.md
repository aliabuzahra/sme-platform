# ADR-0011: Repository license

**Status:** Proposed. Needs the owner (علي).
**Date:** 2026-11-12
**Card:** UC-003

## Context

There is no `LICENSE` file. The repository is public on GitHub, which means others can read and fork it but have no right to use the code. That may be the intent for a commercial product, or it may not.

## Decision (proposed)

Add a `LICENSE` file once the owner chooses. The choice depends on the business, not on engineering.

- Proprietary: add a short "All rights reserved" notice, or make the repository private. This is the usual choice for a commercial SaaS platform.
- Open source: choose a standard license such as Apache-2.0 or MIT.

Until then, no file is added and contributors should assume all rights reserved.

## Consequences

- Nothing in the code changes.
- Pull requests from outside the team should not be accepted until the license is settled.
