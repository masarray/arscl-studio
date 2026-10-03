namespace ArSclStudio.Scl.Documents;

public sealed record SclProbeResult(
    bool IsScl,
    SclFileKind FileKindHint,
    string RootNamespace,
    SclSchemaRevision SchemaRevision,
    string? HeaderId);
