# Project handoff

Last updated: 2026-10-06

## Stop point for this thread

This thread is intentionally closed at a **frozen M3 workstation baseline + finalization program handoff**.

Repository: `masarray/arscl-studio`  
Active branch: `feature/m3a0-iec-workstation-ia`  
PR: #5 (draft)  
Frozen runtime head before this documentation/CI handoff: `b1a212626db78bfb90a1bccddc412024742e00f4`

Runtime qualification evidence for that head:
- full Windows / Ubuntu / macOS build + test: green;
- Windows self-contained publish: green;
- qualification run: `37201697560`;
- PR-head CI: `37201701488` green;
- Windows artifact: `11302379663` (`ARSCL-Studio-win-x64`);
- artifact digest: `sha256:371ecd90d27ecb4e58df6e644672e5bd95398fa6920509ae2d30767799577e6b`.

The documentation/CI handoff commit after this runtime head does not intentionally change application runtime behavior.

## What PR #5 contains

Implemented and regression-covered foundations:
- secure SCL loading and preservation-oriented syntax document;
- stable node handles/source spans;
- semantic index + typed reference graph + ambiguity-safe resolution;
- lazy Engineering/XML projection, shared selection, Where Used and search;
- bounded/coalesced/revision-safe workers;
- transaction/undo/redo/atomic save kernel;
- source-linked diagnostics;
- real workspaces for Devices/IED context, Network, GOOSE/GSSE, DataSets, Reports, MMS/Data Model and Setting Groups;
- typed GSE/SMV communication binding diagnostics;
- DOI/SDI/DAI vs template consistency validation;
- declared Services projection and basic consistency checks;
- M3UX1-M3UX5 dense workstation evolution.

M3UX5 specifically:
- removes selected heavy GOOSE/DataSet/Report/Data Model/Setting detail work from the synchronous UI selection path;
- uses latest-wins cancellation + revision/request checks for engineering details;
- adds cancellation checkpoints to Setting projection;
- moves the shell toward station/domain-first workflow;
- restores station-wide Network rows;
- removes the IED dropdown as primary workflow authority;
- exposes compact domain tabs: Devices / Network / GOOSE / DataSets / Reports / MMS Data / Settings.

## Acceptance gate before merging PR #5

PR #5 is now **scope-frozen**. Do not add new feature scope.

Only fix blockers found by this acceptance:

1. C264 -> Reports: rapidly select/switch RCBs; no busy freeze, crash, stale detail, or forced close.
2. BCUGE/GE -> Settings: rapidly switch Settings -> MMS Data -> Reports -> Settings; no freeze/crash/stale publish.
3. Network shows station-wide IED/AP/IP/subnet/gateway rows.
4. Domain navigation has one obvious authority; no confusing duplicate IED/domain selector.
5. Normal/high-DPI screenshots at 100%, 125%, 150% show no clipping/wrapping regression.
6. Existing golden SCD still opens, navigates, saves safely, and diagnostics remain source-linked.

If this gate passes, merge #5. If it fails, only the minimum acceptance-blocker fix remains on #5.

## Audit: unfinished product work

The current product is a strong inspection baseline, **not yet a final system configuration tool**.

### Reliability/performance — partial

- selected detail projections are now asynchronous, but IED-scoped catalog refresh still invokes multiple projectors synchronously;
- multiple workspace projectors independently scan `SemanticIndex.Nodes`, causing repeated O(N) scans on IED changes;
- Setting projection is cancellable but still rebuilds resolved Data Model per LN and can repeat type traversal;
- leak tests prove important collectability properties, but rapid open/navigate/cancel/close stress is not yet a release gate;
- timing tests exist, but hot workspace allocation/latency profiling is incomplete.

Owner: issue #8.

### Standards authority — incomplete

- `SclSchemaRevision` currently exposes raw Version/Revision/Release strings;
- no typed edition/namespace profile maps the current SCL Edition 2.2 baseline;
- default schema provider is deliberately unavailable;
- no legally sourced XSD/rule pack is configured;
- IEC TS 61850-6-3:2025 machine-processable rules are not integrated;
- edition-specific rule governance needs a single provider architecture.

Owner: issue #9.

### Semantic/index completeness — partial

Strong coverage exists for current M3 domains, but remaining work includes:
- deep Substation/VoltageLevel/Bay/ConductingEquipment/connectivity semantics when real fixture evidence is available;
- complete engineer-facing Sampled Values workflow;
- deeper Log/Inputs/ExtRef consistency;
- EnumType/value normalization and additional data-model semantics;
- hot-query typed indexes by kind/IED/LD/LN instead of repeated full scans;
- explicit evaluation of newer engineering-file concepts without pretending unsupported roles are understood.

Owner: issue #10.

### Safe engineering mutations — largely not implemented

The intentionally narrow mutation policy still allows only proven-safe property editing. DataSet surgery, RCB engineering, GOOSE/SMV/Communication editing, identity rename/delete and deterministic quick fixes remain locked.

Owner: issue #11.

### Diff/extract/export/merge — not implemented as production workflow

Semantic diff, IED extraction, file-role-aware export preflight and deterministic multi-file merge remain roadmap work.

Owner: issue #12.

### Interoperability evidence — incomplete

Current evidence proves internal parsing/semantics/tests and some real-file behavior. It does **not** prove broad multi-vendor tool interoperability or formal IEC 61850 engineering-tool conformance.

Need a versioned fixture matrix, cross-tool import/export evidence, runtime checks where available, and an IEC 61850-10/UCA readiness plan.

Owner: issue #13.

### Final UX/release — partial

The station-first shell is moving in the correct direction, but final multi-DPI acceptance, accessibility/keyboard polish, release packaging/supportability and evidence-scoped product claims remain.

Owner: issue #14.

## Finalization control plane

Master issue: #7

Lane issues:
- #8 P0 Reliability & performance
- #9 P1 Standards authority / edition-rule providers
- #10 P2 Semantic/index completeness
- #11 P3 Safe SCL surgery
- #12 P4 Diff/export/merge
- #13 P5 Interoperability evidence
- #14 P6 Final UX/release

Read `docs/continuation/FINALIZATION_MASTER_PLAN.md` for dependencies, parallelism and acceptance rules.

## Work that must NOT be redone

Do not restart these from scratch:
- authoritative syntax + node-handle model;
- reference graph and ambiguity policy;
- session revision model;
- bounded latest-wins worker primitive;
- transaction/undo/redo/atomic save boundary;
- existing semantic projectors/tests;
- shared selection/navigation;
- M3 engineering information architecture.

Improve measured bottlenecks in place. A rewrite requires profiler/test evidence that the current architecture cannot meet a release requirement.

## Next-thread startup protocol

A new coding thread should begin with:

```
@GitHub repo masarray/arscl-studio
Read AGENTS.md, docs/continuation/HANDOFF.md,
docs/continuation/FINALIZATION_MASTER_PLAN.md,
and the exact GitHub issue I am assigning.
Audit current authority before coding.
Do not create a second authority or redo completed work.
Keep the branch buildable, obey the CI budget, and finish the issue acceptance criteria.
```

First action is the PR #5 acceptance gate. After #5 merges, start parallel work only according to issue #7 dependency lanes.
