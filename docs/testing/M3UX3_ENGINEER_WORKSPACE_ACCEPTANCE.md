# M3UX3 — Engineer mental-workspace finishing

Date: 2026-10-04

## Goal

Finish the existing M3 engineering workstation without changing IEC semantics or restarting the desktop architecture.

This slice addresses the remaining gap between a set of correct domain tables and a coherent engineer mental workspace: the selected IED, selected engineering domain, selected object, and source/relationship context must stay obvious while the engineer moves through a large SCL.

## Implemented

### Compact IED + engineering navigator

The primary Project Explorer tab is now an Engineer navigator:
- selected IED is a compact context selector instead of a tall card list;
- Overview / Network / GOOSE / DataSets / Reports / Data Model / Settings are always visible in the left navigation;
- concise per-workspace counts provide scope awareness without controlling center-tab width;
- Model Tree and XML remain secondary expert/source tabs.

This is presentation state only. Existing Engine projectors remain authoritative.

### Persistent engineering context

The center pane header now keeps:
- selected IED;
- selected engineering workspace;
- selected semantic path.

The previous low-opacity footer path was removed.

### Workspace-aware cross navigation

Problem, Search, and Where Used navigation now activates the appropriate engineering workspace before selecting the source object.

Mappings include:
- Communication / ConnectedAP / Address → Network;
- GSEControl / ExtRef → GOOSE;
- DataSet / FCDA → DataSets;
- ReportControl / LogControl → Reports;
- LN / DOI / SDI / DAI / type members → Data Model;
- SettingControl / projected SG values → Settings.

Direct selection inside a workspace does not force a domain jump.

### Reports expose transmitted signals directly

For a selected ReportControl/LogControl, Reports now shows the members of the deterministically resolved bound DataSet.

Resolution uses the existing typed `DataSetBinding` reference edge. If zero or multiple bindings exist, ARSCL does not guess and the bound-member pane remains empty.

### Collapsible Data Model inspection

The Engine's full Data Model projection remains unchanged and authoritative.

Desktop now builds a lightweight visible projection with:
- expand/collapse disclosure controls;
- retained virtualization;
- selection synchronized to the underlying `SclDataModelRowProjection`;
- automatic ancestor expansion when external navigation targets a hidden child.

The LN selector was also rebalanced so LD/LN/Class/DO count fit the allocated width; long descriptions move to tooltip/Inspector instead of consuming the selector.

### Network scope is consistent with IED context

Network rows are now scoped to the selected IED, matching the other engineering workspaces and the left navigation count. The full station network projection is retained internally only as the source for changing IED context.

### Finishing cleanup

- `Ctrl+F` now focuses/selects the IEC object search box;
- roadmap-only disabled Merge / Add IED / Extract / Export controls are hidden;
- disabled Signal Basket / Diff / Output bottom tabs are hidden;
- empty menu placeholders are removed.

## Architectural boundary

Unchanged:
- Desktop does not parse XML to invent IEC meaning;
- no XML mutation was added;
- no diagnostic rule changed;
- no destructive edit policy changed;
- SclNodeHandle remains the shared navigation identity;
- all domain semantics remain Engine-owned;
- Data Model collapse state is Desktop-only presentation state.

## Regression coverage

Existing tests were extended to verify:
- Problem navigation activates Reports for a ReportControl reference problem;
- Problem navigation activates GOOSE for a GSEControl problem;
- Reports resolves and exposes the bound DataSet members;
- Data Model parent rows collapse and restore their child rows;
- Network workspace follows selected IED context.

## Verified executable evidence

Executable head:
`531b023564336fcdfd8de26eb3a2a3fb67baddcb`

GitHub Actions run:
`37172958318`

Per OS:
- SCL tests: 11/11;
- Engine/Desktop tests: 66/66;
- total: **77/77**;
- Windows: green;
- Ubuntu: green;
- macOS: green;
- Windows self-contained publish: green.

Windows artifact:
`ARSCL-Studio-win-x64` — artifact `11291892937`

Artifact digest:
`sha256:cfbe90b60e318fe40fc61662c96566af0948de6ea590c2dacab0bcbe269a02a9`

## Manual visual gate

Run the M3UX3 Windows artifact on the golden SCD and inspect at 100%, 125%, and 150% DPI.

Required screenshots:
1. Overview with compact IED/workspace navigator;
2. Network after switching between at least two IEDs;
3. GOOSE with published signals/subscribers;
4. Reports with the bound DataSet-member pane;
5. Data Model with one DO collapsed and one expanded;
6. Settings;
7. Problems after selecting a Report/GOOSE finding.

Acceptance:
- engineer can identify IED + domain + object without reconstructing context from multiple panes;
- external navigation lands on the correct visible workspace;
- Data Model hierarchy is scan-friendly and does not become one permanently expanded wall of rows;
- Network does not silently mix other IEDs into the selected IED context;
- Reports answers “what signals does this RCB contain?” without requiring a manual trip to DataSets;
- no clipping/wrapping regression is introduced by high DPI.
