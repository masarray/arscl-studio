# ADR-0007: Isolated property transactions and bounded patch history

Status: Accepted
Date: 2026-10-03

## Decision

M2A supports the **unqualified `desc` attribute of a direct SCL IED** in the standard SCL namespace. It does not expose generic attribute mutation. Namespace, identity, DataSet membership, RCB configuration, and vendor extensions remain read-only.

Commands prepare immutable property patches with expected old values. The kernel also requires an expected document revision, validates every target against the Engine policy, and rejects duplicate targets in a compound command. The GUI does not decide which IEC properties are safe.

A document-scoped exclusive queue admits at most 16 operations and runs one explicit edit/save/open-commit at a time. Edits are not coalesced away. Latest-state search continues to use the existing coalescing coordinator. Both queues are cancelled and drained on disposal.

Prepare one isolated syntax copy per compound edit, apply patches there, run fast post-edit checks, then publish one state/revision. A cancelled or rejected candidate is discarded, so published readers never observe partially mutated XML. Final publication rechecks revision, lifetime and state identity under the state gate. Open also rechecks its request sequence at this boundary.

The syntax copy uses a cancellable iterative traversal, retains existing node handles and source provenance, and does not serialize/parse a giant intermediate string. Removed attributes leave no registry objects or tombstone arrays behind. Recreated attributes receive fresh handles; surviving elements and attributes retain handles.

`desc` does not participate in the current semantic identity, display, badge, or reference indexes. Those immutable indexes can therefore be shared between description revisions. This is an **explicit dependency decision for this one property**, not a generic permission to skip reindexing. Every new editable property must declare and rebuild/update its affected projections.

## History, journal and dirty state

History stores patches, never syntax snapshots, GUI objects or sessions. Limits:

- 64 property changes per transaction;
- 4,096 characters in both old and new values;
- 128 undo transactions;
- 1,048,576 total characters per retained history and per journal;
- 256 journal entries;
- 256 characters per transaction description.

Undo/redo execute the same staging and validation path. Undo and redo each advance the monotonic revision. A fresh nonempty edit clears redo. No-op and rejected edits do not alter revision, journal or redo. Saved content has a separate identity, so undoing back to the saved state clears dirty status even though the revision keeps increasing. Oldest entries are evicted when a bound is reached; the Changes UI explicitly calls this recent bounded history, not a permanent audit log.

## Save

Save is serialized with edits. It checks the expected revision and destination SHA-256, streams UTF-8 XML to a unique same-directory temporary file, flushes it, reopens through the production secure loader, and compares every XML node/attribute/value, including comments, PI, namespace prefixes, vendor extensions and whitespace. Only declaration encoding may change. Existing files use `File.Replace`; new files use a non-overwriting `File.Move`. No delete-then-copy fallback exists.

The verification walk rebinds current handles to reopened nodes/source locations without rebuilding IEC indexes. File role changes are rejected by Save As. Source fingerprints are captured before and after open and checked before save/commit. Existing Save As destinations require explicit overwrite.

After atomic replacement begins, success is reported even if a cancellation arrives immediately afterward. Failed/cancelled operations preserve the prior destination and dirty/history state. Cleanup is best effort and cannot disguise a completed commit.

## Costs and limits

- Staging is O(N) time and temporary memory for the XML/registry, even for one property. No claim of incremental DOM storage is made. One active writer and patch-only history bound retained state. The 100k-DAI regression records time/allocation; a future persistent syntax/patch layer can reduce staging cost without changing command semantics.
- XML/vendor fidelity is semantic/structural, not byte identity: encoding, entity spelling and quote style can change.
- Source locations refer to the loaded source until successful save; after save they refer to the verified output. Newly created attributes have unknown locations until then.
- Fast edit validation proves allowed target, XML character/value limits and patch postconditions. It is not XSD/NSD, complete IEC validation or SICAM certification. Existing unsupported/dangling model semantics remain visible/preserved.
- Filesystem rename atomicity depends on the underlying filesystem. SHA checks detect observed external changes; there remains an unavoidable small race with a non-cooperating external writer between final fingerprint check and rename. No power-loss durability claim is made for directory metadata or network shares.
- Desktop native dialogs and rendered visuals still require a real OS smoke test; cross-platform compilation and ViewModel integration tests are automated.

## Next slice

M2B must add edition/schema provider validation and explicit unresolved/ambiguous-reference diagnostics before broadening mutations. Identity changes, deletion and SCL surgery require a reference/impact plan; do not extend `SetDescription` into a generic XML editor.
