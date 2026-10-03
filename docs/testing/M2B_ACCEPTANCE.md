# M2B — Validation & Reference Diagnostics acceptance

Date: 2026-10-03

## Scope accepted

M2B proves the validation/reference boundary required before identity/delete surgery.

Implemented:
- structured unresolved and ambiguous reference evidence;
- deterministic no-guess handling for duplicate identities;
- revision-stamped and source-linked diagnostics;
- separate Reference and Schema domains;
- `ISclSchemaProvider` boundary with explicit unavailable/deferred state and provenance;
- cancellable/latest-wins `ValidateFast` and `ValidateFull`;
- stale-revision publication rejection;
- Problems navigation to the diagnostic source using `SclNodeHandle`;
- recovery viewing for well-formed SCL that contains reference-integrity errors.

Not implemented:
- bundled IEC XSD/NSD assets;
- a claim of complete IEC 61850 validation;
- complete semantic/engineering rule coverage;
- target compatibility certification;
- identity rename/delete, DataSet surgery or RCB editing.

## Automated evidence

Verified code head:

`22bec93f3c564d2e98c17ace2cf0b71046b85f45`

GitHub Actions run:

`37117698045`

Result:
- Ubuntu: build + tests green;
- macOS: build + tests green;
- Windows: build + tests green;
- Windows self-contained desktop publish green.

Per operating system:
- 10 SCL tests;
- 45 Engine/Desktop tests;
- **55 total tests**.

New M2B tests prove:
- unresolved DataSet reference produces `SCL-REF-0001`;
- duplicate/ambiguous type identity produces `SCL-REF-0002` and no guessed target edge;
- diagnostics carry source path, source span and document revision;
- default schema unavailability is explicit rather than silently skipped;
- configured schema-provider findings remain in the Schema domain;
- fast validation is latest-wins and cancels/supersedes obsolete same-kind work;
- Problems selection navigates to the source engineering object.

Existing regression gates remained green, including:
- secure/preservation-oriented loading;
- vendor XML round-trip;
- atomic Save/Save As and external-modification protection;
- transaction rollback and undo/redo;
- cancellation/disposal and snapshot collectability;
- deep lazy tree/search/reference tests;
- 5,000-IED and 100k-DAI regression fixtures.

The 100k-DAI fixture is a regression gate, not a user latency guarantee. On the Ubuntu runner in this M2B code-head run it reported one edit over 205,010 indexed syntax nodes at 341.1 ms and 36,307,144 allocated bytes; the gate passed.

## Artifacts

Code-head run artifacts:
- Windows self-contained desktop: artifact `11271414372`;
- Windows TRX: `11271563785`;
- macOS TRX: `11272212851`;
- Ubuntu TRX: `11272411500`.

A later documentation-only head may have a different run/artifact ID while leaving the verified executable code unchanged.

## Manual checks still useful

Automated acceptance does not replace:
- native file-dialog smoke testing;
- DPI/layout inspection on real desktops;
- opening curated vendor SCL files;
- importing saved output into real target engineering tools when that target profile is implemented.

Any real vendor compatibility result must be reported separately from IEC/reference validation.
