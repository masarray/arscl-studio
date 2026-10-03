# ADR-0002: Syntax is authoritative, semantics are indexed projections

Status: Accepted
Date: 2026-10-03

## Decision

Maintain one authoritative SCL syntax representation and derive semantic/index projections from it.

Do not maintain an independently editable POCO IEC model and XML model.

## Rationale

Two editable models eventually diverge, especially with vendor extensions and raw XML editing.

Semantic objects therefore retain node handles/source links back to syntax.

## Consequence

Parsing/index architecture is more deliberate, but round-trip fidelity, XML/Engineering synchronization, diff, diagnostics, and impact analysis become tractable.
