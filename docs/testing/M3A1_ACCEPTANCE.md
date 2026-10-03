# M3A1 — Semantic Confidence & Engineering Diagnostics acceptance

Date: 2026-10-03

## Purpose

M3A1 extends the M3A0 engineering workstation without rebuilding its workspaces. The milestone adds source-linked engineering consistency diagnostics, typed SMV communication semantics, explicit Services declarations, deeper diagnostic navigation, and a fast/full validation split that protects large-file responsiveness.

Branch:
`feature/m3a1-semantic-engineering-diagnostics`

Stacked PR:
`#6`, based on `feature/m3a0-iec-workstation-ia` / PR #5.

## Engineering diagnostics

All findings use `DiagnosticDomain.Engineering`, retain `SclNodeHandle`, source span, source path and `DocumentRevision`, and publish through the existing revision-safe validation workers.

### Network

- `SCL-ENG-NET-0001` — duplicate non-empty ConnectedAP IP address within the same SubNetwork.
- Severity: Warning.
- The message lists all involved IED/AP participants.
- This is explicitly an engineering consistency warning, not a schema-invalidity claim.

### GOOSE

- `SCL-ENG-GOOSE-0001` — non-GSSE GSEControl has no resolved Communication/GSE endpoint.
- `SCL-ENG-GOOSE-0002` — resolved Communication/GSE endpoint is missing destination MAC and/or APPID.
- `SCL-ENG-GOOSE-0003` — publisher does not name a DataSet.
- GSSE is excluded from Ethernet GOOSE endpoint warnings.
- If a DataSet name exists but does not resolve, the existing typed Reference diagnostic owns that condition; Engineering does not duplicate it.

### Sampled Values

- `SCL-ENG-SMV-0001` — SampledValueControl has no resolved Communication/SMV endpoint.
- `SCL-ENG-SMV-0002` — resolved Communication/SMV endpoint is missing multicast MAC and/or APPID.
- `SCL-ENG-SMV-0003` — SampledValueControl does not name a DataSet.

### Data Model

- `SCL-ENG-MODEL-0001` — DOI/SDI/DAI instance branch does not match the resolved DataTypeTemplates path.
- Only the topmost unmatched branch is reported; descendants are suppressed to prevent cascading noise.
- ARSCL does not guess an alternative type-template attachment.

### Setting Groups

- `SCL-ENG-SG-0001` — SettingControl does not expose a positive interpretable `numOfSGs`.
- `SCL-ENG-SG-0002` — active setting group is outside the interpreted `1..numOfSGs` range.
- `SCL-ENG-SG-0003` — no configured FC=SG leaf values are resolvable through the current Data Model.
- `SCL-ENG-SG-0003` is Info because absent instantiated values can be legitimate.

## Fast vs full validation

Automatic Fast validation intentionally excludes the deep all-LN DOI/SDI/DAI consistency scan.

Fast validation includes:
- typed Reference diagnostics;
- Network engineering checks;
- GOOSE engineering checks;
- SMV engineering checks;
- SettingControl engineering checks;
- schema-provider status.

Full validation includes everything above plus:
- deep Data Model instance/template consistency;
- configured schema-provider validation when available.

The Desktop exposes a compact `Full validate` action next to the Validation summary. Full validation still uses latest-wins work, cancellation, source revision checks and stale-result rejection.

## Typed SMV semantic foundation

`Communication/SMV` is now a first-class `SmvCommunication` semantic object.

The communication-control resolver was generalized to a typed key:
`(control kind, IED, LD, control-block name)`.

Therefore:
- Communication/GSE can bind only to GSEControl;
- Communication/SMV can bind only to SampledValueControl;
- identical control names across GOOSE and SMV do not cross-bind;
- both retain typed `CommunicationControlBinding` edges;
- SampledValueControl continues to use the existing typed DataSet binding.

## Declared Services model

Direct standard-namespace children of `<Services>` are represented as generic source-linked `ServiceCapability` objects.

ARSCL deliberately does not maintain a guessed hard-coded supported-services list.

The IED Overview shows:
- SampledValueControl count;
- number of explicitly declared service capabilities;
- compact declared-service table with element name, declared attributes and nested-element count.

Absence of a declaration is not automatically interpreted as unsupported. Vendor/private nested content remains preserved and is not promoted to a capability by guess.

## Problems usability

- `Diagnostic.Explanation` is retained in `ProblemRow`.
- Explanation is exposed as a compact tooltip on the Problems message.
- selecting a Communication/GSE endpoint diagnostic keeps the source selected while also highlighting the bound GOOSE publisher.
- selecting an unmatched DOI/SDI/DAI diagnostic keeps the source selected while Data Model highlights the nearest still-resolved ancestor.
- Context / XML traceability therefore remains exact while engineering context is easier to inspect.

## Automated evidence

Verified executable head:
`668a2e6670b012188bae0e615f32e43f78b6c394`

GitHub Actions run:
`37136181640`

Per operating system:
- SCL tests: **12/12**;
- Engine/Desktop tests: **62/62**;
- total: **74/74**.

Windows / Ubuntu / macOS build and tests are green.

Verified Ubuntu build:
- 0 warnings;
- 0 errors.

Windows self-contained publish is green.

Windows artifact:
`ARSCL-Studio-win-x64` — artifact `11278628594`

Artifact SHA-256:
`b11909edabe322e59ea978fac9ac756c26d0ba4bd8d39840f5f7bc5ca7c5ce5c`

## Deliberate limits

- Engineering findings are not presented as normative IEC schema validity unless backed by the configured schema provider.
- No normative IEC XSD/NSD text is embedded.
- Services projection is descriptive; it does not infer capability from absence.
- M3A1 establishes SMV endpoint semantics and diagnostics but does not add an SMV workstation tab without a real acceptance fixture that benefits from it.
- Substation hierarchy remains deferred because the current golden SCD has no `<Substation>` section.
- Broad identity/DataSet/RCB/communication surgery remains locked.

## Manual acceptance

Use the Windows artifact to verify:
- Full validate button remains compact and understandable;
- Problems explanation tooltip is readable;
- problem selection retains source Context/XML while highlighting the useful engineering workspace;
- Services table does not dominate the IED Overview;
- M3A0 density and splitter behavior have not regressed.
