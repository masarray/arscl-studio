# Project handoff

Last updated: 2026-10-03

## Current phase

**M3A0 — IEC 61850 Engineering Workstation: COMPLETE for the current golden SCD domains; manual visual acceptance pending.**

- Repository: `masarray/arscl-studio`
- Branch: `feature/m3a0-iec-workstation-ia`
- PR: #5 (draft; keep unmerged until visual audit is accepted)
- Base main: `6bb9eab76814280e5f27557d638612ab88039eb0` (M2B)
- Verified executable head: `8f8148d0336d0694b6ae03240d0d0c8715d0e60d`
- Verified executable CI: `37124979451`
- Windows / Ubuntu / macOS build + tests: green
- SCL tests: 10/10 per OS
- Engine/Desktop tests: 54/54 per OS
- Total: **64/64 tests per OS**
- Verified Windows self-contained artifact: `11273649986` (`ARSCL-Studio-win-x64`)
- Verified build digest: `sha256:325e3c0112e7fe2e64ff1a14ea8072955823a6c995e17ef6b0def2d8a05bd594`

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
- row source points at the instance when present, otherwise at the type definition.

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
- supported Services interpretation is not yet complete.
- Substation primary-system semantics are deferred until a real fixture exists.
- schema provider remains explicit/legal-source dependent.
- visual quality still requires manual Windows/high-DPI acceptance on the golden SCD.

## Next phase: M3A — Semantic & Engineering Diagnostics

Do not rebuild M3A0 workspaces. Extend their semantic confidence.

Priority:
1. typed Network/GOOSE/Data Model/Setting Group engineering diagnostics;
2. unresolved/ambiguous type-chain and endpoint findings;
3. supported Services interpretation;
4. SMV communication endpoint linkage following the typed GSE pattern;
5. edition-aware schema/rule-provider evolution from legally sourced assets;
6. contextual explanations and quick navigation;
7. Substation hierarchy only after a real Substation fixture is supplied.

Broad SCL surgery remains M4 and must not start until reference-impact and validation coverage for each destructive operation is explicit.
