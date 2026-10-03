namespace ArSclStudio.Scl.Documents;

public readonly record struct SclSchemaRevision(
    string? Version,
    string? Revision,
    string? Release)
{
    public bool IsSpecified =>
        !string.IsNullOrWhiteSpace(Version) ||
        !string.IsNullOrWhiteSpace(Revision) ||
        !string.IsNullOrWhiteSpace(Release);

    public override string ToString()
    {
        if (!IsSpecified)
        {
            return "unspecified";
        }

        return string.Join(
            "/",
            new[] { Version, Revision, Release }.Where(static value => !string.IsNullOrWhiteSpace(value)));
    }
}
