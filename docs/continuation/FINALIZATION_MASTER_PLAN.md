# ARSCL finalization master plan

Status: active program authority  
Created: 2026-10-06  
Master issue: https://github.com/masarray/arscl-studio/issues/7

## 1. Product target

ARSCL Studio is to become a production-grade IEC 61850 SCL engineering workstation for real multi-vendor substation work.

The target is not "an XML editor that recognizes SCL". The target is a deterministic engineering system that can:
- understand station, IED, communication, service and type semantics;
- preserve vendor-specific content;
- diagnose broken or ambiguous engineering;
- perform reference-impact-safe changes;
- export/merge without semantic damage;
- remain responsive on large SCDs;
- produce explicit evidence for interoperability claims.

The workflow model is substation-engineer-first:
`Devices | Network | GOOSE | Reports | DataSets | MMS Data | Settings`
with XML as expert/source evidence.

## 2. Current product scorecard

| Area | State | Release blocker |
|---|---|---|
| Secure XML/SCL loading | strong baseline | no |
| Lossless/vendor preservation | strong baseline; keep gating | yes for every new mutation |
| Semantic/reference core | strong partial | yes for uncovered domains |
| UI inspection workspaces | strong M3 baseline | final acceptance pending |
| UI-thread heavy work | improved M3UX5; not fully eliminated | yes |
| Edition-aware schema/rules | provider skeleton only | yes |
| SCL Ed2.2 authority | not yet modeled as typed profile | yes |
| Substation semantics | deferred by fixture evidence | depends on release scope |
| SMV engineering UI | diagnostics/linkage only | depends on release scope |
| DataSet/RCB/communication editing | locked | yes for configurator claim |
| Semantic diff/export | not production-ready | yes |
| Multi-file merge | not implemented | yes for system-tool claim |
| Compatibility profiles | descriptor/provenance skeleton | yes for target claims |
| Multi-vendor interoperability matrix | not yet systematic | yes |
| Formal engineering-tool conformance | not claimed | no false claims allowed |
| CI efficiency | previously wasteful; policy corrected in this handoff | continuous |
| Release packaging/supportability | partial | yes |

## 3. Architecture authority

Dependency direction remains:

```
Desktop -> Engine -> Scl
        -> Profiles -> Engine/Scl

Cli -> Engine + Profiles
```

### One authoritative document

The preservation-oriented syntax document is authoritative.

Semantic, reference, search, validation and workspace models are revision-scoped indexes/projections over that same document. They are not separately editable copies.

Every engineering object must resolve to:
- `SclNodeHandle`;
- deterministic semantic identity where possible;
- source location;
- current document revision.

### Five correctness domains

Keep these separate in code, diagnostics and UI:

1. XML/SCL syntax/schema validity.
2. IEC semantic/reference/model consistency.
3. Engineering-system consistency.
4. Vendor/target compatibility.
5. Live-device/runtime reality.

A failure/success in one domain never silently proves another.

## 4. Performance model

### Known hot-path debt

The branch already has a bounded worker coordinator, but several catalog projectors still find IED scope by scanning all semantic nodes. One IED selection can invoke several such scans. Settings may also rebuild LN data-model rows repeatedly.

M3UX5 fixes the most dangerous synchronous detail work, but release hardening must finish the job.

### Target execution model

```
document parse
  -> immutable revision state
  -> revision-scoped typed indexes
  -> cheap catalog snapshots
  -> selected-detail latest-wins workers
  -> coarse UI snapshot publish
```

Required indexes should be evidence-driven, but likely include:
- semantic kind -> handles;
- IED -> AP/LD/LN/service/config objects;
- LD/LN scope;
- canonical type id -> definition;
- canonical signal identity -> publisher/member/subscriber;
- incoming/outgoing typed references.

Do not create an independent mutable domain model.

### UI-thread budget

UI thread performs:
- selection bookkeeping;
- cheap dictionary/index lookup;
- assignment of immutable/batched rows;
- small formatting.

UI thread does not perform:
- whole-document scans;
- type graph traversal;
- large sorting;
- schema/semantic validation;
- large search;
- diff/merge;
- compatibility analysis.

### Memory

Document/session owns all:
- pools;
- indexes;
- snapshots;
- caches;
- cancellation roots;
- worker registrations.

Close/dispose must make them collectible.

No `string.Intern`. No process-global document cache. Add a document-scoped canonical pool only when allocation profiling shows benefit; do not add complexity merely because a contract mentions canonicalization.

## 5. Concurrency model

Work categories:

### Latest-state
Examples: search, filters, selected workspace details, fast validation, compatibility preview.

Contract:
- newest request wins for the same work key;
- older pending/running work is cancelled;
- source revision and request identity are checked before publish;
- stale result is discarded silently or logged diagnostically.

### Must-run
Examples: save, export, transaction commit, explicit merge commit.

Contract:
- never coalesced away;
- conflicting operations serialized;
- one atomic publication boundary;
- failure retains previous committed document/output.

### Parallel-safe
Independent read-only jobs may run concurrently only through bounded scheduler capacity. Never create one task per IEC node.

Future coordinator evolution may use a scoped key such as `(WorkKind, scope)` when independent work must coexist. Do not weaken latest-wins semantics for selection-driven detail work.

## 6. Standards program

The standards baseline is maintained in:
`docs/standards/IEC61850_STANDARDS_BASELINE.md`

Important 2026 correction to the earlier roadmap: SCL must account for **IEC 61850-6 Edition 2.2 (2024)**, not stop at 2.1.

Rule assets remain external/provider-based unless redistribution rights are clear.

## 7. Program lanes and dependencies

### Gate 0 — accept and merge PR #5

PR #5 is frozen except acceptance blockers.

Test former crash paths + station-first UX at normal/high DPI. Merge only after acceptance.

### P0 — issue #8: reliability/performance

Can start immediately after #5 acceptance. It may run in parallel with P1.

Primary output:
- no heavy UI-thread catalog/detail computation;
- typed/revision-scoped hot-query indexes/snapshots;
- measured navigation/settings performance;
- cancellation/stale publish/leak stress gates.

### P1 — issue #9: standards authority

Can run in parallel with P0.

Primary output:
- typed edition/namespace profile;
- SCL Ed1/2.0/2.1/2.2 handling;
- external schema/rule-provider architecture;
- rule provenance and edition scope;
- no scattered version conditionals.

### P2 — issue #10: semantic/index completeness

Starts after P0/P1 interfaces are stable.

Primary output:
- remaining read-model semantics;
- efficient typed indexes;
- complete type/reference chains required by later edits;
- no fake workspace without fixture/semantic evidence.

### P3 — issue #11: safe SCL surgery

Depends on P2 for the domains being edited.

Order:
DataSet -> Report -> GOOSE/SMV/Communication -> rename/delete/quick fixes.

Every mutation requires impact plan + transaction + validation + undo/redo + preservation tests.

### P4 — issue #12: diff/export/merge

Semantic diff and export foundations can begin after P2. Mutation-aware merge depends on P3.

No string/XML-text merge.

### P5 — issue #13: interoperability evidence

Harness/fixture governance can begin early in parallel. Strong claims depend on P1/P2/P4 outputs.

Evidence ladder:
parse -> preserve -> validate -> cross-tool -> runtime -> formal conformance.

### P6 — issue #14: final UX/release

UX may refine proven workflows during development, but final product claims/release wait on P0-P5 release gates.

## 8. Team/thread orchestration

### Claim rule

One thread claims one issue and writes a short issue comment before coding:
- scope;
- expected files/write set;
- dependencies;
- acceptance checks.

Do not have two threads independently solve the same authority.

### Branch rule

After PR #5:
- branch from current main;
- use focused branch names such as `perf/8-workspace-snapshots`, `standards/9-edition-providers`;
- one focused PR per issue/slice;
- no new 100+ commit mega-PR.

### Integration captain rule

The master issue #7 is the integration authority.

When two lanes need a shared interface:
1. agree the interface in issue #7 or the dependent issue;
2. land the smallest foundation PR first;
3. downstream branch rebases/updates;
4. do not implement parallel incompatible interfaces.

### Handoff rule

Before a thread ends:
- commit/push coherent work;
- state exact test evidence;
- update issue;
- update HANDOFF if architecture/current phase changed;
- identify next issue/slice;
- do not leave hidden TODO only in chat.

## 9. CI strategy

The workflow is intentionally tiered.

### PR fast gate

For code changes on a pull request:
- Ubuntu restore/build/test;
- no Windows self-contained artifact;
- obsolete PR runs cancelled when superseded.

Purpose: cheap fast feedback.

### Full qualification

On main push or explicit `workflow_dispatch`:
- Windows + Ubuntu + macOS;
- full tests;
- Windows self-contained publish.

Use manual qualification on a feature branch only when a Windows artifact or cross-platform evidence is actually needed.

### Documentation

Docs/Markdown-only changes do not trigger build/test CI.

### Benchmarks/stress

Do not put long benchmark matrices on every PR. Keep cheap deterministic regression tests in normal CI; run profiling/stress qualification manually/nightly/release as appropriate.

## 10. Interoperability strategy

Build a fixture manifest, not a random file folder.

Each fixture records:
- anonymized identifier;
- vendor/tool/product/version when allowed;
- SCL edition/revision;
- file role;
- expected engineering objects/counts;
- vendor extensions;
- expected diagnostics;
- allowed round-trip differences;
- cross-tool acceptance result and date.

Never commit customer-confidential station files without explicit approval.

Preferred evidence families include Siemens, Schneider Electric, Hitachi Energy/ABB lineage, GE/Multilin, and legacy Alstom when available, plus synthetic ambiguity/edge fixtures.

## 11. Release gates

A release candidate must pass:

### Correctness
- secure parse;
- round-trip preservation;
- schema/rule provider status explicit;
- no unresolved/ambiguous references hidden;
- selected release-scope semantics covered.

### Reliability
- former crash paths stress-tested;
- cancellation/stale publication tested;
- repeated open/close collectability;
- no unobserved task failures.

### Performance
- measured large-station navigation;
- measured heavy setting/data-model paths;
- allocations recorded for hot projectors;
- no known O(N x workspace-count) interaction left in routine selection paths where indexing can remove it.

### Engineering edits
- impact preview;
- atomic transaction;
- undo/redo;
- validation;
- safe save/export.

### Interoperability
- evidence level shown explicitly per fixture/tool;
- vendor/profile claims version-scoped;
- no "100% interoperable" or "certified" language without matching evidence.

### UX
- 100/125/150% Windows acceptance;
- keyboard and resize behavior;
- no XML knowledge required for core engineer workflows;
- XML/source evidence remains accessible for experts.

## 12. Freeze/recovery protocol

If a coding thread gets stuck:
1. stop editing;
2. read current issue + AGENTS + HANDOFF;
3. inspect current main/branch diff and tests;
4. identify the last proven green commit;
5. write the unresolved hypothesis in the issue;
6. resume from the last proven authority, not from memory;
7. never restart a completed subsystem merely because the thread context was lost.

This is the anti "maju-mundur" rule.

## 13. Immediate next action

Do not start P1/P2 code inside PR #5.

First, manually accept M3UX5 on the golden SCD. Then merge #5. After merge, P0 (#8) and P1 (#9) are the recommended first parallel coding lanes, while P5 (#13) may start fixture/harness governance without touching shared runtime authority.
