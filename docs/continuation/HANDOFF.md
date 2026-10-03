# Project handoff

Last updated: 2026-10-03

## Current phase

**M1A — Real Document Workspace: COMPLETE**

Branch used for implementation: `foundation/m0-architecture`

Draft PR: #1 — `M0 foundation: Avalonia architecture, reliability contracts, and engine skeleton`

Latest verified code commit before this documentation update:
`6f95690288d321a8d05ac8d32c4c1a299843cac9`

Verified cross-platform CI run:
`37098224357`

Result:
- Windows: build + tests passed
- Ubuntu: build + tests passed
- macOS: build + tests passed

## Product intent

ARSCL Studio is a cross-platform Avalonia IEC 61850 SCL viewer/editor/validator/surgery workbench. The primary use case is preparing trustworthy SCL for HMI/workstation/gateway import, especially when engineers must inspect or repair multi-vendor files without the original vendor engineer.

## UX direction

Do not build a dashboard-style generic IDE.

The accepted direction combines:
- IEDScout-style IEC navigation: IED, DataSets, Reports, GOOSE, Data Model, contextual details
- XML Notepad-style structured editing: synchronized structural navigation, validation list, search, undo/redo, schema-aware editing
- ARSCL-specific semantic surgery, impact analysis, merge/diff, compatibility profiles

See `docs/architecture/GUI_UX.md`.

## Architecture direction

C# / .NET 10 / Avalonia 12.

Projects:
- `ArSclStudio.Scl`
- `ArSclStudio.Engine`
- `ArSclStudio.Profiles`
- `ArSclStudio.Desktop`
- `ArSclStudio.Cli`

Engine is headless. Desktop does not mutate XML directly.

The authoritative source is the SCL syntax document. Semantic objects and GUI rows are indexed/projection views over stable `SclNodeHandle` identities.

## M1A completed scope

### Real document workspace

The application now has a real end-to-end path:

```text
Open SCL
  -> SclDocumentSession
  -> secure SclDocumentLoader
  -> SclTopLevelIndexer
  -> immutable explorer projections
  -> Avalonia virtualized lists
  -> shared SclNodeHandle selection
  -> contextual details / source location / Problems
```

The previous static demonstration data has been removed from the main workspace.

### Session ownership and reliability

`SclDocumentSession` now owns the committed document state.

Implemented behavior:
- monotonic document revision
- latest-wins parse work
- cancellation and stale-result rejection
- successful load is committed atomically to the session
- malformed/new-file open failure does not destroy the previously committed document
- disposal clears the committed document state after owned worker shutdown
- close/dispose collectability is covered by a regression test

### Secure/preservation-oriented SCL loading

Implemented:
- DTD prohibited
- external resolver disabled
- configurable maximum document size
- asynchronous cancellable DOM construction
- whitespace/comments/processing instructions retained
- vendor namespace prefixes retained
- unknown elements and attributes retained
- `Private` content retained
- source line/column captured for selectable nodes
- stable runtime `SclNodeHandle`
- read-only node-info queries
- attribute queries without exposing mutable XML to Desktop

Important fidelity wording:
- the loader targets engineering/semantic/vendor-content preservation
- byte-for-byte lexical identity such as original quote characters/entity spelling is not currently promised
- no-edit save may later preserve source bytes directly
- edited export must pass semantic/vendor-content round-trip gates

### Real Engineering explorer

The Engineering view is now generated from the loaded file.

Current semantic top-level coverage:
- document/root
- Header
- Substation
- Communication
- IED collection and IED identity/manufacturer
- DataTypeTemplates
- Private/extensions

Rows are immutable projections and use stable node handles.

No recursive Avalonia control tree is created.

### Real XML explorer

The XML view is generated from the same authoritative syntax document and uses the same `SclNodeHandle` identities as Engineering view.

M1A intentionally exposes the root and first visible level only. Deep lazy expansion is the first M1B task.

### Shared selection and contextual details

Engineering and XML projections synchronize through `SclSelectionService`.

Selecting a real node can resolve:
- node identity
- semantic/XML kind
- semantic path
- source file + line/column
- namespace
- syntax value where applicable
- compact IEC-context explanation for currently supported top-level kinds

No panel performs an independent string search to identify the selected object.

### Problems / failed-open behavior

Malformed XML produces structured diagnostics with:
- diagnostic code
- severity
- domain
- source path
- parser line/column
- explanation

A failed open leaves the current valid document unchanged.

### Avalonia workspace

The Desktop shell is now wired to real SCL data:
- native file picker for ICD/IID/CID/SCD/SSD/SED/XML
- Engineering and XML tabs
- virtualized row lists
- synchronized selection
- contextual center pane
- context pane
- source location
- Problems pane
- real document metadata/status

Save/Edit/Merge/Extract/Export remain intentionally disabled until their engine contracts exist.

### Performance and lifetime gates

Added a large synthetic regression fixture with 5,000 IEDs.

The test validates real load + semantic top-level projection within a generous CI regression budget rather than making an unsupported performance marketing claim.

A collectability test verifies that a disposed session no longer roots its committed `SclSyntaxDocument`.

## Tests at M1A baseline

Coverage includes:
- secure DTD rejection
- SCL/file-role probing
- vendor/private/comment/prefix preservation
- stable node/source mapping
- top-level semantic indexing
- worker coalescing
- stale-revision rejection
- real file open and commit
- failed-open rollback behavior
- Engineering/XML handle identity
- shared selection service
- large explorer regression
- disposed-session collectability

## Known limitations

M1A is a real viewer foundation, not yet the complete IEC semantic browser.

Still pending:
- deep XML lazy expand/collapse
- IED -> AccessPoint -> Server -> LDevice -> LN hierarchy
- DOI/SDI/DAI engineering projection
- DataSet/FCDA semantic member resolution
- ReportControl/LogControl semantic browser
- GSEControl/SampledValueControl semantic browser
- Inputs/ExtRef semantic browser
- setting groups
- complete DataTypeTemplates reference resolution
- typed forward/reverse reference graph
- Where Used
- semantic search
- schema/OCL/rule validation packs
- editing transaction kernel / undo-redo
- save/export
- semantic diff/merge
- SICAM compatibility execution
- live MMS verification

## Next milestone

**M1B — Deep IEC Semantic Browser & Lazy Tree**

Acceptance target:

1. implement lazy expand/collapse for XML without materializing the whole tree
2. implement semantic IED hierarchy:
   - Services
   - AccessPoint
   - Server
   - LDevice
   - LN0 / LN
3. expose configured DataSets and FCDA members
4. expose ReportControl / LogControl
5. expose GSEControl / SampledValueControl
6. expose Inputs / ExtRef and SettingGroupControl
7. build DataTypeTemplates indexes and resolve LN/DO/DA type chains
8. introduce typed forward/reverse reference graph
9. make Where Used functional for covered object types
10. add debounced/coalesced semantic search
11. preserve virtualized/lazy behavior on large fixtures
12. keep Windows/Linux/macOS CI green

## Continuation rule

Before changing implementation:
1. read `AGENTS.md`
2. read architecture ADRs
3. read this handoff
4. confirm current main/branch/PR/CI state
5. continue M1B from this baseline rather than replacing the architecture without profiler/test evidence
