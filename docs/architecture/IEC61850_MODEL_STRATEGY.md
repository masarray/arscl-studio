# IEC 61850 semantic model strategy

## Goal

The engine must understand why SCL elements exist and how they reference one another. XML validity alone is insufficient.

## Semantic coverage map

### File/system
- Header and History
- file role: ICD, IID, CID, SCD, SSD, SED
- edition/namespace detection

### Substation
- Substation
- VoltageLevel
- Bay
- ConductingEquipment
- LNode mappings

### Communication
- SubNetwork
- ConnectedAP
- Address/P
- GSE
- SMV
- IP/MAC/APPID/VLAN semantics

### IED model
- IED
- Services
- AccessPoint
- Server
- LDevice
- LN0/LN
- DOI/SDI/DAI

### Data/service configuration
- DataSet
- FCDA
- ReportControl
- LogControl
- GSEControl
- SampledValueControl
- SettingGroupControl
- Inputs/ExtRef

### Type system
- LNodeType
- DOType
- DAType
- EnumType
- complete forward/reverse reference chains

### Extensions
- Private
- unknown elements/attributes/namespaces
- vendor-specific content preserved even when semantics are unknown

## Reference graph

References are typed edges, not strings scattered around the UI.

Examples:
- ReportControl -> DataSet
- DataSet -> FCDA target
- LNodeType -> DOType
- DOType -> DAType
- DAType -> EnumType
- ConnectedAP -> IED/AccessPoint
- ExtRef -> source object/control block

Each edge has:
- source handle
- target handle or unresolved target key
- reference kind
- resolution status
- source location

Reverse edges power Where Used and impact analysis.

## File role inference

The root SCL element does not fully encode the engineering role of every file in a way that makes extension irrelevant.

Classification uses:
1. trusted extension hint
2. document contents
3. engineering heuristics

Ambiguity remains explicit instead of guessed.

## Edition strategy

Edition-specific knowledge lives behind rule/schema providers, not scattered if statements.

Target direction:
- IEC 61850 Edition 1
- Edition 2
- Edition 2.1 and applicable SCL schema revisions

The repository may only bundle normative schemas/rules that are legally redistributable. Other packs are loaded from user/local providers.

## Smart engine behavior

"Smart" means:
- resolve references
- explain relationships
- know valid object contexts
- understand capability constraints where present
- identify deterministic repair opportunities
- explain impact
- separate fact from heuristic

It does not mean inventing a missing RCB/DataSet/source mapping.

## Deterministic quick fixes

Safe examples:
- remove a proven dangling reference when user approves
- remove a proven unused template
- normalize a deterministic reference after an explicit rename

Unsafe automatic actions:
- guess which DataSet a broken RCB should use
- fabricate an RCB unsupported by the IED
- guess ExtRef publisher
- convert edition semantics without a reviewed transformation plan


## M1B implemented reference policy

The current M1B graph stores **resolved typed edges only**.

Implemented resolved edge categories:
- control block -> DataSet
- FCDA -> referenced Logical Node
- ConnectedAP -> IED / AccessPoint
- ExtRef -> source Logical Node
- LN/LN0 -> LNodeType
- DO/SDO -> DOType
- structured/enum DA/BDA -> DAType / EnumType

Resolution is intentionally uniqueness-sensitive. If a canonical lookup key occurs more than once, that key becomes ambiguous and is not resolved to an arbitrary node.

The next validation layer will model unresolved/ambiguous target keys explicitly and emit source-linked diagnostics. Until then, absence of a resolved graph edge must not be interpreted as proof that no textual reference exists.
