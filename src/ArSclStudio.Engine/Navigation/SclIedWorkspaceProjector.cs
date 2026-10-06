using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Navigation;

public sealed record SclIedWorkspaceProjection(
    SclNodeHandle Handle,
    string Name,
    string Description,
    string Manufacturer,
    int AccessPointCount,
    int LogicalDeviceCount,
    int LogicalNodeCount,
    int DataSetCount,
    int ReportCount,
    int GooseCount,
    int SettingGroupCount);

public static class SclIedWorkspaceProjector
{
    public static SclIedWorkspaceProjection[] Build(SclDocumentState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var ownership = new Dictionary<SclNodeHandle, SclNodeHandle>();
        var aggregates = new Dictionary<SclNodeHandle, MutableIedAggregate>();

        foreach (var node in state.SemanticIndex.Nodes)
        {
            if (node.Kind != SclSemanticKind.Ied)
            {
                continue;
            }

            ownership[node.Handle] = node.Handle;
            aggregates[node.Handle] = new MutableIedAggregate(node);
        }

        if (aggregates.Count == 0)
        {
            return [];
        }

        foreach (var node in state.SemanticIndex.Nodes)
        {
            if (node.Kind == SclSemanticKind.Ied)
            {
                continue;
            }

            if (!TryResolveOwningIed(
                    state,
                    node,
                    ownership,
                    out var iedHandle) ||
                !aggregates.TryGetValue(iedHandle, out var aggregate))
            {
                continue;
            }

            aggregate.Count(node.Kind);
        }

        var result = new SclIedWorkspaceProjection[aggregates.Count];
        var index = 0;

        foreach (var aggregate in aggregates.Values
                     .OrderBy(static item => item.Node.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            state.Syntax.TryGetAttributeValue(
                aggregate.Node.Handle,
                "desc",
                out var description);

            state.Syntax.TryGetAttributeValue(
                aggregate.Node.Handle,
                "manufacturer",
                out var manufacturer);

            result[index++] = new SclIedWorkspaceProjection(
                aggregate.Node.Handle,
                aggregate.Node.DisplayName,
                description ?? string.Empty,
                manufacturer ?? string.Empty,
                aggregate.AccessPoints,
                aggregate.LogicalDevices,
                aggregate.LogicalNodes,
                aggregate.DataSets,
                aggregate.Reports,
                aggregate.Goose,
                aggregate.SettingGroups);
        }

        return result;
    }

    private static bool TryResolveOwningIed(
        SclDocumentState state,
        SclSemanticNode node,
        Dictionary<SclNodeHandle, SclNodeHandle> ownership,
        out SclNodeHandle iedHandle)
    {
        if (ownership.TryGetValue(node.Handle, out iedHandle))
        {
            return !iedHandle.IsNone;
        }

        var chain = new List<SclNodeHandle>(8);
        var current = node.Parent;

        for (var depth = 0; depth < 256 && !current.IsNone; depth++)
        {
            if (ownership.TryGetValue(current, out iedHandle))
            {
                for (var i = 0; i < chain.Count; i++)
                {
                    ownership[chain[i]] = iedHandle;
                }

                ownership[node.Handle] = iedHandle;
                return !iedHandle.IsNone;
            }

            chain.Add(current);

            if (!state.SemanticIndex.TryGetNode(current, out var parent) ||
                parent is null)
            {
                break;
            }

            if (parent.Kind == SclSemanticKind.Ied)
            {
                iedHandle = parent.Handle;
                ownership[parent.Handle] = parent.Handle;

                for (var i = 0; i < chain.Count; i++)
                {
                    ownership[chain[i]] = iedHandle;
                }

                ownership[node.Handle] = iedHandle;
                return true;
            }

            current = parent.Parent;
        }

        iedHandle = SclNodeHandle.None;
        ownership[node.Handle] = iedHandle;
        return false;
    }

    private sealed class MutableIedAggregate
    {
        public MutableIedAggregate(SclSemanticNode node)
        {
            Node = node;
        }

        public SclSemanticNode Node { get; }

        public int AccessPoints { get; private set; }

        public int LogicalDevices { get; private set; }

        public int LogicalNodes { get; private set; }

        public int DataSets { get; private set; }

        public int Reports { get; private set; }

        public int Goose { get; private set; }

        public int SettingGroups { get; private set; }

        public void Count(SclSemanticKind kind)
        {
            switch (kind)
            {
                case SclSemanticKind.AccessPoint:
                    AccessPoints++;
                    break;

                case SclSemanticKind.LogicalDevice:
                    LogicalDevices++;
                    break;

                case SclSemanticKind.LogicalNodeZero:
                case SclSemanticKind.LogicalNode:
                    LogicalNodes++;
                    break;

                case SclSemanticKind.DataSet:
                    DataSets++;
                    break;

                case SclSemanticKind.ReportControl:
                case SclSemanticKind.LogControl:
                    Reports++;
                    break;

                case SclSemanticKind.GseControl:
                    Goose++;
                    break;

                case SclSemanticKind.SettingGroupControl:
                    SettingGroups++;
                    break;
            }
        }
    }
}
