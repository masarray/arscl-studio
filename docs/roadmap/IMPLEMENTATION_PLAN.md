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

### M3A0 — IEC 61850 workstation information architecture — implemented

The corrective workstation layer now exposes real Engine-backed workspaces for:
- IED overview;
- Network;
- GOOSE/GSSE;
- DataSets;
- Reports & Logs;
- Data Model;
- Setting Groups.

Key semantic expansion delivered in M3A0:
- Address/P network projection;
- Communication/GSE semantic endpoints;
- deterministic Communication/GSE ↔ GSEControl binding;
- GOOSE DataSet/member/subscriber engineering projection;
- LN → LNodeType → DO → DOType → DA/SDO → DAType/BDA model resolution;
- DOI/SDI/DAI instance overlay;
- SettingControl and FC=SG value/unit/bound projection.

The current real golden SCD has no Substation section, so ARSCL does not expose a fake Substation workspace merely to complete the UI.

See:
- `docs/ux/IEC61850_WORKSTATION_INFORMATION_ARCHITECTURE.md`;
- `docs/testing/REAL_SCD_ENGINEERING_WORKSPACE_ACCEPTANCE.md`;
- `docs/testing/M3A0_ACCEPTANCE.md`.

### M3A — semantic/engineering diagnostics & remaining coverage — slices 1-2 implemented

Acceptance:
- typed GOOSE endpoint engineering diagnostics: implemented;
- Communication/SMV typed endpoint linkage: implemented;
- SMV endpoint engineering diagnostics: implemented;
- deep DOI/SDI/DAI ↔ type-template consistency with ambiguity-safe resolution: implemented;
- cached type-context performance guard for large repeated instance models: implemented;
- per-IED Services projection and conservative Fix/Conf/Dyn/limit interpretation: implemented;
- unknown/future Services declarations preserved as uninterpreted, source-linked rows: implemented;
- explicit GOOSE/GSSE/SMV publisher counts vs literal Services max diagnostics: implemented;
- add edition-aware rule/schema-provider evolution using legally sourced assets;
- expand contextual engineering explanations and diagnostic grouping without moving IEC logic into Desktop;
- add additional cross-domain rules only when their semantics are proven by specification/evidence;
- add Substation → VoltageLevel → Bay → ConductingEquipment → Terminal/LNode placement only when a real fixture is available;
- preserve all M1B/M2A/M2B/M3A0/M3A1/M3A2 regression and performance gates.

M3A2 acceptance evidence:
- `docs/testing/M3A2_MODEL_SERVICES_ACCEPTANCE.md`.

Broad destructive editing remains locked until the affected domain has explicit reference-impact and validation coverage.

### M3UX1 — engineering desktop density & inspection workflow — implemented

This visual/interaction slice corrects the remaining dashboard/web-like presentation without changing IEC semantics:
- compact docked Project Explorer / engineering work area / Inspector;
- dense IED rows instead of card-like entries;
- compact workspace tabs and command strip;
- Data Model column reallocation and true depth indentation;
- property-grid-style Inspector and compact Where Used;
- vertically resizable Problems/Search/Changes dock;
- no loss of list virtualization or shared selection semantics.

Manual Windows/high-DPI screenshot acceptance remains the merge gate. See `docs/testing/M3UX1_ENGINEERING_DESKTOP_ACCEPTANCE.md`.

### M3UX2 — screenshot-driven workstation refinement — implemented

Windows screenshots of M3UX1 identified remaining web/dashboard behavior. M3UX2 delivers:
- forced single-line engineering workspace selector with stable short task names;
- compact Overview summary + full-height Services table;
- explicit Setting Groups empty-state;
- severity/domain/text filtering in Problems with preserved source navigation;
- true property-grid Inspector rows including Namespace and long-value tooltips;
- Data Model hierarchy-width prioritization;
- shared subtle row separators for engineering tables.

Evidence: `docs/testing/M3UX2_SCREENSHOT_REFINEMENT_ACCEPTANCE.md`.

### M3UX3 — engineer mental-workspace finishing — implemented

This finishing slice closes the remaining navigation/context gap without rebuilding the M3 shell:
- compact selected-IED context + left engineering workspace navigator;
- persistent IED/domain/object breadcrumb;
- workspace-aware Problems/Search/Where Used navigation;
- Reports bound-DataSet member inspection;
- Desktop-only collapsible Data Model projection;
- selected-IED Network scope;
- functional Ctrl+F;
- removal of roadmap-only disabled controls from the active workspace.

All Engine semantic boundaries, virtualization, SclNodeHandle navigation, and mutation locks remain intact.

Evidence: `docs/testing/M3UX3_ENGINEER_WORKSPACE_ACCEPTANCE.md`.

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

