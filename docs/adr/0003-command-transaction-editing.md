# ADR-0003: All edits use commands and transactions

Status: Accepted
Date: 2026-10-03

## Decision

Desktop code cannot mutate XML/SCL directly.

Every edit is an Engine command. Multi-patch semantic operations execute inside a transaction with preconditions, impact analysis, validation, commit/rollback, and a single undo history entry.

## Rationale

SCL references are non-local. Deleting or renaming one object can affect Reports, DataSets, ExtRefs, Communication, and type chains.

Atomic semantic commands avoid partial corruption and support a trustworthy change journal.
