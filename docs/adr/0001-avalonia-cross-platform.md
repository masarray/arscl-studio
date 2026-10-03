# ADR-0001: Avalonia cross-platform desktop

Status: Accepted
Date: 2026-10-03

## Decision

Use C#/.NET 10 and Avalonia 12 for the desktop application.

Avalonia is isolated to ArSclStudio.Desktop. SCL and Engine projects remain UI-framework independent.

## Consequences

Benefits:
- Windows/Linux/macOS path from one desktop codebase
- mature XAML/MVVM desktop interaction
- compiled bindings
- native desktop files, keyboard, drag/drop

Costs:
- UI performance must be designed around Avalonia virtualization/threading constraints
- platform-specific behavior requires CI and targeted integration tests

The open-source baseline will not depend on Avalonia Pro TreeDataGrid. Large trees use a flattened virtualized projection built on open-source Avalonia controls.
