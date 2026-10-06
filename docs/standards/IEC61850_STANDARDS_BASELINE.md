# IEC 61850 standards baseline for ARSCL

Status: public-source orientation and implementation governance  
Updated: 2026-10-06

## Purpose

This file identifies the IEC 61850 publications and public references that shape ARSCL's engineering architecture.

It is **not** a replacement for licensed normative standards. The repository must not copy restricted standard text. Implementation rules must cite identifiers/version scope/provenance and rely on legally obtained schema/rule assets where required.

## Core SCL authority

### IEC 61850-6

Current public IEC webstore baseline:

- IEC 61850-6:2009 base Edition 2.0
- Amendment 1: 2018
- Amendment 2: 2024
- consolidated publication: **IEC 61850-6:2009+AMD1:2018+AMD2:2024, Edition 2.2**

Public IEC page:
https://webstore.iec.ch/en/publication/103863

Public preview identifies the SCL namespace progression including Edition 2.1 `IEC 61850-6:2007B4` and Edition 2.2 `IEC 61850-6:2007C5`:
https://webstore.iec.ch/en/iec_catalog/product/preview/?id=L3B1Yi9wZGYvcHJldmlldy9pbmZvX2llYzYxODUwLTZ7ZWQyLjJ9ZW4ucGRm

Architecture consequence:
- ARSCL must not stop its typed edition model at "2.1";
- root Version/Revision/Release and namespace need deterministic mapping to an edition/profile object;
- unknown combinations stay explicit;
- rule/schema providers declare the exact supported profile.

## Information model / services

Public IEC references used as orientation:

- IEC 61850-7-1 consolidated Edition 2.1 — principles and models  
  https://webstore.iec.ch/en/publication/67536
- IEC 61850-7-2 consolidated Edition 2.1 — ACSI  
  https://webstore.iec.ch/en/publication/66525
- IEC 61850-7-3 consolidated Edition 2.1 — common data classes  
  https://webstore.iec.ch/en/publication/66526
- IEC 61850-7-4 consolidated Edition 2.1 — logical node/data object classes  
  https://webstore.iec.ch/en/publication/66551

Architecture consequence:
- SCL structural validity is not enough;
- LN/DO/DA/CDC/service semantics must be edition-aware;
- ARSCL semantic diagnostics must not pretend to be a full normative data-model rule set until the correct rule assets are configured.

## Machine-processable validation rules

IEC TS 61850-6-3:2025 defines a format/method for machine-processable validation rules (OCL) for IEC 61850 XML-based files.

Public IEC page:
https://webstore.iec.ch/en/publication/79695

Architecture consequence:
- keep a provider boundary for semantic/rule packs;
- allow externally supplied standard/private rules;
- record rule pack version/provenance;
- never hard-code a growing copy of edition rules across unrelated projectors;
- support deterministic diagnostics tied to source nodes.

OCL execution itself is a later implementation decision; do not add an interpreter without a concrete legally sourced rule pack and performance/security design.

## Conformance / interoperability

IEC 61850-10:2012+AMD1:2025 consolidated Edition 2.1 covers conformance testing and includes engineering-tool related conformance context.

Public IEC page:
https://webstore.iec.ch/en/publication/108858

UCA International interoperability events emphasize real multi-vendor behavior rather than same-tool round trips:
https://ucaiug.org/upcoming-ucaiug-events/61850-iop-2026/

Architecture consequence:
- parsing is not interoperability;
- cross-tool and runtime evidence must be tracked separately;
- "conformant/certified" wording requires actual applicable formal evidence.

## Functional/application engineering references

IEC TR 61850-7-6:2024 discusses Basic Application Profiles and expressing related engineering conventions in SCL.

Public IEC page:
https://webstore.iec.ch/en/publication/84757

IEC TR 61850-90-30:2025 discusses function modelling in SCL and newer collaborative/specification concepts.

Public IEC page:
https://webstore.iec.ch/en/publication/94414

These are research/future-scope references. Do not automatically treat every described file concept or workflow as a core supported ARSCL role. Introduce support only with an explicit roadmap issue, fixtures, edition policy and tests.

## Public IEC overview

IEC's public IEC 61850 portal describes the family in four broad pillars: semantics/data model, SCL engineering language, communication protocols and IEC 62351 security.

https://iec61850.dvl.iec.ch/
https://iec61850.dvl.iec.ch/what-is-61850/technical-principles/

ARSCL architecture should preserve the same separation:
- semantic model;
- engineering language/SCL;
- communication/runtime mappings;
- security-related requirements where in scope.

## Vendor engineering observations — non-normative

Vendor tools are useful for workflow and interoperability evidence, not as normative IEC authority.

### Siemens IEC 61850 System Configurator

Public page:
https://www.siemens.com/en-gb/products/siprotec/iec-61850/

Useful observations:
- multi-vendor system engineering;
- import/export of common SCL file roles;
- station/network/GOOSE/report/process-bus task orientation;
- engineer-facing workflow that does not require XML-first navigation.

### Schneider Electric CET850

Public page:
https://www.se.com/sa/en/download/document/CET850%20IEC850%20configuration%20tool/

Useful observations:
- ICD/IID/CID/SCD engineering;
- DataSet/Report/GOOSE configuration;
- cross-vendor ICD use with product-specific restrictions.

### Hitachi Energy IET600

Public page:
https://www.hitachienergy.com/products-and-solutions/substation-automation-protection-and-control/products/tools/iet600

Useful observations:
- system-wide IEC 61850 dataflow engineering;
- multi-vendor system configuration;
- consistency checking;
- conformance-oriented product positioning.

## ARSCL implementation rule registry

Every future standard-derived rule should carry, at minimum:

```
rule id
diagnostic domain
standard/publication identifier
edition/revision scope
clause/reference identifier when legally appropriate
rule-pack/provider version
provenance
confidence
fixture/test id
```

Do not store copied normative paragraphs.

## Supported-edition policy to implement

Target architecture:

```
SclEditionProfile
  Family            // Ed1 / Ed2
  ConsolidatedLevel // 2.0 / 2.1 / 2.2 etc
  Namespace
  Version
  Revision
  Release
  SchemaProviderId
  RulePackId
  SupportState      // Supported / ReadOnlyBestEffort / Unknown
```

This is conceptual, not a mandate for exact type/property names.

Policy:
- case-sensitive IEC identifiers;
- explicit namespace/revision recognition;
- no silent fallback from newer to older rules;
- read-only best effort is allowed only when safe and clearly indicated;
- export/mutation gates are stricter than inspection gates.

## Legal/source rule

The repository may contain:
- original ARSCL code;
- standard identifiers and public metadata;
- links/citations;
- independently authored explanations;
- legally redistributable schemas/rules with provenance.

It must not contain copied restricted normative IEC text or proprietary vendor manuals without redistribution rights.
