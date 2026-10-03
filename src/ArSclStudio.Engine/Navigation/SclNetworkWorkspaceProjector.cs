using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Navigation;

public sealed record SclNetworkWorkspaceProjection(
    SclNodeHandle Handle,
    string SubNetwork,
    string NetworkType,
    string IedName,
    string AccessPoint,
    string IpAddress,
    string SubnetMask,
    string Gateway,
    string OsiApTitle,
    string OsiAeQualifier);

public static class SclNetworkWorkspaceProjector
{
    public static SclNetworkWorkspaceProjection[] Build(SclDocumentState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var rows = new List<SclNetworkWorkspaceProjection>();

        foreach (var node in state.SemanticIndex.Nodes)
        {
            if (node.Kind != SclSemanticKind.ConnectedAccessPoint)
            {
                continue;
            }

            var subNetworkName = string.Empty;
            var networkType = string.Empty;

            if (state.SemanticIndex.TryFindAncestor(
                    node.Handle,
                    SclSemanticKind.SubNetwork,
                    out var subnet) &&
                subnet is not null)
            {
                subNetworkName = subnet.DisplayName;
                networkType = SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    subnet.Handle,
                    "type");
            }

            var ied = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "iedName");

            var accessPoint = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "apName");

            var values = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            var children = state.SemanticIndex.GetChildren(node.Handle);

            for (var i = 0; i < children.Count; i++)
            {
                if (children[i].Kind != SclSemanticKind.Address)
                {
                    continue;
                }

                foreach (var pair in SclWorkspaceSyntaxReader.ReadPValues(
                             state.Syntax,
                             children[i].Handle))
                {
                    values[pair.Key] = pair.Value;
                }
            }

            rows.Add(new SclNetworkWorkspaceProjection(
                node.Handle,
                subNetworkName,
                networkType,
                ied,
                accessPoint,
                Get(values, "IP"),
                Get(values, "IP-SUBNET"),
                Get(values, "IP-GATEWAY"),
                Get(values, "OSI-AP-Title"),
                Get(values, "OSI-AE-Qualifier")));
        }

        return
        [
            .. rows.OrderBy(static row => row.SubNetwork, StringComparer.OrdinalIgnoreCase)
                   .ThenBy(static row => row.IedName, StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static string Get(
        IReadOnlyDictionary<string, string> values,
        string key) =>
        values.TryGetValue(key, out var value)
            ? value
            : string.Empty;
}
