# Project handoff

Last updated: 2026-10-03

## Current phase

M0 — Foundation

## Product intent

ARSCL Studio is a cross-platform Avalonia IEC 61850 SCL viewer/editor/validator/surgery workbench. The primary use case is preparing trustworthy SCL for HMI/workstation/gateway import, especially when engineers must inspect or repair multi-vendor files without the original vendor engineer.

## UX direction

Do not build a dashboard-style generic IDE.

The accepted direction combines:
- IEDScout-style IEC navigation: IED, DataSets, Reports, GOOSE, Data Model, contextual details
- XML Notepad-style structured editing: synchronized tree/value concept, validation list, search, undo/redo, schema-aware editing
- ARSCL-specific semantic surgery, impact analysis, merge/diff, compatibility profiles

See docs/architecture/GUI_UX.md.

## Architecture direction

C#/.NET 10 + Avalonia 12 cross-platform.

Projects:
- ArSclStudio.Scl
- ArSclStudio.Engine
- ArSclStudio.Profiles
- ArSclStudio.Desktop
- ArSclStudio.Cli

Engine is headless. Desktop never edits XML directly.

Authoritative source is SCL syntax; semantic model is an indexed projection, not a separately serialized copy.

See AGENTS.md before coding.

## Reliability/performance commitments

- secure XML defaults
- document revisions
- stale worker result rejection
- coalesced latest-state work
- bounded concurrency
- cancellation
- document-scoped caches/pools
- explicit disposal
- virtualized flattened explorer
- compiled Avalonia bindings
- transactional save/export
- vendor/private XML preservation
- round-trip gates

## Immediate next acceptance target

Complete M0 so CI proves:
1. solution restores/builds on Windows, Linux, macOS
2. unit tests pass
3. secure SCL probe can identify a minimal file without enabling DTD/external resolution
4. worker coalescing/revision guards have tests
5. Desktop shell launches/builds with Engineering/XML explorer layout
6. CLI references the same headless engine

## Deferred deliberately

Do not implement yet:
- full SCT / SLD editor
- online controls
- simulator
- packet sniffer
- full GOOSE/SV analyzer
- plugin marketplace
- AI auto-repair
