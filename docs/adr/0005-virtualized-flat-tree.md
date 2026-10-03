# ADR-0005: Virtualized flattened explorer

Status: Accepted
Date: 2026-10-03

## Decision

The engineering/XML explorers use a flattened list of currently visible rows with depth/expansion metadata.

Do not create a recursive Avalonia control/container for every SCL node.

## Rationale

Large IEC models may contain 100k+ leaf attributes. Recursive item controls create unacceptable visual-tree and ViewModel overhead.

## Consequence

Expansion/collapse is managed by an explicit projection/index, but selection, scrolling, filtering, and virtualization become predictable.
