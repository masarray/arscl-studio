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

### M2A — Implemented editing slice

PR #3 delivers direct `IED.desc` editing, compound staging/rollback, bounded patch-only undo/redo and journal, saved-content dirty state, verified atomic Save/Save As, Desktop integration and CLI support. See ADR-0007 and the handoff for limits and exact CI evidence.

### M2B — Validation & reference diagnostics — implemented

PR #4 adds structured unresolved/ambiguous-reference evidence, revision/source-linked diagnostics, explicit schema-provider availability/provenance, latest-wins fast/full validation workers, and Problems source navigation. Well-formed reference-invalid SCL remains inspectable and is never silently repaired.

The default schema provider is deliberately unavailable until a legally sourced schema pack is configured; M2B therefore does **not** claim complete IEC/XSD validation. Identity/delete/DataSet/RCB mutations remain locked.

## M3 — Semantic completeness & engineering diagnostics

M1B already delivered the deep lazy browser, typed reference graph, Where Used and semantic search. M3 extends semantic **coverage and explanations** rather than rebuilding that foundation.

Acceptance:
- complete Substation hierarchy coverage: VoltageLevel, Bay, ConductingEquipment, Terminal and LNode placement;
- richer Communication semantics including P-address parameters and GSE/SMV endpoint linkage;
- supported-services interpretation;
- deeper DOI/SDI/DAI ↔ type-template resolution;
- model/reference/engineering diagnostics for newly covered semantics;
- edition-aware rule/schema provider evolution using legally sourced assets;
- contextual engineering explanations without moving IEC logic into Desktop.

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

