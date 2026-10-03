using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Engine.Search;

public sealed record SclSearchResultProjection(
    SclNodeHandle Handle,
    string Name,
    string Kind,
    string Path);
