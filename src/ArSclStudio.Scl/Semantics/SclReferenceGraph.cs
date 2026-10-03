using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Scl.Semantics;

public sealed class SclReferenceGraph
{
    private static readonly SclReferenceEdge[] EmptyEdges = [];

    private readonly Dictionary<SclNodeHandle, SclReferenceEdge[]> _incoming;
    private readonly Dictionary<SclNodeHandle, SclReferenceEdge[]> _outgoing;

    internal SclReferenceGraph(IReadOnlyList<SclReferenceEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);

        _incoming = BuildIndex(edges, incoming: true);
        _outgoing = BuildIndex(edges, incoming: false);
        EdgeCount = edges.Count;
    }

    public int EdgeCount { get; }

    public IReadOnlyList<SclReferenceEdge> GetIncoming(SclNodeHandle target) =>
        _incoming.GetValueOrDefault(target) ?? EmptyEdges;

    public IReadOnlyList<SclReferenceEdge> GetOutgoing(SclNodeHandle source) =>
        _outgoing.GetValueOrDefault(source) ?? EmptyEdges;

    private static Dictionary<SclNodeHandle, SclReferenceEdge[]> BuildIndex(
        IReadOnlyList<SclReferenceEdge> edges,
        bool incoming)
    {
        var staging = new Dictionary<SclNodeHandle, List<SclReferenceEdge>>();

        for (var i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            var key = incoming ? edge.Target : edge.Source;

            if (!staging.TryGetValue(key, out var list))
            {
                list = [];
                staging.Add(key, list);
            }

            list.Add(edge);
        }

        var result = new Dictionary<SclNodeHandle, SclReferenceEdge[]>(staging.Count);

        foreach (var pair in staging)
        {
            result.Add(pair.Key, [.. pair.Value]);
        }

        return result;
    }
}
