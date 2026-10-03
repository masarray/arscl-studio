# ADR-0008: Validation snapshots and reference-resolution diagnostics

Status: Accepted  
Date: 2026-10-03

## Context

M1B built a deterministic typed reference graph, but it retained only resolved edges. A missing target and an ambiguous duplicate identity both appeared as "no edge". That is intentionally insufficient for destructive editing because absence of a resolved edge is not proof that a reference is safe or absent.

M2A added transactions, rollback and safe property editing, but broad identity/reference edits remain locked until validation can explain reference integrity failures and reject stale background results.

IEC schema text also cannot be assumed redistributable. ARSCL must expose schema validation capability without embedding normative assets whose redistribution rights have not been verified.

## Decision

### Reference resolution

Reference construction records two products:

1. resolved typed edges;
2. structured resolution issues for unresolved or ambiguous targets.

Ambiguous identities are never resolved by first-match or arbitrary ordering.

Each issue retains:
- source `SclNodeHandle`;
- typed `SclReferenceKind`;
- unresolved vs ambiguous status;
- reference text;
- expected semantic target kind when known.

### Diagnostic publication

Engine validation converts reference issues into source-linked diagnostics.

A validation snapshot is stamped with the document revision that produced it. Each diagnostic may also carry that revision. UI publication verifies the current session revision again before replacing Problems.

Latest-state validation runs through the existing `LatestWorkCoordinator`:
- `WorkKind.ValidateFast`;
- `WorkKind.ValidateFull`;
- same-kind newer requests cancel/supersede older work;
- stale-revision results are not published.

### Validation domains remain separate

The existing domains remain authoritative:
- XML;
- Schema;
- Semantic;
- Reference;
- Engineering;
- Compatibility;
- Runtime.

A reference-integrity error is not labelled as an XSD violation. A target compatibility warning must not be labelled as IEC validity.

### Schema-provider boundary

`ISclSchemaProvider` is the boundary for legally obtained schema packs.

The default provider is explicitly unavailable. ARSCL does not embed IEC normative schema text merely to make the UI appear complete.

Fast validation may report that a configured schema provider exists while deliberately deferring full XSD work. Full validation invokes the provider only when its status is Available.

Provider status includes a provenance field so future schema packs can document origin/version scope.

### Recovery viewing

A well-formed SCL containing unresolved or ambiguous references remains open and inspectable. Findings are diagnostics; M2B does not silently repair source content.

Malformed XML still follows the existing transactional open rule: parsing fails and the previously committed document remains unchanged.

### Desktop boundary

Desktop receives immutable diagnostics and navigates by stable `SclNodeHandle`. It does not inspect or mutate XML to infer validation results.

## Consequences

- Missing and ambiguous references can no longer disappear behind an absent graph edge.
- Future delete/rename/DataSet/RCB operations can require explicit reference-integrity gates.
- Schema availability is honest and extensible without shipping unverified normative content.
- Validation remains cancellable and revision-safe.
- The graph retains a bounded, document-scoped issue list proportional to reference-resolution failures.
- M2B does not claim complete IEC 61850 validation; rule coverage expands in later semantic/schema milestones.
