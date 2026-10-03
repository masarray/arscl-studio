# ADR-0002: Syntax is authoritative, semantics are indexed projections

Status: Accepted
Date: 2026-10-03

## Decision

Maintain one authoritative SCL syntax representation and derive semantic/index projections from it.

Do not maintain an independently editable POCO IEC model and XML model.

The syntax layer is preservation-oriented: unknown/vendor elements and attributes, namespaces/prefixes, Private content, comments, processing instructions, ordering, and meaningful whitespace are retained.

"Lossless" in this architecture means no silent loss of engineering semantics or unknown/vendor content. It does not currently promise byte-for-byte lexical identity such as original quote characters, entity spelling, or exact source bytes after an edited serialization.

## Rationale

Two editable models eventually diverge, especially with vendor extensions and raw XML editing.

Semantic objects therefore retain node handles/source links back to syntax.

Exact lexical preservation can be added as a specialized source-text/patch layer later without changing the semantic architecture.

## Consequence

Parsing/index architecture is more deliberate, but round-trip fidelity, XML/Engineering synchronization, diff, diagnostics, and impact analysis become tractable.
