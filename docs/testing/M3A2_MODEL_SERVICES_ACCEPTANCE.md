# M3A2 — Deep Data Model consistency + Services interpretation acceptance

Date: 2026-10-03

## Scope

This milestone extends M3A semantic confidence without changing the established M3A0 workstation information architecture.

Implemented:
- bounded DOI/SDI/DAI ↔ type-template consistency validation;
- cached type-context resolution for large repeated instance models;
- declared IED Services projection in Overview;
- source navigation for Services rows;
- contextual capability explanations;
- conservative Services publisher-limit consistency diagnostics.

## Deep Data Model consistency

Validation follows only configured instance branches and resolved type edges.

Resolution path:
`LN/LN0 → LNodeType → DO → DOType → SDO/DA → DOType/DAType → BDA`

Instance checks:
- `SCL-SEM-MODEL-0001`: DOI is absent from the resolved LNodeType;
- `SCL-SEM-MODEL-0002`: SDI is absent from the resolved type context;
- `SCL-SEM-MODEL-0003`: DAI is absent from the resolved type context;
- `SCL-SEM-MODEL-0004`: instance member matches duplicate template definitions; ARSCL does not guess;
- `SCL-SEM-MODEL-0005`: SDI targets a non-Struct leaf;
- `SCL-SEM-MODEL-0006`: DAI targets an SDO or Struct DA/BDA.

Noise-control rule:
if a required type edge is unresolved or ambiguous, the existing `SCL-REF-*` diagnostic remains the root-cause authority and the instance validator stops descending that branch.

Performance design:
- type member contexts are cached by template handle;
- validation traverses actual DOI/SDI/DAI branches only;
- it does not call the Data Model UI projector or materialize full engineering rows.

Regression guard:
- 10,000 GGIO logical nodes sharing the same LNodeType/DOType are fast-validated within the existing regression budget.

## Declared Services interpretation

`<IED><Services>` is exposed inside the IED Overview as a dense table:
`Category | Capability | Raw declaration | Interpretation`.

The table explicitly states that capability is declared by SCL and runtime verification is separate.

Known declarations are categorized and interpreted conservatively:
- association: `DynAssociation`;
- Data Model: directory/definition/read-write services;
- DataSets: value/directory/configuration/dynamic DataSet declarations;
- reporting: configurable report controls, CB values, ReportSettings;
- GSE: GSESettings, GOOSE, GSSE;
- Sampled Values: SMV/SMVSettings;
- Setting Groups: SettingGroups/SGEdit;
- logical-node configuration: ConfLNs;
- file services: FileHandling.

Literal settings are translated without adding unsupported meaning:
- `Fix` → Fixed;
- `Conf` → Configurable;
- `Dyn` → Dynamic;
- `max` and `maxAttributes` remain declared limits;
- `modify`, `fixPrefix`, and `fixLnInst` remain declared boolean properties.

Unknown/future/vendor Services child elements are retained in the table as:
`Other / semantics not interpreted by ARSCL`

with raw attributes preserved and source navigation intact.

## Services consistency diagnostics

Only explicit publisher identity is compared against literal Services limits:
- `SCL-ENG-SERVICE-0001`: explicit `type="GOOSE"` count exceeds `Services/GOOSE@max`;
- `SCL-ENG-SERVICE-0002`: explicit `type="GSSE"` count exceeds `Services/GSSE@max`;
- `SCL-ENG-SERVICE-0003`: SampledValueControl count exceeds `Services/SMV@max`.

Deliberate non-inference:
- GSEControl without `type` is not silently counted as GOOSE;
- `GSEControl@appID` and `SampledValueControl@smvID` are not substitutes for Communication-layer network APPID;
- ConfReportControl max is not treated as a total ReportControl count gate in this milestone.

## Golden SCD evidence

The user-supplied golden fixture declares distinct Services sets per IED.

Representative SIEBCU declarations include:
- SettingGroups/SGEdit;
- ConfDataSet max=50, maxAttributes=200, modify=true;
- DynDataSet max=30, maxAttributes=60;
- ConfReportControl max=60;
- configurable/dynamic ReportSettings;
- GSESettings;
- ConfLNs fixPrefix=false, fixLnInst=false;
- GOOSE max=16;
- FileHandling.

Other IEDs deliberately differ, including:
- E016MD66: SetDataSetValue, GOOSE max=1, GSSE max=0;
- BCUGE: GSSE max=1, GOOSE max=4 and fixed/configurable GSE settings;
- SE_C264: GOOSE max=1, ConfDataSet max=100/maxAttributes=500/modify=false;
- C264: GOOSE max=5 and a smaller Services declaration set.

The implementation therefore derives rows from each IED's actual Services element instead of using a hard-coded capability checklist.

## Automated evidence

Verified executable head:
`e6390cd59ac395947eec94c3a86bcd5c90ef46b7`

GitHub Actions run:
`37138858197`

Per OS:
- SCL tests: 11/11;
- Engine/Desktop tests: 65/65;
- total: **76/76**.

Windows / Ubuntu / macOS build and tests: green.

Verified Ubuntu build:
- 0 warnings;
- 0 errors.

Windows self-contained artifact:
`ARSCL-Studio-win-x64` — `11280036122`

Artifact SHA-256:
`8b64c16801f39e7a4da6b1a5e372590b825b934809a16177909f9ca6a6f3a826`

## Deferred

- runtime/live capability verification;
- broader edition-aware normative service validation;
- additional service-count relationships whose semantics are not safe to infer from the current evidence;
- dedicated SMV visual workspace pending a real fixture that needs it;
- Substation semantics pending a real Substation fixture;
- destructive SCL surgery.
