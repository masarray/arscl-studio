# M3UX4 — IEDScout-style visual workflow finishing

Date: 2026-10-04

## Goal

Finish the M3 desktop from real Windows screenshot evidence. This slice is presentation/navigation work only: IEC semantics remain owned by Engine projectors and the shared SclNodeHandle selection model.

## Implemented

- Problems/Search/Changes dock is collapsed by default to a compact status strip and can be expanded on demand.
- Search automatically opens the Search Results dock.
- Problem summary now explicitly reports Error / Warning / Info counts, removing the visible total-count ambiguity.
- duplicate horizontal engineering workspace headers in the center are suppressed; the left Engineer navigator is the primary workspace selector.
- raw XML-shaped breadcrumb is replaced by an engineering breadcrumb such as `IED › LD › LN › GOOSE › control`; the raw source path remains available through Inspector/tooltip.
- Network uses the selected IED context and exposes endpoint details (AP, subnet, IP, mask, gateway, OSI AP title, AE qualifier).
- GOOSE control catalog no longer consumes most vertical space; published signals/subscribers remain the inspection focus.
- DataSet catalog columns are widened/rebalanced to avoid clipping.
- Setting Groups keeps Value/Unit/Min/Max/Step in the primary grid; type/description move to selected-object detail.
- Inspector long values use compact ellipsis + tooltip rather than turning the Inspector into a wrapped text document.
- small IEC/domain glyphs improve scan speed without increasing typography size.
- the left Engineer navigator is now object-aware for Network, GOOSE, DataSets, Data Model, and Setting Groups.
- those object-aware workspaces no longer repeat their master catalog in the center; center panes are inspection-first.
- Reports intentionally keeps its RCB/LCB catalog in the center because long report lists are more efficient as a table, matching the reference workflow.
- Project Explorer is slightly wider and Inspector slightly narrower to support object identity without reducing the engineering center excessively.

## Preserved boundaries

- no SCL XML parsing was added to Desktop;
- no Engine projector semantics changed;
- no mutation/edit policy changed;
- virtualization remains enabled on long lists;
- cross-navigation still uses SclNodeHandle;
- Data Model collapse state remains Desktop-only presentation state.

## Regression

The existing cross-platform suite remains green. EngineeringWorkspaceViewModelTests now also asserts that active domain objects appear in the left Engineer navigator and that selection remains synchronized with the authoritative workspace object.

## Verified executable evidence

Executable head:
`e7499c361119d8f6c1ff6d9a1806d6d62f9413b6`

CI:
`37184182363`

- Windows: green
- Ubuntu: green
- macOS: green
- Windows self-contained publish: green
- SCL tests: 11/11 per OS
- Engine/Desktop tests: 66/66 per OS
- total: **77/77 tests per OS**

Windows artifact:
`ARSCL-Studio-win-x64` — artifact `11295824860`

Artifact digest:
`sha256:e8ac452dbfa37ae75344017f6aa0fcedfafbac958079dbc98a38d99e6f600832`

## Manual visual gate

Test this artifact with the golden SCD at 100%, 125%, and 150% DPI.

Capture:
1. Overview with Problems collapsed;
2. Network with an endpoint selected in the left navigator;
3. GOOSE with a control selected and signals visible;
4. DataSets with one DataSet selected in the left navigator;
5. Reports with RCB list + bound DataSet members;
6. Data Model with LN selected in the left navigator and DO expanded/collapsed;
7. Settings with an SG control selected;
8. Problems expanded after selecting a diagnostic.

Acceptance emphasis:
- one obvious navigation source rather than duplicated domain selectors;
- IED/domain/object context remains visible;
- center area is primarily inspection, not repeated catalog navigation;
- no important columns clip at normal/high DPI;
- bottom diagnostics do not permanently consume engineering viewport height.
