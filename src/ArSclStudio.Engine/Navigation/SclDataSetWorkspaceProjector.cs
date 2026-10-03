using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Navigation;

public sealed record SclDataSetWorkspaceProjection(
    SclNodeHandle Handle,
    string IedName,
    string LogicalDevice,
    string LogicalNode,
    string Name,
    int MemberCount,
    int UsedByCount);

public sealed record SclDataSetMemberProjection(
    SclNodeHandle Handle,
    string LogicalDevice,
    string LogicalNode,
    string DataObject,
    string DataAttribute,
    string FunctionalConstraint,
    string Reference);

public static class SclDataSetWorkspaceProjector
{
    public static SclDataSetWorkspaceProjection[] BuildCatalog(
        SclDocumentState state,
        SclNodeHandle iedHandle)
    {
        ArgumentNullException.ThrowIfNull(state);

        var rows = new List<SclDataSetWorkspaceProjection>();

        foreach (var node in state.SemanticIndex.Nodes)
        {
            if (node.Kind != SclSemanticKind.DataSet ||
                !BelongsToIed(state, node, iedHandle))
            {
                continue;
            }

            rows.Add(new SclDataSetWorkspaceProjection(
                node.Handle,
                AncestorName(state, node.Handle, SclSemanticKind.Ied),
                AncestorName(state, node.Handle, SclSemanticKind.LogicalDevice),
                AncestorName(
                    state,
                    node.Handle,
                    SclSemanticKind.LogicalNodeZero,
                    SclSemanticKind.LogicalNode),
                node.DisplayName,
                state.SemanticIndex.GetChildren(node.Handle)
                    .Count(static child => child.Kind == SclSemanticKind.Fcda),
                state.SemanticIndex.References.GetIncoming(node.Handle).Count));
        }

        return
        [
            .. rows.OrderBy(static row => row.LogicalDevice, StringComparer.OrdinalIgnoreCase)
                   .ThenBy(static row => row.LogicalNode, StringComparer.OrdinalIgnoreCase)
                   .ThenBy(static row => row.Name, StringComparer.OrdinalIgnoreCase)
        ];
    }

    public static SclDataSetMemberProjection[] BuildMembers(
        SclDocumentState state,
        SclNodeHandle dataSetHandle)
    {
        ArgumentNullException.ThrowIfNull(state);

        var children = state.SemanticIndex.GetChildren(dataSetHandle);
        var rows = new List<SclDataSetMemberProjection>(children.Count);

        for (var i = 0; i < children.Count; i++)
        {
            var node = children[i];

            if (node.Kind != SclSemanticKind.Fcda)
            {
                continue;
            }

            var ld = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "ldInst");
            var prefix = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "prefix");
            var lnClass = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "lnClass");
            var lnInst = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "lnInst");
            var dataObject = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "doName");
            var dataAttribute = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "daName");
            var fc = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "fc");
            var ln = string.Concat(prefix, lnClass, lnInst);
            var reference = string.Concat(
                ld,
                "/",
                ln,
                "/",
                dataObject,
                string.IsNullOrWhiteSpace(dataAttribute)
                    ? string.Empty
                    : string.Concat(".", dataAttribute));

            rows.Add(new SclDataSetMemberProjection(
                node.Handle,
                ld,
                ln,
                dataObject,
                dataAttribute,
                fc,
                reference));
        }

        return [.. rows];
    }

    private static bool BelongsToIed(
        SclDocumentState state,
        SclSemanticNode node,
        SclNodeHandle iedHandle) =>
        state.SemanticIndex.TryFindAncestor(
            node.Handle,
            SclSemanticKind.Ied,
            out var ied) &&
        ied is not null &&
        ied.Handle == iedHandle;

    private static string AncestorName(
        SclDocumentState state,
        SclNodeHandle handle,
        params SclSemanticKind[] kinds)
    {
        for (var i = 0; i < kinds.Length; i++)
        {
            if (state.SemanticIndex.TryFindAncestor(
                    handle,
                    kinds[i],
                    out var node) &&
                node is not null)
            {
                return node.DisplayName;
            }
        }

        return string.Empty;
    }
}
