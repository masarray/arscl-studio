using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Navigation;

public sealed record SclDataModelLogicalNodeProjection(
    SclNodeHandle Handle,
    string LogicalDevice,
    string LogicalNode,
    string LnClass,
    string Description,
    string LnType,
    int DataObjectCount);

public sealed record SclDataModelRowProjection(
    SclNodeHandle Handle,
    int Depth,
    string Kind,
    string Name,
    string Path,
    string Cdc,
    string FunctionalConstraint,
    string BasicType,
    string Value,
    string TypeId,
    string Description)
{
    public string DisplayName =>
        string.Concat(
            new string(' ', Depth * 2),
            Name);
}

public static class SclDataModelWorkspaceProjector
{
    public static SclDataModelLogicalNodeProjection[] BuildLogicalNodes(
        SclDocumentState state,
        SclNodeHandle iedHandle)
    {
        ArgumentNullException.ThrowIfNull(state);

        var rows = new List<SclDataModelLogicalNodeProjection>();

        foreach (var node in state.SemanticIndex.Nodes)
        {
            if (node.Kind is not (
                    SclSemanticKind.LogicalNodeZero or
                    SclSemanticKind.LogicalNode) ||
                !BelongsToIed(state, node.Handle, iedHandle))
            {
                continue;
            }

            var typeTarget = FindOutgoingTarget(
                state,
                node.Handle,
                SclReferenceKind.TypeDefinition,
                SclSemanticKind.LogicalNodeType);

            var dataObjectCount = typeTarget.IsNone
                ? 0
                : state.SemanticIndex.GetChildren(typeTarget)
                    .Count(static child =>
                        child.Kind ==
                            SclSemanticKind.DataObjectDefinition);

            rows.Add(new SclDataModelLogicalNodeProjection(
                node.Handle,
                AncestorName(
                    state,
                    node.Handle,
                    SclSemanticKind.LogicalDevice),
                node.DisplayName,
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "lnClass"),
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "desc"),
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "lnType"),
                dataObjectCount));
        }

        return
        [
            .. rows.OrderBy(
                    static row => row.LogicalDevice,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static row => row.LogicalNode,
                    StringComparer.OrdinalIgnoreCase)
        ];
    }

    public static SclDataModelRowProjection[] BuildRows(
        SclDocumentState state,
        SclNodeHandle logicalNodeHandle)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.SemanticIndex.TryGetNode(
                logicalNodeHandle,
                out var logicalNode) ||
            logicalNode is null ||
            logicalNode.Kind is not (
                SclSemanticKind.LogicalNodeZero or
                SclSemanticKind.LogicalNode))
        {
            return [];
        }

        var logicalNodeType = FindOutgoingTarget(
            state,
            logicalNodeHandle,
            SclReferenceKind.TypeDefinition,
            SclSemanticKind.LogicalNodeType);

        if (logicalNodeType.IsNone)
        {
            return [];
        }

        var instances = BuildInstanceMap(
            state,
            logicalNodeHandle);

        var rows = new List<SclDataModelRowProjection>();
        var children = state.SemanticIndex.GetChildren(
            logicalNodeType);

        for (var i = 0; i < children.Count; i++)
        {
            var definition = children[i];

            if (definition.Kind != SclSemanticKind.DataObjectDefinition)
            {
                continue;
            }

            var name = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                definition.Handle,
                "name");

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var path = name;
            var instance = GetInstance(
                instances,
                path);

            var doType = FindOutgoingTarget(
                state,
                definition.Handle,
                SclReferenceKind.TypeDefinition,
                SclSemanticKind.DataObjectType);

            var cdc = doType.IsNone
                ? string.Empty
                : SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    doType,
                    "cdc");

            rows.Add(new SclDataModelRowProjection(
                instance.IsNone
                    ? definition.Handle
                    : instance,
                0,
                "DO",
                name,
                path,
                cdc,
                string.Empty,
                string.Empty,
                string.Empty,
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    definition.Handle,
                    "type"),
                GetDescription(
                    state,
                    instance,
                    definition.Handle)));

            if (!doType.IsNone)
            {
                AppendDataObjectType(
                    state,
                    doType,
                    path,
                    depth: 1,
                    inheritedFc: string.Empty,
                    instances,
                    rows,
                    new HashSet<SclNodeHandle>());
            }
        }

        return [.. rows];
    }

    private static void AppendDataObjectType(
        SclDocumentState state,
        SclNodeHandle doType,
        string parentPath,
        int depth,
        string inheritedFc,
        Dictionary<string, SclNodeHandle> instances,
        List<SclDataModelRowProjection> rows,
        HashSet<SclNodeHandle> typeStack)
    {
        if (depth > 32 ||
            !typeStack.Add(doType))
        {
            return;
        }

        try
        {
            var children = state.SemanticIndex.GetChildren(doType);

            for (var i = 0; i < children.Count; i++)
            {
                var definition = children[i];

                switch (definition.Kind)
                {
                    case SclSemanticKind.DataAttributeDefinition:
                        AppendDataAttribute(
                            state,
                            definition,
                            parentPath,
                            depth,
                            inheritedFc,
                            instances,
                            rows,
                            typeStack);
                        break;

                    case SclSemanticKind.SubDataObjectDefinition:
                        AppendSubDataObject(
                            state,
                            definition,
                            parentPath,
                            depth,
                            inheritedFc,
                            instances,
                            rows,
                            typeStack);
                        break;
                }
            }
        }
        finally
        {
            typeStack.Remove(doType);
        }
    }

    private static void AppendSubDataObject(
        SclDocumentState state,
        SclSemanticNode definition,
        string parentPath,
        int depth,
        string inheritedFc,
        Dictionary<string, SclNodeHandle> instances,
        List<SclDataModelRowProjection> rows,
        HashSet<SclNodeHandle> typeStack)
    {
        var name = SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            definition.Handle,
            "name");

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var path = string.Concat(
            parentPath,
            "/",
            name);

        var instance = GetInstance(
            instances,
            path);

        var target = FindOutgoingTarget(
            state,
            definition.Handle,
            SclReferenceKind.TypeDefinition,
            SclSemanticKind.DataObjectType);

        var cdc = target.IsNone
            ? string.Empty
            : SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                target,
                "cdc");

        rows.Add(new SclDataModelRowProjection(
            instance.IsNone
                ? definition.Handle
                : instance,
            depth,
            "SDO",
            name,
            path,
            cdc,
            inheritedFc,
            string.Empty,
            string.Empty,
            SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                definition.Handle,
                "type"),
            GetDescription(
                state,
                instance,
                definition.Handle)));

        if (!target.IsNone)
        {
            AppendDataObjectType(
                state,
                target,
                path,
                depth + 1,
                inheritedFc,
                instances,
                rows,
                typeStack);
        }
    }

    private static void AppendDataAttribute(
        SclDocumentState state,
        SclSemanticNode definition,
        string parentPath,
        int depth,
        string inheritedFc,
        Dictionary<string, SclNodeHandle> instances,
        List<SclDataModelRowProjection> rows,
        HashSet<SclNodeHandle> typeStack)
    {
        var name = SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            definition.Handle,
            "name");

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var path = string.Concat(
            parentPath,
            "/",
            name);

        var instance = GetInstance(
            instances,
            path);

        var fc = SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            definition.Handle,
            "fc");

        if (string.IsNullOrWhiteSpace(fc))
        {
            fc = inheritedFc;
        }

        var basicType = SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            definition.Handle,
            "bType");

        var typeId = SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            definition.Handle,
            "type");

        rows.Add(new SclDataModelRowProjection(
            instance.IsNone
                ? definition.Handle
                : instance,
            depth,
            "DA",
            name,
            path,
            string.Empty,
            fc,
            basicType,
            ReadInstanceValue(
                state,
                instance),
            typeId,
            GetDescription(
                state,
                instance,
                definition.Handle)));

        if (!string.Equals(
                basicType,
                "Struct",
                StringComparison.Ordinal) ||
            depth >= 32)
        {
            return;
        }

        var daType = FindOutgoingTarget(
            state,
            definition.Handle,
            SclReferenceKind.TypeDefinition,
            SclSemanticKind.DataAttributeType);

        if (!daType.IsNone)
        {
            AppendDataAttributeType(
                state,
                daType,
                path,
                depth + 1,
                fc,
                instances,
                rows,
                typeStack);
        }
    }

    private static void AppendDataAttributeType(
        SclDocumentState state,
        SclNodeHandle daType,
        string parentPath,
        int depth,
        string inheritedFc,
        Dictionary<string, SclNodeHandle> instances,
        List<SclDataModelRowProjection> rows,
        HashSet<SclNodeHandle> typeStack)
    {
        if (depth > 32 ||
            !typeStack.Add(daType))
        {
            return;
        }

        try
        {
            var children = state.SemanticIndex.GetChildren(daType);

            for (var i = 0; i < children.Count; i++)
            {
                var definition = children[i];

                if (definition.Kind !=
                    SclSemanticKind.BasicDataAttributeDefinition)
                {
                    continue;
                }

                var name = SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    definition.Handle,
                    "name");

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var path = string.Concat(
                    parentPath,
                    "/",
                    name);

                var instance = GetInstance(
                    instances,
                    path);

                var basicType = SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    definition.Handle,
                    "bType");

                var typeId = SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    definition.Handle,
                    "type");

                rows.Add(new SclDataModelRowProjection(
                    instance.IsNone
                        ? definition.Handle
                        : instance,
                    depth,
                    "BDA",
                    name,
                    path,
                    string.Empty,
                    inheritedFc,
                    basicType,
                    ReadInstanceValue(
                        state,
                        instance),
                    typeId,
                    GetDescription(
                        state,
                        instance,
                        definition.Handle)));

                if (!string.Equals(
                        basicType,
                        "Struct",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var nestedType = FindOutgoingTarget(
                    state,
                    definition.Handle,
                    SclReferenceKind.TypeDefinition,
                    SclSemanticKind.DataAttributeType);

                if (!nestedType.IsNone)
                {
                    AppendDataAttributeType(
                        state,
                        nestedType,
                        path,
                        depth + 1,
                        inheritedFc,
                        instances,
                        rows,
                        typeStack);
                }
            }
        }
        finally
        {
            typeStack.Remove(daType);
        }
    }

    private static Dictionary<string, SclNodeHandle> BuildInstanceMap(
        SclDocumentState state,
        SclNodeHandle logicalNode)
    {
        var result = new Dictionary<string, SclNodeHandle>(
            StringComparer.Ordinal);

        var children = state.SemanticIndex.GetChildren(
            logicalNode);

        for (var i = 0; i < children.Count; i++)
        {
            if (children[i].Kind == SclSemanticKind.Doi)
            {
                AddInstanceBranch(
                    state,
                    children[i],
                    parentPath: string.Empty,
                    result);
            }
        }

        return result;
    }

    private static void AddInstanceBranch(
        SclDocumentState state,
        SclSemanticNode node,
        string parentPath,
        Dictionary<string, SclNodeHandle> destination)
    {
        var name = SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            node.Handle,
            "name");

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var path = string.IsNullOrWhiteSpace(parentPath)
            ? name
            : string.Concat(
                parentPath,
                "/",
                name);

        destination[path] = node.Handle;

        var children = state.SemanticIndex.GetChildren(
            node.Handle);

        for (var i = 0; i < children.Count; i++)
        {
            if (children[i].Kind is
                SclSemanticKind.Sdi or
                SclSemanticKind.Dai)
            {
                AddInstanceBranch(
                    state,
                    children[i],
                    path,
                    destination);
            }
        }
    }

    private static string ReadInstanceValue(
        SclDocumentState state,
        SclNodeHandle instance)
    {
        if (instance.IsNone ||
            !SclWorkspaceSyntaxReader.TryFindDirectElement(
                state.Syntax,
                instance,
                "Val",
                out var value))
        {
            return string.Empty;
        }

        return SclWorkspaceSyntaxReader.ReadElementText(
            state.Syntax,
            value);
    }

    private static string GetDescription(
        SclDocumentState state,
        SclNodeHandle instance,
        SclNodeHandle definition)
    {
        if (!instance.IsNone)
        {
            var instanceDescription =
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    instance,
                    "desc");

            if (!string.IsNullOrWhiteSpace(instanceDescription))
            {
                return instanceDescription;
            }
        }

        return SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            definition,
            "desc");
    }

    private static SclNodeHandle GetInstance(
        Dictionary<string, SclNodeHandle> instances,
        string path) =>
        instances.TryGetValue(path, out var handle)
            ? handle
            : SclNodeHandle.None;

    private static SclNodeHandle FindOutgoingTarget(
        SclDocumentState state,
        SclNodeHandle source,
        SclReferenceKind kind,
        SclSemanticKind expectedKind)
    {
        var outgoing = state.SemanticIndex.References.GetOutgoing(
            source);

        for (var i = 0; i < outgoing.Count; i++)
        {
            var edge = outgoing[i];

            if (edge.Kind != kind)
            {
                continue;
            }

            if (state.SemanticIndex.TryGetNode(
                    edge.Target,
                    out var target) &&
                target is not null &&
                target.Kind == expectedKind)
            {
                return edge.Target;
            }
        }

        return SclNodeHandle.None;
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
