# Project handoff

Last updated: 2026-10-03

## Current phase

M0 — Foundation, with the first M1 document-loading slice already landed.

Branch: `foundation/m0-architecture`

Draft PR: #1 — `M0 foundation: Avalonia architecture, reliability contracts, and engine skeleton`

Latest code commit verified by cross-platform CI before this documentation update:
`324f98befdf499ead9adec9effb8cebbfb9b824b`

Verified CI run:
`37093777256` — Windows, Ubuntu, and macOS build + tests all passed.

## Product intent

ARSCL Studio is a cross-platform Avalonia IEC 61850 SCL viewer/editor/validator/surgery workbench. The primary use case is preparing trustworthy SCL for HMI/workstation/gateway import, especially when engineers must inspect or repair multi-vendor files without the original vendor engineer.

## UX direction

Do not build a dashboard-style generic IDE.

The accepted direction combines:
- IEDScout-style IEC navigation: IED, DataSets, Reports, GOOSE, Data Model, contextual details
- XML Notepad-style structured editing: synchronized tree/value concept, validation list, search, undo/redo, schema-aware editing
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

Engine is headless. Desktop never edits XML directly.

Authoritative source is the SCL syntax document; semantic models are indexed projections rather than independently serialized copies.

## Implemented foundation

### Repository governance
- strict `AGENTS.md`
- architecture/performance/reliability/IEC model/GUI documents
- ADRs for Avalonia, source-of-truth, transactions, workers, virtualization, and validation-vs-compatibility
- implementation roadmap and research notes

### Secure/preservation-oriented SCL loading
- secure XML defaults
- DTD prohibited
- external resolver disabled
- configurable maximum document size
- lightweight SCL probe
- asynchronous cancellable DOM construction
- whitespace/comments/processing instructions retained
- vendor namespace prefixes, unknown elements/attributes, and Private content retained
- source line/column captured for selectable nodes
- stable runtime `SclNodeHandle` registry
- immutable on-demand node-info projection
- XML snapshot only through an explicit API, not exposed mutable DOM to Desktop
- top-level semantic index for Header/Substation/Communication/IED/DataTypeTemplates/Private

Important fidelity wording:
- the loader targets engineering/semantic/vendor-content preservation
- byte-for-byte lexical identity (attribute quote style, entity spelling, original formatting bytes) is not currently promised
- no-edit save can later preserve the original bytes as an optimization; edited export will be validated for semantic/vendor-content fidelity

### Engine foundation
- document revision
- session lifecycle
- bounded latest-work coordinator
- cancellation
- same-kind latest-wins coalescing
- stale revision rejection
- structured diagnostics primitives

### UI foundation
- Avalonia 12 desktop shell
- compiled bindings enabled
- dense engineering layout
- Engineering/XML explorer placement
- contextual center editor
- Context/Where Used pane
- Problems/References/Changes/Signal Basket/Diff bottom tool area
- virtualizing-list baseline rather than recursive control tree

### Tooling
- headless CLI using the same SCL layer
- MSTest baseline
- cross-platform GitHub Actions matrix

## Reliability/performance commitments

The architecture is committed to:
- secure XML defaults
- document revisions
- stale worker result rejection
- coalesced latest-state work
- bounded concurrency
- cancellation
- document-scoped caches/pools
- explicit disposal
- virtualized flattened explorer
- compiled Avalonia bindings
- transactional save/export
- vendor/private XML preservation
- round-trip gates

Some contracts, especially editing transactions and transactional export, are not implemented yet; do not claim otherwise.

## Known limitations

The current GUI uses demonstration rows to prove shell/layout only; it is not yet wired to the loaded SCL model.

The top-level index is intentionally shallow. The complete IEC reference graph and type/data model indexes are still pending.

The following are deliberately not implemented yet:
- full IEC semantic reference graph
- schema/OCL validation packs
- complete IED/LN/DO/DA/type indexes
- editing transactions/undo
- real save/export
- semantic diff/merge
- SICAM compatibility execution
- live MMS verification

## Immediate next acceptance target

Continue M1 in controlled vertical slices:

1. create a real `Open file -> SclDocumentSession -> SclDocumentLoader -> top-level index` Engine pipeline
2. replace static ExplorerRows with a flattened virtualized projection backed by real node handles
3. synchronize Engineering and XML selection through a SelectionService
4. expose contextual details from immutable semantic projections
5. route parse/load failures to source-linked Problems
6. add large synthetic SCL fixture and timing/allocation regression baseline
7. add repeated open/close collectability test for session-owned state
8. preserve the green Windows/Linux/macOS CI baseline

## Continuation rule

Before changing implementation:
1. read `AGENTS.md`
2. read architecture ADRs
3. inspect this handoff
4. confirm current branch/PR/CI
5. continue the current milestone instead of redesigning completed decisions without evidence
