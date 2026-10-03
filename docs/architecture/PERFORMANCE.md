# Performance architecture

Performance is a correctness requirement for ARSCL Studio because modern IEC 61850 files can contain very large data models.

## Budgets and philosophy

Do not optimize blindly, but design hot paths so they can be optimized without rewriting architecture.

Initial engineering targets, to be validated and refined with real fixtures:
- 100k+ Data Attributes remain browsable
- opening a large document exposes a useful shallow tree before deep indexing completes
- search typing never starts one expensive full scan per keystroke
- expanding one tree branch does not materialize unrelated branches
- diagnostic refresh does not rebuild every visible row
- closing a document releases session-owned indexes/workers/caches

These are product targets, not claims that M0 already meets them.

## Parsing

Use a secure forward reader for probing and metadata detection.

Avoid accidental multiple full parses. If a second pass is required, document why.

Do not repeatedly parse qualified IEC references in every view. Parse/canonicalize once and cache in document scope.

## Canonical values

Do not use process-global string.Intern.

Use:
- XmlNameTable for XML names
- known enum/value singletons for FC/CDC/etc. where appropriate
- document-scoped canonical pools for repeated values
- stable numeric node handles in UI projections

Dispose document pools with the session.

## Indexes

Build indexes by purpose:
- shallow explorer
- semantic identity
- typed references
- search
- reverse references

Do not build every expensive index synchronously on Open.

Indexes should support incremental invalidation where a transaction affects a bounded region.

## Coalescing and debounce

Latest-state work:
- search
- filter
- fast validation
- compatibility preview
- derived statistics

uses latest-wins coalescing.

Explicit commands:
- save
- export
- merge commit

are must-run and must not be dropped.

## Worker concurrency

CPU-heavy work is bounded. Starting one Task.Run per IED/node is prohibited.

Use a shared scheduler/semaphore with a small CPU-based concurrency cap. IO and CPU work are separated.

Every task:
- accepts CancellationToken
- observes cancellation
- captures no UI controls
- publishes immutable results
- checks source revision before publication

## UI virtualization

Do not bind recursive TreeView item containers to every SCL node.

The explorer uses a flattened visible-row model:
- depth
- expansion state
- node handle
- icon/kind
- label
- diagnostic state

Only visible rows are presented by a virtualizing list.

Large member tables follow the same pattern.

Avalonia compiled bindings are default.

Avoid:
- DataGrid if a lighter virtualized list is sufficient
- deeply nested control templates
- converters on every cell in hot tables
- per-row event subscriptions

## Batching

A background worker may discover thousands of nodes, but UI updates are published in coarse batches.

Never dispatch one UI message per Data Attribute.

## Memory

Each document has a memory owner boundary.

Bounded cache rule:
- every cache has a capacity or is naturally bounded by the document
- cache value ownership is explicit
- cache keys must not accidentally retain closed sessions

Avoid storing duplicate full strings for semantic paths where structured identifiers or shared segments suffice.

## Profiling gates

Before claiming an optimization:
- deterministic throughput benchmark
- dotnet-counters/dotnet-trace for runtime behavior
- allocation profiling for hot loops
- memory snapshot after opening/closing repeated documents
- Avalonia DevTools/frame profiling for UI

Performance regressions should become tests/benchmarks, not tribal knowledge.
