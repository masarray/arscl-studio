# Reliability and failure-containment strategy

## Principle

An engineering tool must prefer a recoverable explicit failure over silently producing a damaged SCL file.

## Session isolation

Each open document is a SclDocumentSession with its own:
- lifetime cancellation token
- indexes
- diagnostics
- revision
- caches
- pending worker registrations

One corrupted/closed session must not poison another.

## Exceptions

Background worker exceptions are observed centrally and converted into structured internal diagnostics/log events where appropriate.

Fire-and-forget tasks are prohibited unless routed through an exception-observing task owner.

Expected document errors are represented as diagnostics/results, not thrown through UI event loops.

## Cancellation

Closing a tab cancels all session work first, then awaits/drains owned workers, then releases indexes/caches.

Application shutdown performs the same process for all sessions.

## Revision safety

Every mutable session has a monotonically increasing Revision.

A worker records SourceRevision at start. On completion:

    if SourceRevision != CurrentRevision
        discard result

This prevents stale validation/search/index results from replacing newer state.

## Transactions

A mutation has explicit phases and rollback.

Partial semantic/index updates must never escape to observers.

The UI sees either:
- previous committed revision
- next committed revision

not an in-between state.

## File safety

Save/export:
- never writes in place first
- flush strategy where practical
- reparses output
- validates required gates
- atomic replace/move where platform supports it
- retains original on failure

## Malformed files

Recovery behavior:
- well-formed XML but invalid SCL: open semantic areas that can be resolved and show diagnostics
- malformed XML: source/recovery view with exact parser location; do not pretend semantic model is valid
- unsupported edition/namespace: explicit diagnostic and best-effort read-only inspection only if safe

## XML security

Defaults:
- DTD prohibited
- external entities disabled
- resolver null
- no automatic network schema fetch
- trusted/local schema providers only
- configurable size/depth limits with sane defaults and explicit override for trusted large station files

## Leak detection

Repeated open/close tests should verify that sessions become collectible.

Review checklist:
- subscriptions detached
- CTS disposed
- streams disposed
- timers stopped
- workers stopped
- caches cleared
- static references checked

## Logging

Logging must help diagnose without leaking entire customer engineering files by default.

Prefer:
- document fingerprint
- object semantic key
- operation id
- diagnostic code
- timings/counts

Raw XML/value payload logging is opt-in debug behavior.
