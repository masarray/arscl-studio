using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Scl.Semantics;

public sealed record SclReferenceEdge(
    SclNodeHandle Source,
    SclNodeHandle Target,
    SclReferenceKind Kind,
    string? ReferenceText);
