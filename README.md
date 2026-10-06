# ARSCL Studio

Cross-platform **IEC 61850 SCL engineering workbench** built with C# / .NET / Avalonia.

ARSCL Studio is intended for engineers who need to **open, understand, inspect, edit, repair, merge, validate, and safely export** ICD/IID/CID/SCD/SSD/SED files for HMI, SCADA, gateway, and system-engineering workflows.

The product direction combines:
- IEDScout-style IEC 61850 navigation and contextual details
- XML Notepad-style structured editing discipline
- ARSCL-specific reference-aware surgery, semantic diff/merge, compatibility analysis, and safe export

## Status

**M3A0–M3A2 IEC 61850 engineering workstation + M3UX5 reliability/workflow baseline**

The current M3 branch provides Engine-backed engineering workspaces for:
- IED Overview
- Network
- GOOSE / GSSE
- DataSets
- Reports & Logs
- Data Model
- Setting Groups

Secondary expert/source views remain:
- Model Tree
- XML

Implemented engineering depth includes:
- secure real SCL loading for ICD/IID/CID/SCD/SSD/SED
- deep lazy semantic and XML navigation
- typed forward/reverse reference graph and Where Used
- semantic search with cancellation/coalescing/stale-result protection
- transaction kernel with undo/redo and verified atomic Save / Save As
- source-linked reference and engineering diagnostics
- Communication/GSE and SMV endpoint semantics
- LN → LNodeType → DO → DOType → DA/SDO → DAType/BDA resolution
- DOI/SDI/DAI instance-value overlay and consistency diagnostics
- SettingControl / FC=SG engineering projection
- per-IED declared Services projection and conservative consistency checks
- compact three-pane Avalonia engineering desktop with virtualized large-model views
- compact selected-IED + engineering-domain navigator
- workspace-aware Problems/Search/Where Used navigation
- Reports bound-DataSet member inspection
- collapsible Data Model inspection
- selected-IED Network scope
- collapsed diagnostics dock with explicit Error/Warning/Info counts
- object-aware left engineering navigator
- inspection-first center panes and engineering breadcrumbs

The golden real-SCD engineering baseline is implemented and CI-green. PR #5 is scope-frozen pending the M3UX5 Windows/high-DPI + former-crash-path acceptance gate. The remaining product program is tracked by master issue #7 and lane issues #8-#14.

Broad destructive SCL surgery remains intentionally locked. Identity rename/delete, DataSet surgery, RCB surgery, broad Communication editing, merge, and target-aware export continue in later milestones after explicit reference-impact and validation coverage exists.

See [finalization master plan](docs/continuation/FINALIZATION_MASTER_PLAN.md), [standards baseline](docs/standards/IEC61850_STANDARDS_BASELINE.md), [M3UX4 acceptance](docs/testing/M3UX4_VISUAL_WORKFLOW_ACCEPTANCE.md), [M3A2 acceptance](docs/testing/M3A2_MODEL_SERVICES_ACCEPTANCE.md), and [current handoff](docs/continuation/HANDOFF.md).

## Architecture

Projects:

```text
ArSclStudio.Desktop  -> ArSclStudio.Engine -> ArSclStudio.Scl
                     -> ArSclStudio.Profiles

ArSclStudio.Cli      -> ArSclStudio.Engine + ArSclStudio.Profiles
```

Key rules:
- the desktop never mutates SCL XML directly
- syntax is authoritative; semantic models are projections/indexes
- semantic references resolve through typed graph edges
- ambiguous identity is never resolved by choosing an arbitrary first match
- all edits go through commands/transactions
- large models use lazy/virtualized projections
- background work is cancellable, bounded, coalesced, and revision-aware
- target compatibility is separate from IEC validity
- unknown/vendor XML must survive engineering round trips
- save is transactional; future export must use the same verified boundary

Read **[AGENTS.md](AGENTS.md)** before making code changes.

## Documentation

- [Architecture](docs/architecture/ARCHITECTURE.md)
- [Performance](docs/architecture/PERFORMANCE.md)
- [Reliability](docs/architecture/RELIABILITY.md)
- [IEC 61850 model strategy](docs/architecture/IEC61850_MODEL_STRATEGY.md)
- [GUI/UX](docs/architecture/GUI_UX.md)
- [Implementation plan](docs/roadmap/IMPLEMENTATION_PLAN.md)
- [Current handoff](docs/continuation/HANDOFF.md)
- [IEDScout/XML Notepad research](docs/research/IEDSCOUT_XMLNOTEPAD_FINDINGS.md)

## Build

Requires .NET 10 SDK.

```bash
dotnet restore ArSclStudio.sln
dotnet build ArSclStudio.sln --configuration Release
dotnet test ArSclStudio.sln --configuration Release
```

Run the desktop application:

```bash
dotnet run --project src/ArSclStudio.Desktop
```

Probe an SCL file with the headless CLI:

```bash
dotnet run --project src/ArSclStudio.Cli -- probe path/to/station.scd
```

## Engineering quality

The repository treats secure XML handling, canonical identity, reference ambiguity, cancellation, worker coalescing, memory ownership, source traceability, lazy virtualization, round-trip fidelity, deterministic IEC semantics, and screenshot-driven workstation usability as implementation requirements rather than late-stage cleanup.

See `AGENTS.md` for the full engineering contract.
