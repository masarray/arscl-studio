using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Source;

namespace ArSclStudio.Scl.Syntax;

public sealed record SclSyntaxNodeInfo(
    SclNodeHandle Handle,
    SclNodeHandle Parent,
    SclSyntaxNodeKind Kind,
    string Prefix,
    string LocalName,
    string NamespaceUri,
    string? Value,
    SclSourceSpan SourceSpan);
