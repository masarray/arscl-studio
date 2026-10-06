# M3A0 — IEC 61850 Engineering Workstation acceptance

Date: 2026-10-03

## Accepted scope

M3A0 corrects the original XML-shaped desktop information architecture.

Enabled engineering workspaces:
- IED overview;
- Network;
- GOOSE;
- DataSets;
- Reports & Logs;
- Data Model;
- Setting Groups.

Secondary/expert views:
- Model Tree;
- XML.

Substation is deliberately not enabled because the current golden SCD does not contain a Substation section.

## Architectural acceptance

Desktop does not parse XML to invent IEC meaning.

Each enabled workspace is backed by an Engine projector using:
- stable `SclNodeHandle` identities;
- the semantic index;
- the typed reference graph;
- bounded syntax reads for exact attributes/values;
- shared selection for Context / Where Used / XML traceability.

### Network

Projects SubNetwork / ConnectedAP / Address/P as:
- SubNetwork/type;
- IED/AP;
- IP;
- subnet;
- gateway;
- OSI AP-title / AE-qualifier when present.

### GOOSE

Projects:
- GSEControl;
- GOOSE vs GSSE service type;
- bound DataSet and FCDA signals;
- Communication/GSE endpoint;
- destination MAC;
- network APPID;
- VLAN ID/priority;
- MinTime/MaxTime;
- subscriber ExtRefs.

Communication/GSE is a semantic object and is deterministically linked to GSEControl by IED + LD + control-block name.

Subscriber matching uses source signal identity rather than control-block-name guessing.

### DataSets

Projects:
- LD/LN/DataSet;
- member count;
- typed usage count;
- member LD/LN/DO/DA/FC/reference.

### Reports & Logs

Projects:
- BRCB vs URCB;
- DataSet;
- rptID;
- confRev;
- bufTime/intgPd;
- maximum clients;
- trigger options;
- optional fields.

### Data Model

Resolves:
`LN → LNodeType → DO → DOType → DA/SDO → DAType/BDA`

and overlays DOI/SDI/DAI instance values.

The UI exposes:
- LD/LN/class;
- DO/SDO/DA/BDA hierarchy;
- CDC;
- FC;
- bType;
- configured value;
- type ID;
- description.

### Setting Groups

Projects:
- SettingControl metadata;
- FC=SG leaf values;
- units/multipliers where represented;
- min/max/step where represented;
- structured values such as `setMag.f`.

## Golden real-SCD contract

See:
`docs/testing/REAL_SCD_ENGINEERING_WORKSPACE_ACCEPTANCE.md`

Golden fixture:
`IEC_station_1_20260924_C264_SCC_SELECTIVE_POLL_CSWI3.scd`

SHA-256:
`45dd0b8c0a39c0aaefcda2419fc24efea49030f04f09e9fe644678b60f6cbab5`

The fixture itself is not committed.

## Automated evidence

Verified executable head:
`8f8148d0336d0694b6ae03240d0d0c8715d0e60d`

CI:
`37124979451`

Per operating system:
- SCL tests: 10/10;
- Engine/Desktop tests: 54/54;
- total: **64/64**.

Windows / Ubuntu / macOS build and tests are green.

Windows self-contained publish is green.

Windows artifact:
`ARSCL-Studio-win-x64` — `11273649986`

The verified Ubuntu build reported zero warnings and zero errors.

Existing preservation, transaction, validation, large-tree and 100k-DAI regression gates remained green.

## Manual acceptance still required

Automation does not prove visual quality. Before merge/release, manually inspect the Windows artifact using the golden real SCD for:
- workstation hierarchy clarity;
- table density/readability;
- splitter defaults;
- text clipping;
- high-DPI behavior;
- GOOSE signal/subscriber readability;
- Data Model hierarchy readability;
- Setting Groups value/unit/bound presentation.

Do not enable broad mutation merely because these read projections are green.
