using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Scl.Semantics;

public enum SclReferenceResolutionStatus
{
    Unresolved = 0,
    Ambiguous
}

public sealed record SclReferenceIssue(
    SclNodeHandle Source,
    SclReferenceKind Kind,
    SclReferenceResolutionStatus Status,
    string ReferenceText,
    SclSemanticKind? ExpectedTargetKind = null);
