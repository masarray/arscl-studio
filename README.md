# ARSCL Studio

Cross-platform IEC 61850 SCL engineering workbench built with **C# / .NET / Avalonia**.

ARSCL Studio is being designed for engineers who need to **open, understand, inspect, edit, repair, merge, validate, and safely export** IEC 61850 SCL files (ICD/IID/CID/SCD/SSD/SED) for HMI, SCADA, gateway, and system-engineering workflows.

The product direction combines:

- the IEC 61850 browsing mental model of tools such as OMICRON IEDScout;
- the structured editing discipline of Microsoft XML Notepad;
- a dedicated semantic SCL engine with reference-aware surgery, validation, diff/merge, compatibility analysis, and future live-IED verification.

> Status: **M0 Foundation** — architecture, contracts, reliability/performance guardrails, and the first executable skeleton are being established before feature expansion.

## Core product principles

1. **IEC 61850 semantic model first.** Raw XML is an expert representation, not the product's primary mental model.
2. **Lossless round trip.** Unknown/vendor XML, namespaces, comments, `Private` elements, and extensions must not be silently destroyed.
3. **Safe editing.** The GUI never mutates XML directly. All edits go through command/transaction layers with impact analysis, validation, undo/redo, and rollback.
4. **Engine and GUI are independent.** The SCL engine is headless and reusable by desktop UI, CLI, tests, and future integrations.
5. **Large-model performance is a design constraint, not a later optimization.** Parsing, indexing, validation, search, and tree projection are designed for very large SCL models.
6. **Standard validity and target compatibility are separate.** IEC validation must never be conflated with vendor/product import behavior.
7. **No naive coding.** Architecture, concurrency, cancellation, memory ownership, coalescing, caching, canonicalization, and deterministic behavior are part of the implementation contract.

See [AGENTS.md](AGENTS.md) and [docs/architecture/](docs/architecture/) once the M0 foundation lands.
