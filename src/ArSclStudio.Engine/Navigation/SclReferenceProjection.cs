using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Navigation;

public sealed record SclReferenceProjection(
    SclNodeHandle Source,
    SclReferenceKind Kind,
    string SourceLabel,
    string SourceKind,
    string? ReferenceText);
