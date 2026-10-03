namespace ArSclStudio.Profiles.Compatibility;

public enum RuleEvidenceConfidence
{
    Unknown = 0,
    FieldObserved,
    VendorDocumented,
    StandardDocumented
}

public sealed record RuleProvenance(
    string SourceReference,
    string? ProductVersion,
    string? Section,
    RuleEvidenceConfidence Confidence,
    DateOnly VerifiedOn);

public sealed record CompatibilityProfileDescriptor(
    string Id,
    string ProductName,
    string ProfileVersion,
    string? ProductVersionRange);
