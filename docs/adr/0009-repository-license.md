# 0009. Repository license

**Status:** Proposed — needs علي's decision
**Date:** 2026-11-12
**Card:** UC-003
**Owner:** علي

## Context

The repository is public on GitHub and has no `LICENSE` file. Without a license, nobody else may legally use or copy the code, which may be the intent; but a public repository without one invites confusion.

## Decision

To be taken. The choice is between keeping the repository **private** (recommended if the code is proprietary), or keeping it public with an explicit **proprietary notice**, or adopting an open-source license.

## Options considered

- **Make the repository private** — simplest if the product is commercial; GitHub Actions minutes remain free for the current usage.
- **Public + "All rights reserved" `LICENSE`** — clarifies intent, keeps Actions minutes free.
- **MIT / Apache-2.0** — only if the platform is meant to be open source.

## Consequences

Until decided, nothing in the repository may be copied from sources whose license would conflict with a later proprietary choice.
