# IEDScout and XML Notepad research distilled for ARSCL

## IEDScout lessons

The Browser workflow demonstrates a strong IEC-first mental model:
- IED navigation is primary
- GOOSE, Reports, DataSets, and Data Model are top-level engineering concepts
- selected items are shown in a contextual Details pane
- descriptions make standard abbreviations understandable
- a working monitor can collect mixed IEC objects
- SCL open/validation issues are surfaced in status/problem history
- large IED models must remain usable while background analysis continues

ARSCL adopts the mental model, not the visual branding.

## XML Notepad lessons

The project demonstrates:
- tree and node-value views synchronized over one XML document
- schema-aware IntelliSense
- validation after edit with navigable errors
- command-pattern undo/redo
- CompoundCommand for atomic multi-node edits
- full-text/regex/XPath search
- dynamic contextual help
- drag/drop with explicit placement feedback

ARSCL adopts these interaction and architecture patterns but operates on IEC semantic commands rather than raw XML operations.

## Combined ARSCL principle

IEDScout answers "how should an IEC engineer browse this?"

XML Notepad answers "how should structured editing stay safe and efficient?"

ARSCL must answer both, then add:
- typed references
- impact analysis
- SCL surgery
- semantic diff/merge
- target compatibility
- safe export
