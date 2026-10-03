# Implementation strategy

The project advances in vertical, releasable slices. Engine and UI mature together; neither is allowed to become a speculative rewrite.

## M0 — Foundation

Acceptance:
- repository contracts and ADRs
- .NET/Avalonia cross-platform skeleton
- SCL/Engine/Profiles/Desktop/CLI boundaries
- secure streaming document probe
- document session + revision model
- coalescing worker primitive
- baseline tests
- cross-platform CI
- GUI shell matching the intended workstation layout

## M1 — Real SCL viewer

Acceptance:
- open real ICD/IID/CID/SCD/SSD/SED
- classify file/edition
- shallow engineering tree
- XML tree projection
- synchronized selection
- contextual details
- source-linked problems
- background indexing
- responsive large synthetic fixture

## M1B — Accelerated semantic-browser foundation

Completed before M2 to make future editing/reference-impact work deterministic:
- deep lazy Engineering/XML projections
- IED/LDevice/LN/DataSet/control-block semantic hierarchy
- DataType template reference chains
- typed resolved reference graph
- Where Used
- semantic search
- ambiguity-safe identity resolution

The later M3 phase therefore focuses on semantic **completeness, diagnostics, and explanations**, not rebuilding these foundations.

## M2 — Editing kernel

Acceptance:
- command interface
- compound transaction
- undo/redo
- one safe editable property end-to-end
- fast validation after edit
- atomic save
- exact/no-semantic-change round-trip tests

## M3 — Semantic browser

Acceptance:
- IED/Communication/DataSet/Report/Data Model/DataTypes
- typed reference graph
- Where Used
- semantic search
- descriptions/explanations
- dangling reference diagnostics

## M4 — SCL surgery

Acceptance:
- DataSet membership editing
- RCB editing/removal
- Communication editing
- safe rename
- deterministic quick fixes
- impact preview
- change journal

## M5 — Diff/extract/export

Acceptance:
- semantic diff
- XML diff expert view
- extract selected IED
- file-role-aware export
- export preflight

## M6 — Multi-file merge

Acceptance:
- staging workspace
- ICD/IID/CID/SCD inputs
- typed conflict model
- DataType structural fingerprints
- deterministic rename/reference rewrite
- merge preview
- create master SCD

## M7 — Target compatibility

Initial profiles:
- Generic MMS client
- SICAM SCC

Acceptance:
- versioned profile packs
- provenance for every rule
- profile diagnostics separate from IEC diagnostics
- target-aware export preflight
- no "100% compatible" claims without runtime/import verification

## M8 — Advanced/live verification

Candidates:
- ExtRef/GOOSE/SV engineering
- live MMS model comparison
- SCL vs IED reference verification
- repair proposals from live model
- optional reuse/integration of proven IEC 61850 discovery components

## Anti-stall rules

A phase may not expand indefinitely.

Every milestone has:
- bounded acceptance criteria
- fixture/test list
- explicit deferred items
- handoff update
- buildable mainline

If a risky feature blocks a milestone, isolate it behind an interface/feature flag and complete the rest. Do not leave the repository unbuildable while experimenting.
