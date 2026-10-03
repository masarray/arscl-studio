# ARSCL Studio

Cross-platform **IEC 61850 SCL engineering workbench** built with C# / .NET / Avalonia.

ARSCL Studio is intended for engineers who need to **open, understand, inspect, edit, repair, merge, validate, and safely export** ICD/IID/CID/SCD/SSD/SED files for HMI, SCADA, gateway, and system-engineering workflows.

The product direction combines:
- IEDScout-style IEC 61850 navigation and contextual details
- XML Notepad-style structured editing discipline
- ARSCL-specific reference-aware surgery, semantic diff/merge, compatibility analysis, and safe export

## Status

**M1B Deep IEC Semantic Browser & Lazy Tree — complete**

Current viewer capabilities include:
- secure real SCL loading
- deep lazy Engineering and XML navigation
- IED / AccessPoint / Server / LDevice / LN0 / LN
- DataSet / FCDA
- Report / Log / GOOSE / Sampled Value control blocks
- Inputs / ExtRef / setting-group control
- DOI / SDI / DAI
- DataTypeTemplates and typed type-reference chains
- Communication / ConnectedAP semantic relationships
- typed forward/reverse reference graph
- functional Where Used
- debounced/coalesced semantic search
- semantic source paths and source line/column
- large-model lazy projection regression gates

The next milestone is **M2 — Editing Kernel**.

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
- all future edits go through commands/transactions
- large models use lazy/virtualized projections
- background work is cancellable, bounded, coalesced, and revision-aware
- target compatibility is separate from IEC validity
- unknown/vendor XML must survive engineering round trips
- save/export will be transactional

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

The repository treats secure XML handling, canonical identity, reference ambiguity, cancellation, worker coalescing, memory ownership, source traceability, lazy virtualization, round-trip fidelity, and deterministic IEC semantics as implementation requirements rather than late-stage cleanup.

See `AGENTS.md` for the full engineering contract.
