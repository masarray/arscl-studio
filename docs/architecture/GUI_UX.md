# GUI/UX architecture

## Reference principles

IEDScout contributes the IEC 61850 mental model:
- Navigation/IED pane
- contextual Details pane
- first-class DataSets, Reports, GOOSE, Data Model
- descriptions
- working collection concept similar to Activity Monitor

XML Notepad contributes editing interaction:
- synchronized structural tree and values/details
- direct contextual editing
- validation list with navigation
- schema-aware suggestions
- search
- undo/redo
- drag/drop with clear feedback

ARSCL Studio adds semantic surgery, impact analysis, merge/diff, compatibility, and safe export.

## Main shell

    Menu / command bar
    Document tabs
    ----------------------------------------------------------
    Explorer       Contextual Details            Context/Help
    ----------------------------------------------------------
    Problems | References | Search | Changes | Signal Basket
    ----------------------------------------------------------
    Status

No dashboard-style giant cards. This is a dense professional engineering application.

## Explorer

Tabs:
- Engineering
- XML

Engineering hierarchy emphasizes:
- Substation
- IEDs
  - Communication
  - DataSets
  - Reports
  - GOOSE
  - Setting Groups
  - Data Model
- Communication
- DataTypeTemplates
- Private/Extensions

Engineering and XML selections synchronize through SclNodeHandle.

## Details

Content depends on selected semantic kind.

Examples:
- DataSet -> overview, members, relations, XML
- ReportControl -> options, DataSet binding, clients, relations, XML
- LN/DO/DA -> model hierarchy, FC/CDC/type, descriptions, usage
- ConnectedAP -> network addressing
- DataType -> structure, forward references, where-used

Details are provider-based, not one giant conditional ViewModel.

## Context/help

A compact optional right pane explains:
- what the selected IEC object means
- semantic path
- FC/CDC/type
- where it is used
- impact of deletion/change
- diagnostic explanations

Experts can collapse it.

## Bottom tool window

Problems:
- severity
- diagnostic code
- domain
- object
- message
- source
- action

Double-click navigates to the exact engineering/XML object.

References:
- incoming/outgoing typed references

Signal Basket:
- selected HMI/gateway candidate signals
- DataSet coverage
- report coverage
- target compatibility summary

## Performance UI decisions

- Avalonia compiled bindings by default
- flattened virtualized tree instead of recursive container creation
- virtualized list/table rows
- lazy details
- no per-node control/event subscription
- batched updates from workers
- search debounce and latest-wins coalescing
- no hard dependency on commercial TreeDataGrid

## Editing

A visible Edit mode is contextual, but all modifications invoke Engine commands.

Drag/drop is semantic:
- dropping a DA/DO onto a DataSet proposes FCDA membership
- dropping an IED into merge staging imports it
- invalid drops are rejected before mutation

## Visual language

- compact system font
- restrained typography
- row height approximately 24-28 px
- semantic color only for status/selection/type badges
- minimal cards
- resizable panes
- keyboard-first navigation
- context menus
- persisted layout
- high-DPI and light/dark theme support
