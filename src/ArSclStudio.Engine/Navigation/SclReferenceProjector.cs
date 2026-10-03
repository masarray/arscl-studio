using ArSclStudio.Engine.Documents;

namespace ArSclStudio.Engine.Navigation;

public static class SclReferenceProjector
{
    public static SclReferenceProjection[] BuildWhereUsed(
        SclDocumentState state,
        Scl.Identity.SclNodeHandle target)
    {
        ArgumentNullException.ThrowIfNull(state);

        var incoming = state.SemanticIndex.References.GetIncoming(target);

        if (incoming.Count == 0)
        {
            return [];
        }

        var rows = new SclReferenceProjection[incoming.Count];

        for (var i = 0; i < incoming.Count; i++)
        {
            var edge = incoming[i];

            if (state.SemanticIndex.TryGetNode(
                    edge.Source,
                    out var semanticNode) &&
                semanticNode is not null)
            {
                rows[i] = new SclReferenceProjection(
                    edge.Source,
                    edge.Kind,
                    semanticNode.DisplayName,
                    semanticNode.Kind.ToString(),
                    edge.ReferenceText);
                continue;
            }

            if (state.Syntax.TryGetNodeInfo(
                    edge.Source,
                    out var syntaxNode) &&
                syntaxNode is not null)
            {
                rows[i] = new SclReferenceProjection(
                    edge.Source,
                    edge.Kind,
                    syntaxNode.LocalName,
                    syntaxNode.Kind.ToString(),
                    edge.ReferenceText);
                continue;
            }

            rows[i] = new SclReferenceProjection(
                edge.Source,
                edge.Kind,
                edge.Source.ToString(),
                "Unknown",
                edge.ReferenceText);
        }

        return rows;
    }
}
