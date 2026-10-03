# M3A — Semantic & Engineering Diagnostics acceptance

Date: 2026-10-03

## Slice 1 — implemented

This slice deepens semantic confidence for the M3A0 engineering workspaces without changing their information architecture.

Implemented diagnostics:
- `SCL-ENG-GOOSE-0001`: explicit GOOSE control has no Communication/GSE endpoint;
- `SCL-ENG-GOOSE-0002`: GOOSE endpoint has no Address;
- `SCL-ENG-GOOSE-0003`: GOOSE endpoint has no destination MAC;
- `SCL-ENG-GOOSE-0004`: GOOSE endpoint has no network APPID;
- `SCL-SEM-MODEL-0001`: DOI instance is not declared by the resolved LNodeType;
- `SCL-ENG-SMV-0001`: SampledValueControl has no Communication/SMV endpoint;
- `SCL-ENG-SMV-0002`: SMV endpoint has no Address;
- `SCL-ENG-SMV-0003`: SMV endpoint has no destination MAC;
- `SCL-ENG-SMV-0004`: SMV endpoint has no network APPID.

All findings are source-linked, revision-stamped and flow through the existing Problems / shared-selection pipeline.

## SMV semantic expansion

`Communication/SMV` is now a typed semantic object.

The reference graph resolves:
`Communication/SMV → SampledValueControl`

using the same deterministic identity principle as GOOSE:
`IED + LD + control-block name`.

Unresolved or ambiguous SMV endpoint bindings therefore use the existing reference-diagnostic authority instead of a second ad-hoc resolver.

## Noise-control rules

- GSSE is not treated as missing GOOSE Ethernet endpoint.
- GOOSE diagnostics only require an endpoint for an explicit `type="GOOSE"` control.
- Data Model instance diagnostics do not duplicate an unresolved/ambiguous `lnType` root cause; reference diagnostics remain authoritative in that case.
- `smvID` and GSEControl `appID` are not substituted for Communication-layer network APPID.

## Automated evidence

Verified head:
`b9d32c4180b9458753fc144ac49bb3df92fcb846`

GitHub Actions run:
`37136912729`

Per OS:
- SCL tests: 11/11;
- Engine/Desktop tests: 57/57;
- total: **68/68**.

Windows, Ubuntu and macOS builds/tests are green.

Verified Ubuntu build:
- 0 warnings;
- 0 errors.

Windows self-contained publish:
`ARSCL-Studio-win-x64` — artifact `11278714393`

Artifact SHA-256:
`511ed6dfb85f131b438424d9198d5eeead35e27c0e5fc1978f8d79f78cbab8d5`

## Deferred

- nested SDI/DAI template-consistency diagnostics beyond the DOI declaration check;
- supported Services interpretation;
- dedicated SMV engineering workspace;
- Substation semantics pending a real Substation fixture;
- broad destructive SCL surgery.
