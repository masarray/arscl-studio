# Project handoff

Last updated: 2026-10-03

## Current phase

M0 — Foundation, active.

Branch: `foundation/m0-architecture`

Last code commit verified by cross-platform CI before this documentation update:
`55981e2f5b0b68b14f2fd940ba5ee85009c31fb7`

Verified CI run:
`37093403688` — Windows, Ubuntu, and macOS build + tests all passed.

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

Projects now exist:
- `ArSclStudio.Scl`
- `ArSclStudio.Engine`
- `ArSclStudio.Profiles`
- `ArSclStudio.Desktop`
- `ArSclStudio.Cli`

Engine is headless. Desktop never edits XML directly.

Authoritative source is the SCL syntax document; semantic models are indexed projections rather than independently serialized copies.

## Implemented M0 subset

### Repository governance
- strict `AGENTS.md`
- architecture/performance/reliability/IEC model/GUI documents
- ADRs for Avalonia, source-of-truth, transactions, workers, virtualization, and validation-vs-compatibility
- implementation roadmap and research notes

### SCL foundation
- secure XML reader defaults
- DTD prohibited
- external resolver disabled
- configurable maximum document size
- lightweight SCL probe
- extension-based file-kind hint
- Header/schema revision probe
- canonical identity primitives

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
- virtualizing list baseline rather than recursive control tree

### Tooling
- headless CLI using the same SCL layer
- MSTest baseline
- cross-platform GitHub Actions matrix

## Reliability/performance commitments

The architecture is already committed to:
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

These are contracts. Some are not implemented yet; do not claim otherwise.

## Known limitations

The current GUI uses demonstration rows to prove shell/layout only; it is not wired to a real semantic tree yet.

The SCL probe is not the full lossless document loader.

The following are deliberately not implemented yet:
- semantic reference graph
- schema validation packs
- complete IEC object model/index
- editing transactions/undo
- real save/export
- semantic diff/merge
- SICAM compatibility execution
- live MMS verification

## Immediate next acceptance target

Finish the remainder of M0 and begin M1 in controlled vertical slices:

1. implement lossless document-load abstraction with source/node handles
2. create shallow semantic index for root/Header/Substation/Communication/IED/DataTypeTemplates
3. replace static ExplorerRows with a flattened virtualized projection backed by the loaded document
4. wire Open file -> Session -> probe/load -> explorer
5. source-linked parse diagnostics
6. selection service shared by Engineering and XML projections
7. large synthetic fixture and open/close leak/performance test
8. preserve the same green Windows/Linux/macOS CI baseline

## Continuation rule

Before changing implementation:
1. read `AGENTS.md`
2. read architecture ADRs
3. inspect this handoff
4. confirm current branch/CI
5. continue the current milestone instead of redesigning completed decisions without evidence
