using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Scl.Semantics;

public sealed record SclSemanticNode(
    SclNodeHandle Handle,
    SclNodeHandle Parent,
    SclSemanticKind Kind,
    string DisplayName,
    string? Badge);
