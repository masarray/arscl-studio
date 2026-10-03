using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Scl.Semantics;

public sealed class SclSemanticIndex
{
    private static readonly SclSemanticNode[] EmptyNodes = [];

    private readonly Dictionary<SclNodeHandle, SclSemanticNode> _nodes;
    private readonly Dictionary<SclNodeHandle, SclSemanticNode[]> _children;

    internal SclSemanticIndex(
        Dictionary<SclNodeHandle, SclSemanticNode> nodes,
        Dictionary<SclNodeHandle, SclSemanticNode[]> children,
        SclReferenceGraph references)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(references);

        _nodes = nodes;
        _children = children;
        References = references;
    }

    public int NodeCount => _nodes.Count;

    public IReadOnlyCollection<SclSemanticNode> Nodes => _nodes.Values;

    public SclReferenceGraph References { get; }

    public bool TryGetNode(
        SclNodeHandle handle,
        out SclSemanticNode? node) =>
        _nodes.TryGetValue(handle, out node);

    public IReadOnlyList<SclSemanticNode> GetChildren(SclNodeHandle parent) =>
        _children.GetValueOrDefault(parent) ?? EmptyNodes;

    public bool HasChildren(SclNodeHandle parent) =>
        _children.TryGetValue(parent, out var children) &&
        children.Length != 0;

    public bool TryFindAncestor(
        SclNodeHandle start,
        SclSemanticKind kind,
        out SclSemanticNode? ancestor)
    {
        var current = start;

        for (var depth = 0; depth < 256 && !current.IsNone; depth++)
        {
            if (!_nodes.TryGetValue(current, out var node))
            {
                break;
            }

            if (node.Kind == kind)
            {
                ancestor = node;
                return true;
            }

            current = node.Parent;
        }

        ancestor = null;
        return false;
    }
}
