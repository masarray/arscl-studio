namespace ArSclStudio.Scl.Documents;

public sealed record SclDocumentMetadata(
    SclFileKind FileKindHint,
    string RootNamespace,
    SclSchemaRevision SchemaRevision,
    string? HeaderId);
