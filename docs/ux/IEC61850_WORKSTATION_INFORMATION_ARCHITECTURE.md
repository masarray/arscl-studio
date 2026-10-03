# IEC 61850 workstation information architecture

Status: M3A0 corrective design contract  
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

### M3A0.1 — IED workspace

- real IED projection;
- default IED selection;
- IED-centric navigator;
- engineering counts;
- Model Tree/XML demoted to secondary views.

### M3A0.2 — Network workspace

- semantic Address/P model;
- SubNetwork/ConnectedAP projection;
- IP/network property table.

### M3A0.3 — DataSets + Reports

- DataSet catalog/member table;
- RCB catalog/properties;
- bidirectional DataSet ↔ control relationship navigation.

### M3A0.4 — GOOSE

- GSEControl + communication endpoint join;
- DataSet content;
- subscriber/ExtRef relationship surface.

### M3A0.5 — Data Model + Setting Groups

- LN/DO/DA resolved engineering tree;
- values/FC/CDC/unit presentation;
- SG-specific projection.

Only after these slices are stable should the project resume broad mutation/surgery work.
