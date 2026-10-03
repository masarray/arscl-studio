# M3UX1 — Engineering Desktop Density & Inspection Workflow

Date: 2026-10-03

## Why this slice exists

The M3A0-M3A2 desktop had correct IEC 61850 domain projections but still presented too much like a web/dashboard UI. The user visual audit showed that detailed observation was less comfortable than IEDScout/System Configurator because navigation rows were card-like, tabs were visually oversized, the inspector read like an article, and the diagnostic area was fixed.

This slice changes presentation and workstation behavior without moving IEC 61850 semantics into Desktop or changing the authoritative model.

## Implemented

### Engineering shell
- default font density reduced to workstation scale;
- square docked panes instead of rounded card-like panels;
- compact selected workspace tabs;
- left/right panes reduced to give the engineering table more width;
- center IED context header converted to a compact context strip;
- generic explanatory footer replaced by a selected-object engineering status strip.

### Project Explorer
- explicit `Project Explorer` pane chrome;
- IED selector rows changed from multi-line cards to compact engineering rows;
- per-IED manufacturer/description/count context remains visible;
- Model Tree and XML remain secondary tabs;
- search stays in the explorer pane.

### Data Model inspection
- Logical Node selector narrowed so the model table receives more width;
- DO/DA/BDA Name column receives stable width instead of competing stars;
- compact 24-25 px data rows;
- selected-object detail is a compact lower property strip;
- hierarchy indentation uses the Engine-provided `Depth` through a Desktop-only pixel-margin converter; no IEC logic is duplicated in XAML;
- virtualization remains unchanged.

### Inspector
- right `Context` article-like panel replaced with `Inspector`;
- `Properties` uses dense name/value presentation for type, value, semantic path and source;
- engineering explanation is separated from raw identity properties;
- `Where Used` becomes a compact source/kind table with reference text below;
- shared SclNodeHandle selection remains the navigation authority.

### Command and diagnostics docks
- top commands grouped into file actions, SCL-structure actions, validation/profile/export actions;
- Problems/Search/Changes remain a bottom dock rather than page content;
- bottom dock is vertically resizable using a GridSplitter;
- diagnostic rows are denser;
- status bar is reduced to workstation scale.

## Architectural boundary

- no XML traversal or IEC business rule was added to Desktop;
- no SCL mutation policy changed;
- no semantic projector was duplicated;
- the only new Desktop helper is `DepthToIndentConverter`, a presentation-only mapping from an existing integer depth to Avalonia Thickness.

## Automated evidence

Verified executable head:
`8259b15765c46e8f20f59b15b7025a1c1e6a92ea`

GitHub Actions run:
`37139948935`

Result per OS:
- SCL tests: 11/11;
- Engine/Desktop tests: 65/65;
- total: **76/76**;
- Windows: green;
- Ubuntu: green;
- macOS: green;
- Ubuntu build: 0 warnings / 0 errors.

Windows self-contained artifact:
`ARSCL-Studio-win-x64` — `11279419971`

Artifact SHA-256:
`e1569501819ca2b859e57b1acde86e1c61746df94c2aabc36734a4facf2adf24`

## Manual visual acceptance still required

Open the golden SCD at normal Windows scaling and inspect at minimum:
1. project browser density and selected IED readability;
2. whether all engineering workspace tabs fit naturally without the two-line/web-link appearance;
3. Data Model LN selection and DO/DA/BDA hierarchy readability;
4. Properties/Where Used legibility at realistic long paths;
5. splitter behavior for left/right panes and Problems dock;
6. 125% and 150% Windows DPI;
7. whether any column becomes unusably clipped at ~1366, 1500 and 1920 px widths.

Do not merge PR #5 until this manual screenshot audit is accepted or remaining visual defects are explicitly deferred.
