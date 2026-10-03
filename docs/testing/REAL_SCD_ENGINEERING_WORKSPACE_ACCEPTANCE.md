# Real SCD engineering-workspace acceptance target

Status: M3A0 golden manual fixture contract  
Date: 2026-10-03

## Fixture identity

User-supplied real SCD:

`IEC_station_1_20260924_C264_SCC_SELECTIVE_POLL_CSWI3.scd`

SHA-256:

`45dd0b8c0a39c0aaefcda2419fc24efea49030f04f09e9fe644678b60f6cbab5`

The fixture itself is **not committed** to the repository. This document records expected engineering projections derived from that exact file so future GUI work has a stable real-world acceptance target.

## Document identity

Header:
- id: `IEC station 1`
- version: `1`
- revision: `48`
- toolID: `IEC 61850 System Configurator, Version: 9.0.55`
- nameStructure: `IEDName`

Top-level vendor `Private` elements from Siemens are present and must be preserved, but they must not dominate the default engineering navigation.

## IED workspace

The default IED workspace must show exactly these five IEDs from the fixture:

| IED | Manufacturer | Type | AP | LD | LN incl. LN0 | DataSets | FCDA | ReportControl | GOOSE | ExtRef | SGCB |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| SIEBCU | SIEMENS | 6MD85 | 1 | 5 | 74 | 2 | 22 | 56 | 1 | 18 | 1 |
| E016MD66 | — | — | 1 | 3 | 73 | 2 | 25 | 176 | 1 | 0 | 0 |
| BCUGE | GE Multilin - Europe | F650 Bay Controller | 1 | 1 | 153 | 7 | 119 | 15 | 2 | 0 | 1 |
| SE_C264 | Schneider Electric | C264 | 1 | 3 | 24 | 31 | 334 | 30 | 1 | 6 | 0 |
| C264 | ALSTOM | C264 | 1 | 3 | 23 | 30 | 360 | 38 | 1 | 0 | 0 |

Whole-file engineering totals:
- IEDs: 5
- AccessPoints: 5
- Servers: 5
- LogicalDevices: 15
- LogicalNodes including LN0: 347
- DataSets: 72
- FCDA members: 860
- ReportControls: 315
- GSEControls: 6
- SettingControls: 2
- ExtRefs: 24

The UI must not make the user infer these facts from XML nesting.

## Network workspace

The file contains one SubNetwork:
- name: `Subnet`
- type: `8-MMS`

Expected IED/AP/IP projection:

| IED | AccessPoint | IP | Subnet |
| --- | --- | --- | --- |
| SIEBCU | E | 192.16.1.192 | 255.255.255.0 |
| E016MD66 | P1 | 192.16.1.43 | 255.255.255.0 |
| BCUGE | S1 | 192.16.1.33 | 255.255.255.0 |
| SE_C264 | AP1 | 192.16.1.23 | 255.255.255.0 |
| C264 | C26xIECMapping | 192.16.1.13 | 255.255.255.0 |

BCUGE additionally carries gateway `192.16.1.1`.

The Network workspace must expose address information as engineering properties. Raw `Address/P` XML nodes are an expert representation, not the primary view.

## GOOSE workspace

The fixture contains six GSEControl publishers.

At minimum ARSCL must join:
- IED / AccessPoint;
- LogicalDevice;
- GSEControl;
- bound DataSet;
- DataSet members;
- Communication/GSE endpoint;
- destination MAC;
- APPID;
- VLAN ID;
- VLAN priority;
- MinTime / MaxTime;
- subscriber/ExtRef relationships where resolvable.

Representative publisher from SIEBCU:

GSEControl:
- LD: `CTRL`
- name: `Control_DataSet`
- type: `GOOSE`
- DataSet: `DataSet`
- appID: `SIEBCU/CTRL/LLN0/Control_DataSet`
- confRev: `20001`

Communication endpoint:
- destination MAC: `01-0C-CD-01-00-93`
- APPID: `0001`
- VLAN ID: `000`
- VLAN priority: `4`
- MinTime: `10`
- MaxTime: `2000`

SIEBCU also contains ExtRef subscriptions to sources including SE_C264, BCUGE and E016MD66. Those relationships must be shown as engineering source/subscriber relationships instead of isolated ExtRef XML attributes.

## DataSets workspace

Representative SIEBCU DataSets:

### DataSet

4 FCDA members:
- CTRL / XSWI5 / Pos.stVal [ST]
- CTRL / XSWI5 / Pos.q [ST]
- CTRL / XSWI6 / Pos.stVal [ST]
- CTRL / XSWI6 / Pos.q [ST]

This DataSet is bound to SIEBCU GOOSE `Control_DataSet`.

### DataSet_1

18 FCDA members.

Representative members include:
- CTRL / XCBR1 / Pos [ST]
- CTRL / CSWI1 / Pos [ST]
- CTRL / XSWI1 / Pos [ST]
- CTRL / CSWI2 / Pos [ST]
- CTRL / XSWI2 / Pos [ST]

ARSCL must show DataSet membership as a member table with resolved IEC object identity and FC context, not as generic FCDA XML rows.

## Reports & Logs workspace

The fixture contains 315 ReportControl elements total.

Per IED:

| IED | Total RCB | Buffered | Unbuffered |
| --- | ---: | ---: | ---: |
| SIEBCU | 56 | 25 | 31 |
| E016MD66 | 176 | 6 | 170 |
| BCUGE | 15 | 10 | 5 |
| SE_C264 | 30 | 3 | 27 |
| C264 | 38 | 12 | 26 |

Representative SIEBCU buffered report:

- name: `Buffer`
- DataSet: `DataSet_1`
- rptID: `SIEBCU/CTRL/LLN0$BR$Buffer`
- confRev: `80001`
- buffered: `true`
- bufTime: `100`
- RptEnabled max: `6`

Trigger options:
- data change: true
- quality change: true
- data update: true
- period: true

Optional fields:
- sequence number: true
- timestamp: true
- dataset: true
- reason code: true
- data reference: false
- entry ID: false
- configuration revision: true

The Reports workspace must translate this into a readable RCB property view comparable in concept to Siemens System Configurator / IEDScout. It must not make the user read child XML elements to understand TrgOps or OptFlds.

## Setting Groups workspace

The file contains two SettingControl objects.

Representative SIEBCU SettingControl:
- numOfSGs: `1`
- actSG: `1`

SIEBCU PROT contains configured setting values and metadata. The workspace must eventually resolve SG-related DA/DAI values through DataTypeTemplates so the engineer sees:
- setting identity;
- FC=SG context;
- value;
- unit where resolvable;
- bounds/step where represented.

Do not infer a setting's IEC meaning from a DAI name alone.

## Data Model workspace

The fixture contains:
- 137 LNodeType definitions;
- 175 DOType definitions;
- 21 DAType definitions;
- 41 EnumType definitions;
- 1,742 DO definitions;
- 50 SDO definitions;
- 896 DA definitions;
- 67 BDA definitions;
- 438 EnumVal entries.

The Data Model workspace must resolve:

`IED → LogicalDevice → LogicalNode → DataObject → DataAttribute`

and join instance data with type templates.

Raw `lnType`, `DOType`, `DAType` IDs are useful expert metadata but must not be the only representation.

## XML expert view

The complete source remains available here, including:
- Siemens `Private` data;
- namespaces;
- exact attributes/text;
- source position;
- unsupported vendor extensions.

Unknown content must be preserved even when ARSCL cannot interpret it.

## Acceptance principle

The fixture is considered **opened successfully as an IEC 61850 engineering project** only when an engineer can answer the following without reading XML:

1. Which IEDs are in the station?
2. What IP/AP belongs to each IED?
3. Which GOOSE publishers exist and what MAC/APPID/VLAN do they use?
4. Which DataSet does a GOOSE or RCB use?
5. What signals are members of each DataSet?
6. Which RCBs are buffered/unbuffered and what are their trigger/optional-field settings?
7. What LD/LN/DO/DA model belongs to an IED?
8. Which setting groups and setting values exist?
9. Which subscriptions/references connect one IED to another?
10. Where in the original SCL did a selected engineering object come from, when expert traceability is needed?

If those questions require navigating generic XML nodes, the engineering workspace is not complete.
