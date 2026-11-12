# ADR-0010: Classic `.sln` alongside `slnx`

**Status:** Proposed. Default is no; accept only if a tool needs it.
**Date:** 2026-11-12
**Card:** UC-003

## Context

ADR-0002 chose `slnx`. Some older tools, extensions and scripts read only the classic `.sln` format. Keeping both means keeping them in sync by hand.

## Decision (proposed)

No classic `.sln` is added. If a tool the team must use cannot open `slnx`, the person who hits it opens a pull request that adds `Sme.Platform.sln`, names the tool in this record, and adds a CI step that fails when the two files list different projects.

## Consequences

- One solution file to maintain today.
- The cost of adding the second file is paid only when a concrete tool needs it.
