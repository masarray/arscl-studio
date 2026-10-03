# Project handoff

Last updated: 2026-10-03

## Current phase

**M2B — Validation & Reference Diagnostics: COMPLETE (automated code acceptance).**

- Repository: `masarray/arscl-studio`
- Branch: `feature/m2b-validation-reference-diagnostics`
- PR: #4
- Base: M2A main `8c143b7bf4c9b044429c4da57c0ccf00371f9489`
- Verified executable-code head: `22bec93f3c564d2e98c17ace2cf0b71046b85f45`
- Verified code CI: run `37117698045`
- Windows / Ubuntu / macOS: build + **55 tests per OS passed** (10 SCL + 45 Engine/Desktop)
- Windows self-contained desktop artifact from the verified code run: `11271414372`
- Documentation-only commits after the verified code head do not change executable behavior; final PR/main CI must still remain green.

## Read first

1. `AGENTS.md` — mandatory reliability, ownership and no-naive-coding contract.
2. `docs/adr/0007-isolated-property-transactions.md` — M2A transaction/save invariants.
3. `docs/adr/0008-validation-reference-diagnostics.md` — M2B validation/reference invariants.
4. `docs/testing/M2B_ACCEPTANCE.md` — exact automated evidence and limits.
5. Existing architecture/IEC strategy and ADRs 0001–0006.

ARSCL remains a cross-platform Avalonia/.NET 10 IEC 61850 engineering workbench. Desktop never mutates SCL XML. The preservation-oriented syntax document is authoritative; semantic/reference/validation objects are projections over stable source handles.

## Stable baseline retained

From M1B/M2A:
- secure SCL loader and vendor/private XML preservation;
- stable `SclNodeHandle` and source spans;
- lazy flattened Engineering/XML trees and virtualized lists;
- IED/AP/Server/LD/LN, DataSet/FCDA, report/log/GOOSE/SV controls, Inputs/ExtRef, DOI/SDI/DAI and type templates;
- typed resolved reference graph, Where Used and semantic search;
- compound transaction staging, rollback, bounded patch history, undo/redo;
- only direct standard-namespace `IED.desc` is editable;
- verified atomic Save/Save As with external-modification protection;
- cancellation, bounded workers, latest-wins work, stale revision rejection and lifecycle tests.

## M2B implemented scope

### Reference evidence

The reference graph now retains both:
- resolved typed edges;
- `SclReferenceIssue` records for unresolved or ambiguous targets.

The resolver distinguishes Missing / Unique / Ambiguous. Duplicate identities are never resolved by first/random match.

Current diagnostics cover the reference kinds already supported by the M1B graph, including:
- LN/type-template references;
- DO/DA type references;
- DataSet control bindings;
- ConnectedAP → IED/AP;
- FCDA → logical node;
- ExtRef → source logical node.

Do not assume this is every possible IEC 61850 reference yet.

### Validation

- `SclValidationSnapshot` is stamped with its source `DocumentRevision`.
- reference diagnostics contain source handle, path/span and revision;
- `SCL-REF-0001`: unresolved;
- `SCL-REF-0002`: ambiguous/no-guess;
- Reference, Schema, Semantic, Engineering and Compatibility remain distinct domains;
- `ValidateFastAsync` and `ValidateFullAsync` use the existing latest-work coordinator;
- same-kind newer validation supersedes/cancels older work;
- stale revision output is rejected before publication.

### Schema provider

`ISclSchemaProvider` is the pluggable boundary for legally sourced schema assets.

The default provider is intentionally unavailable and reports that state explicitly. ARSCL does not embed normative IEC schema text whose redistribution rights have not been verified.

A configured provider reports:
- provider ID;
- Available / Unavailable / UnsupportedRevision;
- message;
- provenance.

Fast validation can report that a provider is available while deferring heavy XSD work. Full validation invokes the provider when Available.

### Desktop

Problems is now a real source-navigation surface:
- rows retain the diagnostic `SclNodeHandle`;
- semantic object names are projected when known;
- selecting a problem navigates through the shared selection service;
- required Engineering/XML ancestors are expanded lazily;
- stale validation snapshots are not published.

A well-formed SCL with reference errors still opens for recovery/inspection. M2B never silently repairs it.

## Tests and evidence

Verified code run `37117698045` is green on Windows, Ubuntu and macOS.

Per OS:
- SCL: 10/10;
- Engine/Desktop: 45/45;
- total: **55/55**.

New tests prove:
- unresolved reference diagnostics;
- ambiguous no-guess diagnostics;
- revision/source linking;
- explicit schema-unavailable status;
- configured schema-provider findings;
- fast-validation latest-wins cancellation;
- Problems source navigation.

All M2A gates remained green: transactions, rollback, undo/redo, save verification, preservation, race handling, lifetime/collectability and 100k-DAI regression.

See `docs/testing/M2B_ACCEPTANCE.md` for exact evidence.

## Important limits

- M2B is **not** complete IEC 61850 XSD/NSD/semantic validation.
- No normative IEC XSD/NSD assets are bundled.
- Fast validation currently emphasizes reference integrity and schema-provider state; semantic/engineering rule coverage expands in M3.
- Existing reference diagnostics cover modeled M1B reference kinds, not every reference form in every edition.
- Target compatibility remains a separate future profile layer.
- Identity rename/delete, DataSet surgery, RCB editing/removal, merge and broad XML editing remain locked.
- Desktop still has no generic XML mutation path.
- Existing source spans describe the current loaded/verified syntax state.
- Real vendor-tool import, native dialog and DPI/layout acceptance remains manual.

## Next milestone: M3A — Semantic Completeness & Engineering Diagnostics

Do **not** rebuild the browser/reference/search foundations. Extend them systematically.

Acceptance target:

1. Deepen Substation semantics:
   - Substation → VoltageLevel → Bay;
   - ConductingEquipment;
   - Terminal/connectivity context;
   - LNode placement/binding.
2. Deepen Communication semantics:
   - Address/P interpretation such as IP, subnet, gateway, MAC, APPID and VLAN where represented by SCL;
   - GSE/SMV communication endpoint linkage.
3. Interpret supported Services without inventing unsupported capability.
4. Complete more DOI/SDI/DAI ↔ DataTypeTemplates chains and surface unresolved/ambiguous model findings.
5. Add typed Semantic/Engineering diagnostics with source navigation and revision-safe publication.
6. Keep schema-provider assets legally sourced and edition-aware; do not hard-code copied normative text.
7. Preserve lazy/virtualized UI, bounded allocation, cancellation/coalescing, round-trip fidelity and all M1B/M2A/M2B gates.
8. Do not broaden destructive edit policy until semantic/reference impact coverage for that operation is explicitly proven.

M4 remains the first broad SCL Surgery phase after these semantic gates are mature.
