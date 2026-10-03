using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Engine.Navigation;

public sealed record ExplorerRowProjection(
    SclNodeHandle Handle,
    ExplorerNodeKind Kind,
    int Depth,
    string Label,
    string? Badge,
    bool IsSelectable,
    bool HasChildren,
    bool IsExpanded);
