# Project handoff

Last updated: 2026-10-03

## Current phase

**M1B — Deep IEC Semantic Browser & Lazy Tree: COMPLETE**

Implementation branch:
`feature/m1b-deep-semantic-browser`

Pull request:
`#2 — M1B: deep IEC semantic browser and lazy tree`

Latest verified code commit before this documentation update:
`63489b148aeddf8bc86ca17fdaae936a7afbef10`

Verified cross-platform CI run:
`37105965479`

Result:
- Windows: build + tests passed
- Ubuntu: build + tests passed
- macOS: build + tests passed

## Product intent

ARSCL Studio is a cross-platform Avalonia IEC 61850 SCL viewer/editor/validator/surgery workbench. The primary use case is preparing trustworthy SCL for HMI/workstation/gateway import, especially when engineers must inspect or repair multi-vendor files without the original vendor engineer.

## M1B architecture

The authoritative document remains the preservation-oriented SCL syntax tree.

M1B adds a lightweight semantic layer and typed reference graph over the same stable `SclNodeHandle` identities:

```text
SclSyntaxDocument
        |
        +--> SclSemanticIndex
        |      +--> IEC semantic hierarchy
        |      +--> lightweight immutable semantic nodes
        |      +--> typed resolved reference graph
        |
        +--> Engineering lazy projector
        +--> XML lazy projector
        +--> source-linked details
```

There is still only one editable source of truth.

## M1B completed scope

### Deep Engineering hierarchy

The semantic browser now recognizes and navigates:
- SCL document
- Header
- Substation
- Communication
- SubNetwork
- ConnectedAP
- Address
- IED
- Services
- AccessPoint
- Server
- LDevice
- LN0 / LN
- DataSet
- FCDA
- ReportControl
- LogControl
- GSEControl
- SampledValueControl
- Inputs
- ExtRef
- SettingControl
- DOI
- SDI
- DAI
- DataTypeTemplates
- LNodeType
- DOType
- DAType
- EnumType
- DO
- SDO
- DA
- BDA
- Private/vendor extension nodes

Semantic rows continue to point to the original syntax node handle.

### Lazy flattened trees

Engineering and XML explorers now use explicit expanded-handle sets.

Only visible branches become row projections.

The GUI does not construct a recursive Avalonia tree or one ViewModel per document node.

Expand/collapse is implemented for both Engineering and XML views.

Selecting a deep result can expand the ancestor path so Engineering/XML navigation remains synchronized.

### DataSet and control-block relationships

The typed graph resolves, when identity is unambiguous:
- ReportControl -> DataSet
- LogControl -> DataSet
- GSEControl -> DataSet
- SampledValueControl -> DataSet
- FCDA -> referenced Logical Node
- ConnectedAP -> IED
- ConnectedAP -> AccessPoint
- ExtRef -> referenced source Logical Node

Reference text such as DO/DA identity is retained on graph edges where applicable.

### DataType reference chains

The graph resolves:
- LN/LN0 `lnType` -> LNodeType
- DO/SDO `type` -> DOType
- DA/BDA with `bType="Struct"` -> DAType
- DA/BDA with `bType="Enum"` -> EnumType

These are case-sensitive identity lookups.

### Ambiguity policy

Reference resolution never chooses the first match when identity is duplicated.

Duplicate/ambiguous identities are removed from the unique-resolution index, so dependent references remain unresolved rather than being attached to an arbitrary target.

This currently applies to:
- type IDs
- IED names
- AccessPoint identities
- Logical Device identities
- Logical Node identities
- DataSet identities

A later validation milestone will turn unresolved/ambiguous identities into explicit diagnostics and quick-fix/impact workflows.

### Where Used

The right-side `Where Used` view is now functional for resolved graph edges.

Examples:
- selecting a DataSet shows Report/GOOSE/SV/Log controls that reference it
- selecting an IED or AccessPoint can show ConnectedAP bindings
- selecting a type template shows resolved semantic users
- selecting a Logical Node can show FCDA/ExtRef references

Selecting a Where Used result navigates back to the source object.

### Semantic search

Search now operates on the semantic index rather than traversing Avalonia rows.

Properties:
- 180 ms UI debounce
- Engine `WorkKind.Search`
- latest-wins coalescing
- cancellation
- document-revision stale-result protection
- background execution
- capped result publication
- result navigation expands the semantic/XML ancestor path

Search currently indexes semantic display identity, badge/context, and semantic kind.

### Semantic paths

Context/search paths now prefer IEC identity where available.

Examples:
- `IED[Relay_A]`
- `LDevice[Protection]`
- `LN[XCBR1]`

rather than presenting only generic XML tag names.

### Performance / regression gates

Added:
- deep lazy-projection regression with 100 Logical Devices x 100 Logical Nodes
- assertion that a collapsed model does not materialize the full semantic tree as visible rows
- existing large 5,000-IED load/projection gate remains active
- session collectability/leak regression remains active

### GUI

The Avalonia shell now exposes:
- real expandable Engineering tree
- real expandable XML tree
- semantic search
- clickable search results
- functional Where Used
- exact source location/context
- semantic object/reference counts in status
- virtualized lists for tree/search/reference panes

Editing remains intentionally disabled.

## Tests at M1B baseline

Coverage now includes:
- secure XML / DTD rejection
- preservation of vendor/private content
- real open/failed-open transaction behavior
- session collectability
- worker cancellation/coalescing/stale-result rejection
- deep semantic hierarchy
- DataSet/control-block reference graph
- ConnectedAP bindings
- ExtRef source resolution
- LN -> LNodeType
- DO -> DOType
- DA -> EnumType/DAType paths
- duplicate type identity must not be guessed
- Engineering/XML stable handle identity
- lazy XML expansion
- lazy Engineering expansion
- semantic Where Used
- semantic search
- large/deep model projection regression

## Known limitations

M1B is a semantic browser/reference foundation, not yet a complete IEC validator.

Still pending:
- full Substation VoltageLevel/Bay/ConductingEquipment engineering hierarchy
- detailed service-capability interpretation below Services
- Communication P/IP/MAC/APPID/VLAN semantic decoding
- GSE/SMV communication endpoint semantic linking
- DOI/SDI/DAI-to-template instance binding for every nested case
- explicit unresolved/ambiguous reference diagnostics
- schema/NSD/rule-pack validation
- edition-specific semantic rule packs
- transaction editing
- undo/redo
- save/export
- semantic diff/merge
- SICAM compatibility execution
- live MMS verification

Unknown/vendor XML remains available in XML View and preserved by the syntax layer even when not semantically interpreted.

## Next milestone

**M2 — Editing Kernel**

Recommended first slice: **M2A — Transaction Kernel & Safe Property Editing**

Acceptance target:
1. command interface with explicit preconditions
2. document write/exclusive transaction boundary
3. compound transaction
4. rollback on failure
5. undo / redo
6. redo invalidation after a new edit
7. change journal
8. one safe property edit end-to-end through Engine, never direct XML from Desktop
9. fast post-edit integrity validation
10. dirty-state/document revision integration
11. atomic save to temporary output
12. production-parser reopen before replacement
13. no-edit and edited round-trip preservation tests
14. Windows/Linux/macOS CI green

Do not begin destructive SCL surgery until the transaction/undo/reference-impact foundation is proven.

## Continuation rule

Before changing implementation:
1. read `AGENTS.md`
2. read architecture ADRs
3. read this handoff
4. confirm current main/PR/CI state
5. begin M2 through Engine transactions rather than adding XML mutation to ViewModels
