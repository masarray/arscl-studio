# ARSCL Studio architecture

## Product shape

ARSCL Studio is an IEC 61850 semantic engineering workbench, not a generic XML editor.

Primary workflow:

    Open -> Understand -> Navigate -> Edit/Surgery
         -> Impact Analysis -> Validate -> Target Check -> Export

The application exposes the same SCL source through synchronized projections:
- Engineering Explorer
- XML tree
- contextual details/editor
- references/where-used
- problems
- signal basket
- semantic diff

## Layers

### SCL layer

Owns syntax and semantic primitives.

The long-term design is a lossless syntax store plus semantic indexes. A semantic object never becomes an independently serialized second source of truth.

Key concepts:
- SclNodeHandle: cheap runtime node identity
- SclSemanticKey: canonical IEC identity
- SclSourceSpan: line/offset information
- SclDocumentMetadata: detected file kind/edition
- semantic indexes keyed by handle and canonical key
- reference graph with typed edges and resolution state

### Engine layer

Owns behavior:
- SclDocumentSession
- document revision
- worker orchestration
- transactions
- command history
- diagnostics
- search
- merge/diff
- compatibility execution
- export

Every asynchronous result carries the source revision. The session rejects stale results.

### Profiles layer

Compatibility is a separate axis from IEC validity.

A target rule states:
- profile/product
- version scope
- rule id
- severity
- evidence/provenance
- confidence
- remediation guidance

The profile can say "known importer risk" without falsely saying "IEC violation."

### Desktop layer

Avalonia UI only.

The desktop consumes immutable/batched projections from Engine. It owns no XML mutation and no IEC standard logic.

## Document lifecycle

    file
      -> secure probe
      -> metadata detection
      -> syntax load
      -> shallow semantic index
      -> interactive UI
      -> background reference/search indexes
      -> fast validation
      -> full validation

The UI should become useful before every secondary index has finished.

## Selection/navigation

All panels communicate through stable node handles.

    Explorer selection
      -> SelectionService
      -> DetailsHost
      -> Context
      -> References
      -> XML projection

A diagnostic navigates through the same NavigationService. No panel performs custom tree walking.

## Editing

High-level command:

    RemoveReportControlCommand

may create multiple low-level patches but is one user transaction.

Transaction stages:
1. analyze references and impact
2. validate preconditions
3. apply patches to working syntax
4. update affected indexes
5. run required validation
6. commit or rollback
7. publish one coherent change batch

## Save/export

Never serialize directly over the original file.

A temporary result is written, reopened by the same parser, validated, and only then moved/replaced atomically.

## Why Avalonia

Cross-platform desktop is a first-class requirement. Avalonia 12 is selected for Windows, Linux, and macOS desktop support while preserving a native desktop engineering workflow.

The architecture deliberately prevents Avalonia types from leaking into SCL/Engine so a future alternative front end does not require rewriting the engine.
