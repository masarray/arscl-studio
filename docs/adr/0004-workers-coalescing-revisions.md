# ADR-0004: Revision-aware workers and coalescing

Status: Accepted
Date: 2026-10-03

## Decision

Document sessions carry a monotonic revision.

Background results can publish only if their source revision still matches the current session revision.

Latest-state tasks such as search and fast validation are coalesced and cancel obsolete work. Explicit save/export tasks are must-run and separately serialized.

## Rationale

Large models require background work, but unbounded queued tasks cause latency, memory growth, and stale UI state.
