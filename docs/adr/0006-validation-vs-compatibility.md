# ADR-0006: IEC validation and target compatibility are separate

Status: Accepted
Date: 2026-10-03

## Decision

Validation domains are independently reported:
- XML
- IEC schema
- IEC semantic/model
- reference integrity
- engineering sanity
- target compatibility
- future live-device reality

A vendor/HMI compatibility warning must never be presented as an IEC violation unless it actually is one.

Every target-profile rule includes provenance/version scope.
