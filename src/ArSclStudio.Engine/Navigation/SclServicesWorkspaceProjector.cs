using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Navigation;

public sealed record SclServiceCapabilityProjection(
    SclNodeHandle Handle,
    string Name,
    string Parameters,
    int NestedElementCount);

public static class SclServicesWorkspaceProjector
{
    public static SclServiceCapabilityProjection[] Build(
        SclDocumentState state,
        SclNodeHandle iedHandle)
    {
        ArgumentNullException.ThrowIfNull(state);

        var rows = new List<SclServiceCapabilityProjection>();

        foreach (var node in state.SemanticIndex.Nodes)
        {
            if (node.Kind != SclSemanticKind.Services ||
                !BelongsToIed(
                    state,
                    node.Handle,
                    iedHandle))
            {
                continue;
            }

            var children = state.SemanticIndex.GetChildren(
                node.Handle);

            for (var i = 0; i < children.Count; i++)
            {
                if (children[i].Kind !=
                    SclSemanticKind.ServiceCapability)
                {
                    continue;
                }

                rows.Add(new SclServiceCapabilityProjection(
                    children[i].Handle,
                    children[i].DisplayName,
                    ReadAttributeSummary(
                        state.Syntax,
                        children[i].Handle),
                    CountDirectElementChildren(
                        state.Syntax,
                        children[i].Handle)));
            }
        }

        return
        [
            .. rows.OrderBy(
                static row => row.Name,
                StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static string ReadAttributeSummary(
        SclSyntaxDocument syntax,
        SclNodeHandle element)
    {
        var children = syntax.GetSelectableChildren(
            element);

        var attributes = new List<(string Name, string Value)>();

        for (var i = 0; i < children.Count; i++)
        {
            if (!syntax.TryGetNodeInfo(
                    children[i],
                    out var info) ||
                info is null ||
                info.Kind != SclSyntaxNodeKind.Attribute ||
                info.NamespaceUri.Length != 0)
            {
                continue;
            }

            attributes.Add((
                info.LocalName,
                info.Value ?? string.Empty));
        }

        if (attributes.Count == 0)
        {
            return string.Empty;
        }

        attributes.Sort(static (left, right) =>
            StringComparer.OrdinalIgnoreCase.Compare(
                left.Name,
                right.Name));

        return string.Join(
            ", ",
            attributes.Select(static item =>
                string.Concat(
                    item.Name,
                    "=",
                    item.Value)));
    }

    private static int CountDirectElementChildren(
        SclSyntaxDocument syntax,
        SclNodeHandle element)
    {
        var children = syntax.GetSelectableChildren(
            element);

        var count = 0;

        for (var i = 0; i < children.Count; i++)
        {
            if (syntax.TryGetNodeInfo(
                    children[i],
                    out var info) &&
                info is not null &&
                info.Kind == SclSyntaxNodeKind.Element &&
                info.Parent == element)
            {
                count++;
            }
        }

        return count;
    }

    private static bool BelongsToIed(
        SclDocumentState state,
        SclNodeHandle handle,
        SclNodeHandle iedHandle) =>
        state.SemanticIndex.TryFindAncestor(
            handle,
            SclSemanticKind.Ied,
            out var ied) &&
        ied is not null &&
        ied.Handle == iedHandle;
}
