using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Engine.Navigation;

public sealed record SclNodeDetailsProjection(
    SclNodeHandle Handle,
    string Title,
    string Kind,
    string Path,
    string SourceLocation,
    string NamespaceUri,
    string? Value,
    string Description);
