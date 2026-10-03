# M3UX2 — Screenshot-driven engineering workstation refinement

Date: 2026-10-04

## Input

Manual screenshots of the M3UX1 Windows build were reviewed across:
- Overview;
- Network;
- GOOSE;
- DataSets;
- Reports;
- Data Model;
- Setting Groups;
- the overall Project Explorer / Inspector / Problems shell.

The semantic content was correct, but the screenshots still exposed several presentation problems that reduced long-session engineering usability.

## Findings addressed

### 1. Workspace navigation wrapped to two rows

This made the center pane feel like web navigation rather than a task strip.

Correction:
- the engineering TabControl now uses a horizontal StackPanel ItemsPanel;
- task names are intentionally short and stable: Overview, Network, GOOSE, DataSets, Reports, Data Model, Settings;
- volatile object counts are no longer allowed to expand tab headers;
- counts remain available in the IED navigator and workspace content.

### 2. Overview contained dashboard-like dead space

Correction:
- the Overview page is now a fixed engineering summary + full-height declared-Services grid;
- the redundant selected-object article was removed because Inspector already owns object context;
- IED identity/counts are shown in a compact property matrix;
- Services rows fill the remaining work area and remain virtualized;
- edit-description UI appears only when editing.

### 3. Setting Groups produced a blank page for an IED without SettingControl

Correction:
- an explicit engineering empty-state is shown;
- it states that no SettingControl exists in the selected IED;
- it directs the engineer to another IED or Data Model rather than leaving an unexplained white surface.

### 4. Problems was a diagnostic dump rather than a diagnostic tool

Correction:
- severity filter;
- domain filter;
- free-text filter over code/domain/object/message/source;
- dynamic `Problems (visible/total)` header;
- filters preserve problem-to-source shared navigation;
- problem rows use compact engineering-grid separators and message tooltips.

### 5. Inspector long paths/sources were difficult to scan

Correction:
- Properties now uses explicit bordered property rows;
- Type, Value, Path, Source and Namespace are separate fields;
- long values wrap and have tooltips;
- engineering explanation remains separated below raw identity/source properties.

### 6. Data Model hierarchy still sacrificed the most important column

Correction:
- Logical Node selector reduced to 210 px baseline;
- hierarchy/name column increased to 220 px;
- bType/value columns reduced so total table width does not grow;
- object path is available as a tooltip on hierarchy rows;
- Engine-provided depth indentation remains authoritative.

### 7. Engineering tables lacked horizontal scan guidance

Correction:
- central engineering ListBoxes use a shared `engineeringGrid` class;
- ListBoxItems receive subtle bottom separators;
- virtualization is unchanged.

## Architectural boundary

- no IEC 61850 semantic logic moved into Desktop;
- no XML traversal added to Desktop;
- no mutation policy expanded;
- no reference/validation rule changed;
- all changes are presentation, filtering, and navigation ergonomics over existing Engine state.

## Regression

A new desktop regression verifies:
- filtering Problems by domain;
- filtering by severity;
- filtering by text;
- selecting a filtered Problem still navigates to its source node.

## Automated evidence

Verified executable head:
`b035cec83bf81c723a5f82ec9bf38460883a993f`

GitHub Actions run:
`37141798157`

Per OS:
- SCL tests: 11/11;
- Engine/Desktop tests: 66/66;
- total: **77/77**;
- Windows: green;
- Ubuntu: green;
- macOS: green;
- Ubuntu build: 0 warnings / 0 errors.

Windows self-contained artifact:
`ARSCL-Studio-win-x64` — artifact `11280143971`

Artifact SHA-256:
`db1bb0c62da9a4e8e24507f040eba2809585c3d0912858f1f43ba1f9192c3da6`

## Next manual visual gate

Run the M3UX2 artifact on the same golden SCD and inspect:
1. workspace selector stays on one line at 100%, 125%, and 150% DPI;
2. Overview Services grid fills the center pane without dashboard-like dead area;
3. Data Model hierarchy names remain readable and indentation is obvious;
4. Settings displays an explicit no-SettingControl state on C264;
5. Problems filters make the 13 reference errors easy to isolate;
6. Inspector paths/sources are readable without widening the pane excessively;
7. GOOSE/DataSets/Reports row separators improve scanning without making the UI visually noisy.

PR #5 remains draft until the screenshot gate is accepted.
