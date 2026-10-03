using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Scl.Semantics;

public sealed record SclTopLevelIndex(
    SclNodeHandle Header,
    IReadOnlyList<SclNodeHandle> Substations,
    IReadOnlyList<SclNodeHandle> Communications,
    IReadOnlyList<SclIedSummary> Ieds,
    SclNodeHandle DataTypeTemplates,
    IReadOnlyList<SclNodeHandle> PrivateElements);
