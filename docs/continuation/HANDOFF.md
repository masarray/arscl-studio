# Project handoff

Last updated: 2026-10-03

## Current phase

**M2A — Transaction Kernel & Safe Property Editing: implementation complete; final verification in progress.**

- Repository: `masarray/arscl-studio`
- Branch: `feature/m2a-transaction-kernel`
- PR: [#3](https://github.com/masarray/arscl-studio/pull/3)
- Base: M1B main `dbfd84461aacfb81eb3cd502c176aee91ba54bc3`
- First engine build passed Windows/Ubuntu/macOS: run `37114720037`.
- Full test matrix is being finalized; inspect PR #3 checks for the current exact head.

## Read first

1. `AGENTS.md` — mandatory reliability, ownership and architectural contract.
2. `docs/adr/0007-isolated-property-transactions.md` — M2A invariants, costs and limits.
3. `docs/testing/M2A_ACCEPTANCE.md` — automated gates and real desktop smoke procedure.
4. Existing architecture/IEC strategy and ADRs 0001–0006.

The product remains a cross-platform Avalonia/.NET 10 IEC 61850 engineering workbench. Desktop never mutates XML. There is one authoritative preservation-oriented syntax document; semantic/reference indexes are projections. Unknown XML is preserved even when its semantics are not understood.

## Existing M1B baseline retained

- Secure SCL loader, stable node handles and loaded-source spans.
- Lazy flattened Engineering/XML trees and virtualized lists.
- IED/AP/Server/LD/LN, DataSet/FCDA, report/log/GOOSE/SV controls, inputs/ExtRef, DOI/SDI/DAI and type templates.
- Typed resolved reference graph, ambiguity-sensitive lookup, Where Used and semantic search.
- Search debounce/coalescing, revision guards, bounded background workers and lifecycle tests.

## M2A implemented scope

### Engine

- `ISclEditCommand`, `SetIedDescriptionCommand`, `CompoundEditCommand`.
- Only direct standard-namespace `IED.desc` is editable; identities/references/vendor properties are not exposed for mutation.
- Explicit expected revision and expected old value; absent and empty are distinct.
- Bounded exclusive operation queue shared by edit/save/open publication.
- One cancellable staging syntax copy per compound transaction; published readers stay immutable.
- Fast policy/postcondition validation, optional validator veto, discard-on-failure rollback.
- Stable surviving node handles and reuse of unaffected semantic/reference indexes for description-only edits.
- Undo/redo and redo invalidation; no-op does not advance revision or clear redo.
- Patch-only bounded history/journal, independent saved-content identity and dirty state.
- Open request/revision checks repeated at the commit boundary; stale search result checks at Engine and UI publication.
- Document close cancels/drains work and releases owned state.

### Save

- Same-directory unique temporary file, cancellable streaming serialization and file flush.
- Production-parser reopen and complete XML content comparison, including vendor data.
- Rebind original handles to verified output with refreshed source locations.
- Atomic replace for an existing file; atomic non-overwriting move for a new file.
- SHA-256 checks reject observed external modifications.
- Save As preserves the SCL file role/extension and requires explicit overwrite for an existing different path.
- UTF-8 output; UTF-16/BOM input and declaration-free input covered by tests.
- Failed/cancelled save preserves destination, dirty state and history.

### Desktop and CLI

- IED description editor with Apply, Cancel and explicit removal.
- Draft retains its original IED/revision even when selection changes.
- Read-only description field, dirty indicator, Undo/Redo and virtualized recent Changes.
- Save/Save As and unsaved-change Open/Close prompts.
- Async close/disposal avoids the former UI-thread blocking wait.
- CLI: `arscl set-description input.scd Relay_A "Feeder A" output.scd` uses the same Engine.
- CI provides a self-contained Windows desktop artifact and per-OS TRX test evidence.

## Tests and evidence

See `docs/testing/M2A_ACCEPTANCE.md`. Existing M1B gates remain in CI.

The full test run includes transactions, compound rollback, revision races, cancellation/disposal, bounded history/registry, snapshot collectability, preservation/encoding, failed save paths and Desktop ViewModel integration. A 100k-DAI fixture measures staging time/allocation and asserts index reuse and collapsed-tree laziness. Measurements are printed in CI/TRX; do not extrapolate them to all vendor SCDs or all machines.

## Important limits

- M2A fast validation is not complete IEC/XSD/NSD validation or target compatibility certification.
- Staging remains O(N) XML/registry copy time and temporary memory. History does not retain whole documents. Do not claim incremental DOM storage.
- Existing source spans describe loaded input until successful save; new attributes have unknown spans until save.
- Fidelity is structural/semantic/vendor content, not byte-for-byte source identity.
- Recent history is bounded, not a permanent audit log.
- Real native-dialog, DPI/layout and vendor-tool import testing remains a manual acceptance step.
- No reference deletion/rename, DataSet surgery, RCB editing, merge, schema packs, SICAM rules or live MMS work is included.
- File fingerprint checks cannot completely eliminate a race with a non-cooperating external writer between check and filesystem rename. See ADR-0007.

## Next milestone: M2B — Validation & Reference Diagnostics

Acceptance target:

1. Add revision-stamped, source-linked unresolved/ambiguous reference diagnostics (M1B currently stores resolved graph edges only).
2. Introduce legally sourced schema-provider plumbing and make skipped/unavailable validation explicit.
3. Separate XML, schema, model/reference, engineering and target-compatibility findings.
4. Add cancellable/coalesced fast validation publication with source navigation.
5. Preserve recovery viewing for pre-existing invalid SCL without silently repairing it.
6. Prove reference/validation gates before broadening the edit policy or beginning identity/delete operations.
7. Keep no-edit/edited preservation, compound rollback, memory/lifetime and cross-platform CI green.

Do not add generic XML mutation to Desktop or treat absent graph edges as proof that an object is safe to delete. New editable fields require a typed command, impact classification, affected-index plan, reversible patch and tests.
