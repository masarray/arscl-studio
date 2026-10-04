# Project handoff

Last updated: 2026-10-04

## Current phase

**M3A0 workstation baseline + M3A1/M3A2 semantic slices: complete. M3UX1/M3UX2 + engineer mental-workspace finishing M3UX3: implemented; final Windows/high-DPI visual acceptance remains pending.**

- Repository: `masarray/arscl-studio`
- Branch: `feature/m3a0-iec-workstation-ia`
- PR: #5 (draft; keep unmerged until visual audit is accepted)
- Base main: `6bb9eab76814280e5f27557d638612ab88039eb0` (M2B)
- Verified executable head: `531b023564336fcdfd8de26eb3a2a3fb67baddcb`
- Verified executable CI: `37172958318`
- Windows / Ubuntu / macOS build + tests: green
- SCL tests: 11/11 per OS
- Engine/Desktop tests: 66/66 per OS
- Total: **77/77 tests per OS**
- Verified Windows self-contained artifact: `11291892937` (`ARSCL-Studio-win-x64`)
- Verified build digest: `sha256:cfbe90b60e318fe40fc61662c96566af0948de6ea590c2dacab0bcbe269a02a9`

Documentation-only commits after the executable head do not change runtime behavior. Final PR-head CI must still remain green before merge.

## Golden acceptance fixture

User-supplied real SCD:
`IEC_station_1_20260924_C264_SCC_SELECTIVE_POLL_CSWI3.scd`

SHA-256:
`45dd0b8c0a39c0aaefcda2419fc24efea49030f04f09e9fe644678b60f6cbab5`

The fixture itself is not committed.

Read:
- `docs/testing/REAL_SCD_ENGINEERING_WORKSPACE_ACCEPTANCE.md`
- `docs/testing/M3A0_ACCEPTANCE.md`
- `docs/testing/M3A_DIAGNOSTICS_ACCEPTANCE.md`
- `docs/testing/M3A2_MODEL_SERVICES_ACCEPTANCE.md`
- `docs/testing/M3UX1_ENGINEERING_DESKTOP_ACCEPTANCE.md`
- `docs/testing/M3UX2_SCREENSHOT_REFINEMENT_ACCEPTANCE.md`
- `docs/testing/M3UX3_ENGINEER_WORKSPACE_ACCEPTANCE.md`
- `docs/ux/IEC61850_WORKSTATION_INFORMATION_ARCHITECTURE.md`

## Product direction locked by M3A0

ARSCL is an IEC 61850 engineering workstation, not an XML viewer with IEC labels.

Enabled engineering workspaces:
`IED Overview | Network | GOOSE | DataSets | Reports & Logs | Data Model | Setting Groups`

Secondary/expert views:
`Model Tree | XML`

Desktop must never walk XML to invent IEC meaning. Every enabled workspace must be backed by an Engine projector and tests.

## Implemented workspaces

### IED Overview
- default landing context is an IED, not the SCL root;
- manufacturer/description;
- AP / LD / LN / DataSet / Report / GOOSE / Setting Group counts;
- dense per-IED Declared Services table;
- raw Services parameters plus conservative interpretation of limits, booleans and Fix/Conf/Dyn modes;
- unknown/future Services entries remain visible as uninterpreted declarations;
- Services rows navigate to authoritative source nodes;
- vendor Private XML stays preserved but out of the primary engineering navigation.

### Network
- SubNetwork and network type;
- ConnectedAP;
- IED/AP;
- IP, subnet, gateway;
- OSI AP-title/AE-qualifier retained when present;
- shared source navigation.

### GOOSE / GSSE
- Communication/GSE is a typed semantic object;
- deterministic Communication/GSE ↔ GSEControl binding via IED + LD + control name;
- GOOSE vs GSSE remains distinct;
- bound DataSet and FCDA member signals;
- MAC / network APPID / VLAN / priority;
- MinTime / MaxTime;
- ExtRef subscribers matched by source signal identity;
- no guessed srcCBName/control-block relation.

### DataSets
- IED-scoped DataSet catalog;
- LD / LN / DataSet;
- member and usage counts;
- FCDA member LD / LN / DO / DA / FC / reference;
- shared source navigation.

### Reports & Logs
- BRCB vs URCB;
- DataSet;
- rptID;
- confRev;
- bufTime / intgPd;
- RptEnabled max;
- readable TrgOps;
- readable OptFields.

### Data Model
- IED-scoped LD/LN selector;
- LN → LNodeType → DO → DOType → DA/SDO → DAType/BDA resolution;
- DOI/SDI/DAI instance overlay;
- CDC / FC / bType / configured value / type ID / description;
- row source points at the instance when present, otherwise at the type definition;
- bounded instance/template consistency validation for DOI/SDI/DAI;
- duplicate template-member ambiguity is never guessed;
- cached type contexts preserve fast-validation performance on large repeated models.

### Setting Groups
- SettingControl metadata;
- FC=SG leaf values derived from the resolved Data Model;
- structured settings such as `setMag.f`;
- sibling units/multiplier join;
- min/max/step join using the same nested leaf suffix;
- no unit or semantic guess from DAI names.

## Important real-fixture details

- golden SCD contains 5 IEDs;
- 72 DataSets;
- 315 ReportControls;
- 6 GSEControls;
- 24 ExtRefs;
- 2 SettingControls;
- one BCUGE GSEControl is `type="GSSE"` and therefore has no Ethernet Communication/GSE endpoint;
- the golden SCD has **no `<Substation>` section**.

Do not add a fake/empty Substation workspace just to complete a tab list.

## Stable safety boundaries retained

- preservation-oriented syntax document remains authoritative;
- stable SclNodeHandle and source spans;
- typed reference graph and Where Used;
- unresolved/ambiguous reference diagnostics;
- revision-safe validation workers;
- transaction/undo/redo/save guarantees;
- generic XML mutation remains forbidden in Desktop;
- only the previously approved safe property edit policy remains open;
- identity rename/delete, DataSet surgery, RCB surgery, broad communication editing and merge remain locked.

## Current limits

- M3A0 is a read/inspect engineering-workspace milestone, not complete IEC 61850 semantic validation.
- Data Model currently shows configured raw values; enum ordinal/text normalization can be expanded later.
- Setting Group units are only shown when represented/resolvable.
- GOOSE subscriber projection matches source signal identity; deeper service-type diagnostics belong to M3A.
- SMV communication endpoint engineering workspace is not yet implemented.
- Substation primary-system semantics are deferred until a real fixture exists.
- schema provider remains explicit/legal-source dependent.
- visual quality still requires manual Windows/high-DPI acceptance on the golden SCD.

## M3A1 + M3A2 implemented

### Diagnostics slice 1

Problems/validation coverage:
- `SCL-ENG-GOOSE-0001..0004`: missing GOOSE endpoint/address/MAC/network APPID;
- `SCL-ENG-SMV-0001..0004`: missing SMV endpoint/address/MAC/network APPID;
- typed `Communication/SMV → SampledValueControl` reference binding.

### Deep Data Model consistency

- `SCL-SEM-MODEL-0001`: DOI absent from resolved LNodeType;
- `SCL-SEM-MODEL-0002`: SDI absent from resolved type context;
- `SCL-SEM-MODEL-0003`: DAI absent from resolved type context;
- `SCL-SEM-MODEL-0004`: duplicate template member ambiguity, no guessing;
- `SCL-SEM-MODEL-0005`: SDI targets non-Struct leaf;
- `SCL-SEM-MODEL-0006`: DAI targets structured SDO/DA/BDA;
- unresolved type edges remain `SCL-REF-*` root causes rather than producing duplicate downstream noise;
- 10,000-LN shared-type fast-validation performance guard is green.

### Declared Services interpretation

- actual per-IED `<Services>` children are projected; no hard-coded checklist;
- categories and raw declarations are shown in IED Overview;
- `Fix / Conf / Dyn` are rendered as Fixed / Configurable / Dynamic;
- `max`, `maxAttributes`, `modify`, `fixPrefix`, `fixLnInst` remain literal declared properties;
- unknown/future/vendor service elements stay visible and source-linked;
- UI explicitly distinguishes SCL declaration from runtime verification.

Services consistency diagnostics:
- `SCL-ENG-SERVICE-0001`: explicit GOOSE count exceeds declared GOOSE max;
- `SCL-ENG-SERVICE-0002`: explicit GSSE count exceeds declared GSSE max;
- `SCL-ENG-SERVICE-0003`: SampledValueControl count exceeds declared SMV max;
- untyped GSEControl is not silently inferred to be GOOSE.

See:
- `docs/testing/M3A_DIAGNOSTICS_ACCEPTANCE.md`;
- `docs/testing/M3A2_MODEL_SERVICES_ACCEPTANCE.md`.

## M3UX1 + M3UX2 workstation refinement implemented

M3UX1 established the compact three-pane desktop shell. M3UX2 uses real Windows screenshots to correct the remaining web/dashboard behavior:
- engineering workspace selector is forced to one horizontal row;
- tab names are stable and short; volatile counts no longer cause wrapping;
- Overview is a compact IED summary plus full-height Services grid;
- Settings has an explicit no-SettingControl state instead of a blank page;
- Problems can be filtered by severity, domain, and text while preserving source navigation;
- Inspector exposes Type/Value/Path/Source/Namespace as bordered property rows with wrapping/tooltips;
- Data Model gives hierarchy/name more width without increasing total table width;
- central engineering tables use subtle horizontal row separators.

See `docs/testing/M3UX2_SCREENSHOT_REFINEMENT_ACCEPTANCE.md`.

## M3UX3 engineer mental-workspace finishing implemented

M3UX3 finishes navigation coherence without changing IEC semantics:
- compact IED context selector + persistent engineering workspace navigator in Project Explorer;
- persistent center breadcrumb for IED/domain/object context;
- Problems/Search/Where Used automatically activate the relevant engineering workspace;
- Reports directly exposes members of its deterministically resolved bound DataSet;
- Data Model gains Desktop-only expand/collapse while preserving the authoritative Engine projection;
- LN selector width is rebalanced for engineering identity rather than prose;
- Network is scoped consistently to the selected IED;
- Ctrl+F is functional;
- roadmap-only disabled controls/tabs are hidden until implemented.

See `docs/testing/M3UX3_ENGINEER_WORKSPACE_ACCEPTANCE.md`.

## Next visual gate

Before merging PR #5, run the M3UX3 Windows artifact on the golden SCD and provide screenshots of Overview, Network, GOOSE, Reports, Data Model, Settings and Problems at normal/high DPI.

Audit specifically:
1. selected IED + engineering domain remain obvious while navigating;
2. Network changes scope when the IED context changes;
3. Report selection exposes bound DataSet members without a manual DataSets lookup;
4. Data Model DO/DA hierarchy is easy to scan collapsed/expanded;
5. Problems/Search/Where Used land on the correct visible workspace;
6. Properties/Where Used remain readable with the denser center workflow;
7. no clipping/wrapping regression at 100%, 125% and 150% DPI.

Only after that visual gate passes should work return to M3A3 edition-aware rule/schema-provider evolution. Broad SCL surgery remains locked.
