# IEC 61850 workstation information architecture

Status: M3A0 implemented workstation contract (Substation deferred)  
Date: 2026-10-03

## Why M3A0 exists

The first ARSCL desktop shell exposed the semantic index almost directly as a tree. Although the semantic index was already richer than raw XML, the resulting user experience was still shaped by SCL serialization order:

- vendor `Private` content could appear beside major engineering sections;
- `SCL / IED / AccessPoint / Server / LDevice` paths were treated as primary navigation;
- source path, namespace and source line occupied the main content area;
- generic detail panels were reused for unrelated IEC 61850 domains;
- empty visual tabs suggested workspaces that did not yet exist.

That is not the mental model used by protection, SCADA or substation-automation engineers.

M3A0 corrects the information architecture before additional semantic/editing work expands.

## Reference observations

The corrective layout is informed by the supplied Siemens IEC 61850 System Configurator and Omicron IEDScout screenshots.

The references use different visual styles but share the same engineering principle:

1. choose a domain/task context;
2. navigate IEDs or the domain hierarchy;
3. show domain-specific tabular content;
4. show properties/relationships for the selected engineering object;
5. keep file/XML mechanics secondary.

Examples visible in the supplied references include:

- Siemens: Devices, Substation, Network, GOOSE, Reports and logs;
- IEDScout: IED selector followed by GOOSE, Reports, Setting Groups, DataSets and Data Model;
- network views expose IP/subnet/gateway context;
- GOOSE views expose DataSet members, destination/subscriber context and communication parameters;
- report views expose DataSet content and RCB properties;
- Data Model views expose LN/DO/DA hierarchy and FC/value context;
- Setting Groups expose configured setting values rather than XML nodes.

ARSCL does not copy either product's skin. It adopts the domain-oriented information architecture.

## ARSCL target workspaces

The desktop target is:

`IEDs | Network | GOOSE | Reports & Logs | DataSets | Data Model | Setting Groups | Substation | XML`

### IEDs

Default landing workspace after opening SCL.

Navigator:
- IED name;
- manufacturer;
- description;
- compact counts.

Content:
- identity;
- AccessPoint count;
- LogicalDevice count;
- LogicalNode count;
- DataSet count;
- Report/Log controls;
- GOOSE controls;
- Setting Groups.

The first selected object after opening a multi-IED SCL is an IED, not the SCL XML root.

### Network

Target content:
- SubNetwork;
- ConnectedAP;
- IED/AP;
- IP;
- subnet mask;
- gateway;
- OSI/transport parameters where present;
- GSE/SMV communication endpoints.

Requires semantic support for Address/P and communication endpoint elements. Do not hard-code XML parsing in Desktop.

### GOOSE

Target content:
- publisher IED/LD/LN0/GSEControl;
- bound DataSet;
- FCDA members with CDC/FC context;
- destination MAC;
- APPID;
- VLAN ID/priority;
- MinTime/MaxTime where present;
- subscriber/ExtRef relationships when resolvable.

### Reports & Logs

Target content:
- RCB type buffered/unbuffered;
- DataSet;
- rptID;
- confRev;
- buffer time;
- trigger options;
- optional fields;
- client reservation/instance information where represented by SCL.

### DataSets

Target content:
- IED / LD / LN;
- DataSet identity;
- FCDA members;
- FC;
- resolved LN/DO/DA display;
- Where Used by Report/GOOSE/SV/Log.

### Data Model

Target hierarchy:
- IED;
- LogicalDevice;
- LogicalNode;
- DataObject;
- DataAttribute.

Content must resolve instance data and DataTypeTemplates into an engineering model. Raw type IDs are secondary.

### Setting Groups

Target content:
- SettingControl;
- number of groups;
- active/edit group where represented;
- FC=SG data;
- configured values and units where resolvable.

### Substation

Target hierarchy:
- Substation;
- VoltageLevel;
- Bay;
- ConductingEquipment;
- Terminal/connectivity;
- LNode placement.

This requires M3 semantic expansion before the workspace is enabled.

### XML

Expert/source view only:
- complete preservation-oriented XML tree;
- vendor/private extensions;
- source positions;
- namespace;
- exact attributes/text.

XML is authoritative for preservation but not the default engineering UX.

## Architectural rules

1. Desktop does not discover IEC meaning by walking XML.
2. Every enabled engineering workspace is backed by an Engine projector with tests.
3. A workspace is not enabled merely because a tab can be drawn.
4. Unknown/vendor XML stays preserved and visible in XML expert view.
5. Projectors return stable `SclNodeHandle` identities for cross-navigation.
6. Problems, search, Where Used and workspace selection share one selection service.
7. Domain projections remain revision-bound and must not publish stale state.
8. Large-file workspaces remain lazy/virtualized where full materialization is unnecessary.
9. Target compatibility (for example SICAM SCC) is a separate overlay, not a substitute for IEC semantics.
10. No destructive editing is enabled until the corresponding reference/impact model is complete.

## Corrective slices

### M3A0.1 — IED workspace — implemented

- real IED projection;
- default IED selection;
- IED-centric navigator;
- engineering counts;
- Model Tree/XML demoted to secondary views.

### M3A0.2 — Network workspace — implemented

- semantic Address/P model;
- SubNetwork/ConnectedAP projection;
- IP/subnet/gateway property table;
- OSI AP-title/AE-qualifier retained in the Engine projection;
- communication navigation stays source-linked.

### M3A0.3 — DataSets + Reports — implemented

- DataSet catalog/member table;
- FCDA LD/LN/DO/DA/FC engineering projection;
- typed DataSet usage count through the reference graph;
- BRCB/URCB classification;
- rptID/confRev/bufTime/intgPd/RptEnabled presentation;
- readable TrgOps and OptFields summaries;
- shared source navigation.

### M3A0.4 — GOOSE — implemented

- Communication/GSE is a typed semantic object;
- deterministic Communication/GSE ↔ GSEControl reference binding using IED + LD + control name;
- publisher, bound DataSet and FCDA signals;
- MAC/APPID/VLAN/priority and MinTime/MaxTime;
- ExtRef subscriber matching by source signal identity, not guessed control-block names;
- GSSE remains distinct and is not given a fake Ethernet GSE endpoint.

### M3A0.5 — Data Model + Setting Groups — implemented

Data Model:
- IED-scoped LD/LN selector;
- LN → LNodeType → DO → DOType → DA/SDO → DAType/BDA resolution;
- DOI/SDI/DAI instance-value overlay;
- CDC/FC/bType/value/type-id presentation;
- source navigation points to the instance when present and to the template definition otherwise.

Setting Groups:
- SettingControl projection;
- FC=SG leaf filtering over the resolved Data Model;
- configured value, unit, min/max/step and type presentation;
- structured setting values such as setMag.f are supported;
- unit/multiplier are joined from sibling units metadata rather than guessed from names.

### M3A0.6 — Substation — deferred by evidence

The current golden SCD contains no <Substation> section. ARSCL therefore does not expose a fake or empty primary-system workspace merely to complete a tab list.

Enable this slice only after a real fixture proves:
- Substation → VoltageLevel → Bay;
- ConductingEquipment;
- Terminal/connectivity;
- LNode placement/binding.

## Current M3A0 boundary

The workstation now provides real projectors for:

IEDs | Network | GOOSE | DataSets | Reports & Logs | Data Model | Setting Groups

XML remains an expert/source view.

Substation remains intentionally deferred. Broad SCL surgery remains locked until semantic/engineering diagnostics and reference-impact coverage for the affected operation are proven.

## M3UX1 — engineering desktop density correction

User visual acceptance of the real golden SCD showed that correct domain projections alone were insufficient: the application still read visually like a web/dashboard shell.

M3UX1 therefore locks these presentation rules:
- engineering panes are square/docked, not card-like;
- Project Explorer rows prioritize scan density over marketing-card layout;
- workspace tabs are compact task selectors rather than oversized navigation links;
- Properties is a two-column inspector first, prose explanation second;
- Problems/Search/Changes is a resizable dock;
- engineering data tables own the majority of horizontal space;
- hierarchical model depth must be rendered as actual indentation, not string padding.

The visual reference remains the workflow density of IEDScout/System Configurator, not a literal copy of either product's colors or proprietary styling.

Acceptance evidence: `docs/testing/M3UX1_ENGINEERING_DESKTOP_ACCEPTANCE.md`.
