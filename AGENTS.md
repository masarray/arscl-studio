# ARSCL Studio engineering contract

This file is the mandatory implementation contract for every human or coding agent working in this repository. Read it before changing code.

## Mission

ARSCL Studio is a cross-platform IEC 61850 SCL engineering workbench. It must let an engineer safely understand, inspect, edit, repair, merge, validate, diff, and export ICD/IID/CID/SCD/SSD/SED files without destroying vendor-specific information. The long-term engine must understand IEC 61850 semantics rather than treating SCL as generic XML.

The product must remain reliable on large real-world stations and must not become a fragile demo.

## Non-negotiable rules

1. No naive coding.
2. No business or IEC 61850 logic in Avalonia views or view-models.
3. The Desktop project must never mutate SCL XML directly.
4. All mutations pass through Engine commands and transactions.
5. Every destructive operation requires reference/impact analysis before commit.
6. Every committed edit must be undoable unless explicitly documented otherwise.
7. Unknown XML, Private elements, vendor namespaces, comments, and extensions must be preserved.
8. Never silently guess or repair ambiguous IEC 61850 intent.
9. Standard conformance, model integrity, target compatibility, and live-device reality are separate concepts.
10. Heavy parsing, indexing, validation, search, diff, merge, and compatibility work must not block the UI thread.
11. Long-running work must support cancellation.
12. Rapid repeated requests must be debounced or coalesced. Do not queue obsolete work indefinitely.
13. Results from background workers are revision-stamped. Stale results must never overwrite newer document state.
14. Caches must be bounded or document-scoped. Do not create process-lifetime unbounded dictionaries.
15. Do not use string.Intern for document data. It creates process-lifetime retention. Prefer document-scoped canonical pools.
16. Event subscriptions, timers, cancellation sources, streams, and workers must have explicit lifetime/disposal ownership.
17. Avoid per-row/per-node subscriptions for large trees. Prefer shared state, immutable row projections, and batched notifications.
18. Avoid ObservableCollection churn for bulk data. Publish immutable/batched snapshots or range updates.
19. Avoid creating one UI control per SCL node. Large trees and tables require virtualization and lazy projection.
20. Avoid repeated LINQ chains in hot loops when they create avoidable allocations. Correctness first, then profile, then optimize.
21. Avoid reflection-based binding in hot UI paths. Avalonia compiled bindings are the default.
22. Do not introduce a paid/proprietary UI dependency into the open-source core without an explicit ADR.
23. Save and export must be transactional: write temporary output, re-open, validate, then atomically replace/move.
24. Never overwrite the user's original file after a failed save/export.
25. Round-trip fidelity is a release gate.
26. Every compatibility rule requires provenance: source, product/version scope, rule version, and confidence.
27. Do not copy normative IEC text or proprietary vendor manual text into the repository unless redistribution rights are verified.
28. No feature is complete without tests appropriate to its failure modes.
29. No optimization claim is accepted without a benchmark, allocation measurement, or profiler evidence once the relevant implementation exists.
30. Keep the repository buildable at each milestone. Do not leave long-lived broken branches as the project's working baseline.

## Architectural boundaries

Dependency direction:

    ArSclStudio.Desktop  -> ArSclStudio.Engine -> ArSclStudio.Scl
                         -> ArSclStudio.Profiles -> ArSclStudio.Engine/Scl

    ArSclStudio.Cli      -> ArSclStudio.Engine + ArSclStudio.Profiles

ArSclStudio.Scl:
- secure XML/SCL syntax handling
- file/edition classification
- source locations
- canonical identities
- semantic indexing primitives
- reference graph primitives
- serialization abstractions
- no UI references

ArSclStudio.Engine:
- document sessions and revisions
- commands, transactions, undo/redo
- validation orchestration
- search, diff, merge, export
- worker scheduling/coalescing
- bounded caches
- change journal
- no Avalonia references

ArSclStudio.Profiles:
- target compatibility profiles
- SICAM SCC and future gateway/HMI rules
- rule provenance
- must not redefine IEC standard validity

ArSclStudio.Desktop:
- Avalonia shell, views, view-models
- selection/navigation presentation
- virtualized projections
- no direct XML mutation
- no IEC business rules

ArSclStudio.Cli:
- headless entry point proving that Engine is UI-independent

## Authoritative data model

The serialized SCL syntax document is authoritative. Semantic objects are indexed projections over the same source nodes; they are not an independently editable second model.

Every semantic object must be traceable to:
- SclNodeHandle: runtime identity
- SclSemanticKey: deterministic IEC semantic identity where possible
- SourceSpan: source location where available

Engineering view and XML view must resolve to the same node handle.

## IEC 61850 completeness strategy

The engine is edition-aware and must evolve toward full IEC 61850 SCL context:
- SCL document structure and header/history
- Substation section
- Communication / SubNetwork / ConnectedAP / Address
- IED / AccessPoint / Server / LDevice
- Logical Nodes, Data Objects, Data Attributes
- DataTypeTemplates and all reference chains
- DataSet / FCDA
- ReportControl / LogControl
- GSEControl / SampledValueControl
- Inputs / ExtRef
- Setting groups and supported services
- Private/vendor extensions
- edition-specific namespaces and rules
- NSD/schema/rule-pack providers where legally distributable

Do not pretend unsupported semantics are supported. Missing coverage must be explicit in diagnostics and roadmap.

## Canonicalization rules

IEC identifiers are case-sensitive. Never lowercase names to make lookups easier.

Canonical keys must:
- preserve IEC identifier case
- be deterministic
- escape delimiters
- include enough parent scope to avoid collisions
- remain independent of UI object identity
- distinguish unknown/unresolved identity instead of guessing

Repeated strings may be deduplicated only in document-scoped pools. Document disposal must release the pool.

## Worker and coalescing contract

Expensive work is categorized by WorkKind, for example:
- Parse
- Index
- ValidateFast
- ValidateFull
- Search
- Compatibility
- Diff
- MergePreview

For latest-state work such as search/filter/fast validation:
- only the newest pending request for the same document + work kind should survive
- previous pending/running work receives cancellation
- completion checks document revision before publish
- UI updates are posted in coarse batches, not one row at a time

For must-run work such as explicit export:
- do not coalesce it away
- serialize conflicting operations
- use a transaction/exclusive session lock

## Memory and leak discipline

Every SclDocumentSession owns:
- cancellation root
- document-scoped string/canonical pools
- semantic/reference/search indexes
- diagnostic snapshots
- worker registrations
- caches

Closing a document must make all of the above collectible.

Forbidden patterns:
- static collections containing document nodes
- global strong references to ViewModels
- anonymous event handlers that cannot be detached for long-lived publishers
- unbounded memoization
- background loops without cancellation
- fire-and-forget tasks without centralized exception observation

Use weak references only when semantics really call for them; do not use them to hide ownership mistakes.

## Large model UI contract

The target is comfortable interaction with 100k+ data attributes and large multi-IED SCDs.

Therefore:
- explorer is a virtualized flattened tree projection, not recursive control creation
- children are materialized lazily
- selection state uses stable node handles
- details are loaded on demand
- table rows are virtualized
- bulk updates are batched
- row ViewModels should be lightweight and preferably immutable
- counts/statistics may be calculated asynchronously
- UI thread work should be bounded per frame

Avalonia's open-source controls are preferred. Do not make Avalonia Pro TreeDataGrid a hard dependency. A custom flattened virtualized explorer/list is the baseline.

## Reliability gates

A change touching parsing/serialization must pass:
- malformed-input tests
- secure XML tests
- round-trip tests
- vendor-extension preservation tests

A change touching transactions must pass:
- apply
- validate
- rollback on failure
- undo
- redo
- redo invalidation after new edit

A change touching workers must pass:
- cancellation
- stale-result rejection
- coalescing
- disposal
- exception observation

A change touching large lists/trees must include a performance regression test or benchmark fixture.

## Save/export contract

Normal save and target export both use:

    build output plan
      -> serialize to temporary path/stream
      -> re-open using the production parser
      -> validate required gates
      -> semantic sanity check
      -> atomic destination replacement/move

If any stage fails, the previous destination remains intact.

## Testing hierarchy

1. deterministic unit tests
2. round-trip fixture tests
3. semantic/reference tests
4. transaction tests
5. merge/diff tests
6. compatibility-profile tests
7. headless Avalonia tests where useful
8. performance/allocation benchmarks
9. cross-platform CI

Prefer small synthetic fixtures for unit tests and curated real/anonymized fixtures for regression tests.

## Definition of done

A milestone is not done because the UI looks complete. It is done when:
- behavior is implemented in the correct layer
- failure paths are handled
- cancellation/lifetime is correct
- tests are green
- documentation is updated
- no known regression is hidden
- CI is green on supported platforms
- the next engineer can continue from docs/continuation/HANDOFF.md without reconstructing decisions from chat history

## Handoff discipline

At the end of each meaningful phase update docs/continuation/HANDOFF.md with:
- completed scope
- exact branch/PR
- architecture decisions added/changed
- tests and CI state
- known limitations
- next milestone and acceptance criteria

Do not rely on conversation memory as project documentation.
