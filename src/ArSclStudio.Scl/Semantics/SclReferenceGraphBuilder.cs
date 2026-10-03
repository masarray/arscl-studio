using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Scl.Semantics;

internal static class SclReferenceGraphBuilder
{
    public static SclReferenceGraph Build(
        SclSyntaxDocument syntax,
        SclSemanticIndex index)
    {
        ArgumentNullException.ThrowIfNull(syntax);
        ArgumentNullException.ThrowIfNull(index);

        var edges = new List<SclReferenceEdge>();
        var typeDefinitions = new UniqueHandleIndex<TypeKey>();
        var ieds = new UniqueHandleIndex<string>(StringComparer.Ordinal);
        var accessPoints = new UniqueHandleIndex<AccessPointKey>();
        var logicalDevices = new UniqueHandleIndex<LogicalDeviceKey>();
        var logicalNodes = new UniqueHandleIndex<LogicalNodeKey>();
        var dataSets = new UniqueHandleIndex<DataSetKey>();

        foreach (var node in index.Nodes)
        {
            switch (node.Kind)
            {
                case SclSemanticKind.LogicalNodeType:
                case SclSemanticKind.DataObjectType:
                case SclSemanticKind.DataAttributeType:
                case SclSemanticKind.EnumerationType:
                    AddTypeDefinition(syntax, node, typeDefinitions);
                    break;

                case SclSemanticKind.Ied:
                    AddIed(syntax, node, ieds);
                    break;

                case SclSemanticKind.AccessPoint:
                    AddAccessPoint(syntax, index, node, accessPoints);
                    break;

                case SclSemanticKind.LogicalDevice:
                    AddLogicalDevice(syntax, index, node, logicalDevices);
                    break;

                case SclSemanticKind.LogicalNodeZero:
                case SclSemanticKind.LogicalNode:
                    AddLogicalNode(syntax, index, node, logicalNodes);
                    break;

                case SclSemanticKind.DataSet:
                    AddDataSet(syntax, node, dataSets);
                    break;
            }
        }

        foreach (var node in index.Nodes)
        {
            switch (node.Kind)
            {
                case SclSemanticKind.LogicalNodeZero:
                case SclSemanticKind.LogicalNode:
                    AddLogicalNodeTypeReference(
                        syntax,
                        node,
                        typeDefinitions,
                        edges);
                    break;

                case SclSemanticKind.DataObjectDefinition:
                case SclSemanticKind.SubDataObjectDefinition:
                    AddTypeReference(
                        syntax,
                        node,
                        SclSemanticKind.DataObjectType,
                        typeDefinitions,
                        edges);
                    break;

                case SclSemanticKind.DataAttributeDefinition:
                case SclSemanticKind.BasicDataAttributeDefinition:
                    AddDataAttributeTypeReference(
                        syntax,
                        node,
                        typeDefinitions,
                        edges);
                    break;

                case SclSemanticKind.ReportControl:
                case SclSemanticKind.LogControl:
                case SclSemanticKind.GseControl:
                case SclSemanticKind.SampledValueControl:
                    AddDataSetBinding(
                        syntax,
                        index,
                        node,
                        dataSets,
                        edges);
                    break;

                case SclSemanticKind.ConnectedAccessPoint:
                    AddCommunicationBindings(
                        syntax,
                        node,
                        ieds,
                        accessPoints,
                        edges);
                    break;

                case SclSemanticKind.Fcda:
                    AddFcdaBinding(
                        syntax,
                        index,
                        node,
                        logicalNodes,
                        edges);
                    break;

                case SclSemanticKind.ExternalReference:
                    AddExtRefBinding(
                        syntax,
                        node,
                        logicalNodes,
                        edges);
                    break;
            }
        }

        return new SclReferenceGraph(edges);
    }

    private static void AddTypeDefinition(
        SclSyntaxDocument syntax,
        SclSemanticNode node,
        UniqueHandleIndex<TypeKey> destination)
    {
        if (syntax.TryGetAttributeValue(node.Handle, "id", out var id) &&
            !string.IsNullOrWhiteSpace(id))
        {
            destination.TryAdd(new TypeKey(node.Kind, id), node.Handle);
        }
    }

    private static void AddIed(
        SclSyntaxDocument syntax,
        SclSemanticNode node,
        UniqueHandleIndex<string> destination)
    {
        if (syntax.TryGetAttributeValue(node.Handle, "name", out var name) &&
            !string.IsNullOrWhiteSpace(name))
        {
            destination.TryAdd(name, node.Handle);
        }
    }

    private static void AddAccessPoint(
        SclSyntaxDocument syntax,
        SclSemanticIndex index,
        SclSemanticNode node,
        UniqueHandleIndex<AccessPointKey> destination)
    {
        if (!TryGetAncestorAttribute(
                syntax,
                index,
                node.Parent,
                SclSemanticKind.Ied,
                "name",
                out var iedName) ||
            !syntax.TryGetAttributeValue(node.Handle, "name", out var apName) ||
            string.IsNullOrWhiteSpace(apName))
        {
            return;
        }

        destination.TryAdd(
            new AccessPointKey(iedName, apName),
            node.Handle);
    }

    private static void AddLogicalDevice(
        SclSyntaxDocument syntax,
        SclSemanticIndex index,
        SclSemanticNode node,
        UniqueHandleIndex<LogicalDeviceKey> destination)
    {
        if (!TryGetAncestorAttribute(
                syntax,
                index,
                node.Parent,
                SclSemanticKind.Ied,
                "name",
                out var iedName) ||
            !syntax.TryGetAttributeValue(node.Handle, "inst", out var inst) ||
            string.IsNullOrWhiteSpace(inst))
        {
            return;
        }

        destination.TryAdd(
            new LogicalDeviceKey(iedName, inst),
            node.Handle);
    }

    private static void AddLogicalNode(
        SclSyntaxDocument syntax,
        SclSemanticIndex index,
        SclSemanticNode node,
        UniqueHandleIndex<LogicalNodeKey> destination)
    {
        if (!TryGetAncestorAttribute(
                syntax,
                index,
                node.Parent,
                SclSemanticKind.Ied,
                "name",
                out var iedName) ||
            !TryGetAncestorAttribute(
                syntax,
                index,
                node.Parent,
                SclSemanticKind.LogicalDevice,
                "inst",
                out var ldInst) ||
            !syntax.TryGetAttributeValue(node.Handle, "lnClass", out var lnClass) ||
            string.IsNullOrWhiteSpace(lnClass))
        {
            return;
        }

        syntax.TryGetAttributeValue(node.Handle, "prefix", out var prefix);
        syntax.TryGetAttributeValue(node.Handle, "inst", out var lnInst);

        destination.TryAdd(
            new LogicalNodeKey(
                iedName,
                ldInst,
                prefix ?? string.Empty,
                lnClass,
                lnInst ?? string.Empty),
            node.Handle);
    }

    private static void AddDataSet(
        SclSyntaxDocument syntax,
        SclSemanticNode node,
        UniqueHandleIndex<DataSetKey> destination)
    {
        if (!node.Parent.IsNone &&
            syntax.TryGetAttributeValue(node.Handle, "name", out var name) &&
            !string.IsNullOrWhiteSpace(name))
        {
            destination.TryAdd(
                new DataSetKey(node.Parent, name),
                node.Handle);
        }
    }

    private static void AddLogicalNodeTypeReference(
        SclSyntaxDocument syntax,
        SclSemanticNode node,
        UniqueHandleIndex<TypeKey> typeDefinitions,
        List<SclReferenceEdge> edges)
    {
        if (!syntax.TryGetAttributeValue(node.Handle, "lnType", out var typeId) ||
            string.IsNullOrWhiteSpace(typeId))
        {
            return;
        }

        AddResolvedTypeEdge(
            node.Handle,
            SclSemanticKind.LogicalNodeType,
            typeId,
            typeDefinitions,
            edges);
    }

    private static void AddTypeReference(
        SclSyntaxDocument syntax,
        SclSemanticNode node,
        SclSemanticKind targetKind,
        UniqueHandleIndex<TypeKey> typeDefinitions,
        List<SclReferenceEdge> edges)
    {
        if (!syntax.TryGetAttributeValue(node.Handle, "type", out var typeId) ||
            string.IsNullOrWhiteSpace(typeId))
        {
            return;
        }

        AddResolvedTypeEdge(
            node.Handle,
            targetKind,
            typeId,
            typeDefinitions,
            edges);
    }

    private static void AddDataAttributeTypeReference(
        SclSyntaxDocument syntax,
        SclSemanticNode node,
        UniqueHandleIndex<TypeKey> typeDefinitions,
        List<SclReferenceEdge> edges)
    {
        if (!syntax.TryGetAttributeValue(node.Handle, "type", out var typeId) ||
            string.IsNullOrWhiteSpace(typeId) ||
            !syntax.TryGetAttributeValue(node.Handle, "bType", out var basicType) ||
            string.IsNullOrWhiteSpace(basicType))
        {
            return;
        }

        var targetKind = basicType switch
        {
            "Struct" => SclSemanticKind.DataAttributeType,
            "Enum" => SclSemanticKind.EnumerationType,
            _ => (SclSemanticKind?)null
        };

        if (targetKind is not null)
        {
            AddResolvedTypeEdge(
                node.Handle,
                targetKind.Value,
                typeId,
                typeDefinitions,
                edges);
        }
    }

    private static void AddResolvedTypeEdge(
        SclNodeHandle source,
        SclSemanticKind targetKind,
        string typeId,
        UniqueHandleIndex<TypeKey> typeDefinitions,
        List<SclReferenceEdge> edges)
    {
        if (typeDefinitions.TryGetValue(
                new TypeKey(targetKind, typeId),
                out var target))
        {
            edges.Add(new SclReferenceEdge(
                source,
                target,
                SclReferenceKind.TypeDefinition,
                typeId));
        }
    }

    private static void AddDataSetBinding(
        SclSyntaxDocument syntax,
        SclSemanticIndex index,
        SclSemanticNode node,
        UniqueHandleIndex<DataSetKey> dataSets,
        List<SclReferenceEdge> edges)
    {
        if (!syntax.TryGetAttributeValue(node.Handle, "datSet", out var dataSetName) ||
            string.IsNullOrWhiteSpace(dataSetName) ||
            !TryFindLogicalNodeAncestor(index, node.Parent, out var logicalNode))
        {
            return;
        }

        if (dataSets.TryGetValue(
                new DataSetKey(logicalNode.Handle, dataSetName),
                out var target))
        {
            edges.Add(new SclReferenceEdge(
                node.Handle,
                target,
                SclReferenceKind.DataSetBinding,
                dataSetName));
        }
    }

    private static void AddCommunicationBindings(
        SclSyntaxDocument syntax,
        SclSemanticNode node,
        UniqueHandleIndex<string> ieds,
        UniqueHandleIndex<AccessPointKey> accessPoints,
        List<SclReferenceEdge> edges)
    {
        if (!syntax.TryGetAttributeValue(node.Handle, "iedName", out var iedName) ||
            string.IsNullOrWhiteSpace(iedName))
        {
            return;
        }

        if (ieds.TryGetValue(iedName, out var iedTarget))
        {
            edges.Add(new SclReferenceEdge(
                node.Handle,
                iedTarget,
                SclReferenceKind.CommunicationBinding,
                iedName));
        }

        if (syntax.TryGetAttributeValue(node.Handle, "apName", out var apName) &&
            !string.IsNullOrWhiteSpace(apName) &&
            accessPoints.TryGetValue(
                new AccessPointKey(iedName, apName),
                out var apTarget))
        {
            edges.Add(new SclReferenceEdge(
                node.Handle,
                apTarget,
                SclReferenceKind.CommunicationBinding,
                string.Concat(iedName, "/", apName)));
        }
    }

    private static void AddFcdaBinding(
        SclSyntaxDocument syntax,
        SclSemanticIndex index,
        SclSemanticNode node,
        UniqueHandleIndex<LogicalNodeKey> logicalNodes,
        List<SclReferenceEdge> edges)
    {
        if (!TryGetAncestorAttribute(
                syntax,
                index,
                node.Parent,
                SclSemanticKind.Ied,
                "name",
                out var iedName) ||
            !TryBuildReferencedLogicalNodeKey(
                syntax,
                node.Handle,
                iedName,
                out var key,
                requireIedName: false) ||
            !logicalNodes.TryGetValue(key, out var target))
        {
            return;
        }

        syntax.TryGetAttributeValue(node.Handle, "doName", out var doName);
        syntax.TryGetAttributeValue(node.Handle, "daName", out var daName);

        edges.Add(new SclReferenceEdge(
            node.Handle,
            target,
            SclReferenceKind.DataSetMember,
            CreateDataReference(doName, daName)));
    }

    private static void AddExtRefBinding(
        SclSyntaxDocument syntax,
        SclSemanticNode node,
        UniqueHandleIndex<LogicalNodeKey> logicalNodes,
        List<SclReferenceEdge> edges)
    {
        if (!syntax.TryGetAttributeValue(node.Handle, "iedName", out var iedName) ||
            string.IsNullOrWhiteSpace(iedName) ||
            !TryBuildReferencedLogicalNodeKey(
                syntax,
                node.Handle,
                iedName,
                out var key,
                requireIedName: true) ||
            !logicalNodes.TryGetValue(key, out var target))
        {
            return;
        }

        syntax.TryGetAttributeValue(node.Handle, "doName", out var doName);
        syntax.TryGetAttributeValue(node.Handle, "daName", out var daName);

        edges.Add(new SclReferenceEdge(
            node.Handle,
            target,
            SclReferenceKind.ExternalSource,
            CreateDataReference(doName, daName)));
    }

    private static bool TryBuildReferencedLogicalNodeKey(
        SclSyntaxDocument syntax,
        SclNodeHandle handle,
        string iedName,
        out LogicalNodeKey key,
        bool requireIedName)
    {
        if (requireIedName &&
            string.IsNullOrWhiteSpace(iedName))
        {
            key = default;
            return false;
        }

        if (!syntax.TryGetAttributeValue(handle, "ldInst", out var ldInst) ||
            string.IsNullOrWhiteSpace(ldInst) ||
            !syntax.TryGetAttributeValue(handle, "lnClass", out var lnClass) ||
            string.IsNullOrWhiteSpace(lnClass))
        {
            key = default;
            return false;
        }

        syntax.TryGetAttributeValue(handle, "prefix", out var prefix);
        syntax.TryGetAttributeValue(handle, "lnInst", out var lnInst);

        key = new LogicalNodeKey(
            iedName,
            ldInst,
            prefix ?? string.Empty,
            lnClass,
            lnInst ?? string.Empty);

        return true;
    }

    private static bool TryGetAncestorAttribute(
        SclSyntaxDocument syntax,
        SclSemanticIndex index,
        SclNodeHandle start,
        SclSemanticKind ancestorKind,
        string attributeName,
        out string value)
    {
        if (index.TryFindAncestor(start, ancestorKind, out var ancestor) &&
            ancestor is not null &&
            syntax.TryGetAttributeValue(
                ancestor.Handle,
                attributeName,
                out var attributeValue) &&
            !string.IsNullOrWhiteSpace(attributeValue))
        {
            value = attributeValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryFindLogicalNodeAncestor(
        SclSemanticIndex index,
        SclNodeHandle start,
        out SclSemanticNode logicalNode)
    {
        var current = start;

        for (var depth = 0; depth < 256 && !current.IsNone; depth++)
        {
            if (!index.TryGetNode(current, out var node) ||
                node is null)
            {
                break;
            }

            if (node.Kind is
                SclSemanticKind.LogicalNodeZero or
                SclSemanticKind.LogicalNode)
            {
                logicalNode = node;
                return true;
            }

            current = node.Parent;
        }

        logicalNode = null!;
        return false;
    }

    private static string? CreateDataReference(
        string? doName,
        string? daName)
    {
        if (string.IsNullOrWhiteSpace(doName))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(daName)
            ? doName
            : string.Concat(doName, ".", daName);
    }


    private sealed class UniqueHandleIndex<TKey>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, SclNodeHandle> _unique;
        private readonly HashSet<TKey> _ambiguous;

        public UniqueHandleIndex(IEqualityComparer<TKey>? comparer = null)
        {
            _unique = new Dictionary<TKey, SclNodeHandle>(comparer);
            _ambiguous = new HashSet<TKey>(comparer);
        }

        public bool TryAdd(TKey key, SclNodeHandle handle)
        {
            if (_ambiguous.Contains(key))
            {
                return false;
            }

            if (_unique.TryAdd(key, handle))
            {
                return true;
            }

            _unique.Remove(key);
            _ambiguous.Add(key);
            return false;
        }

        public bool TryGetValue(
            TKey key,
            out SclNodeHandle handle) =>
            _unique.TryGetValue(key, out handle);
    }

    private readonly record struct TypeKey(
        SclSemanticKind Kind,
        string Id);

    private readonly record struct AccessPointKey(
        string IedName,
        string ApName);

    private readonly record struct LogicalDeviceKey(
        string IedName,
        string Inst);

    private readonly record struct LogicalNodeKey(
        string IedName,
        string LdInst,
        string Prefix,
        string LnClass,
        string LnInst);

    private readonly record struct DataSetKey(
        SclNodeHandle LogicalNode,
        string Name);
}
