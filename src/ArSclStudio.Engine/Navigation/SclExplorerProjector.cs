using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Navigation;

public static class SclExplorerProjector
{
    public static IReadOnlyList<ExplorerRowProjection> BuildEngineering(
        SclDocumentState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var expanded = new HashSet<SclNodeHandle>
        {
            state.Syntax.RootHandle
        };

        return BuildEngineering(state, expanded);
    }

    public static IReadOnlyList<ExplorerRowProjection> BuildEngineering(
        SclDocumentState state,
        IReadOnlySet<SclNodeHandle> expanded)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(expanded);

        var rows = new List<ExplorerRowProjection>();
        var stack = new Stack<VisibleSemanticItem>();

        if (!state.SemanticIndex.TryGetNode(
                state.Syntax.RootHandle,
                out var root) ||
            root is null)
        {
            return rows;
        }

        stack.Push(new VisibleSemanticItem(root, 0));

        while (stack.Count != 0)
        {
            var item = stack.Pop();
            var node = item.Node;
            var isExpanded = expanded.Contains(node.Handle);

            rows.Add(CreateSemanticRow(
                state,
                node,
                item.Depth,
                isExpanded));

            if (!isExpanded)
            {
                continue;
            }

            var children = state.SemanticIndex.GetChildren(node.Handle);

            for (var i = children.Count - 1; i >= 0; i--)
            {
                stack.Push(new VisibleSemanticItem(
                    children[i],
                    item.Depth + 1));
            }
        }

        return rows;
    }

    public static IReadOnlyList<ExplorerRowProjection> BuildXmlRoot(
        SclDocumentState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var expanded = new HashSet<SclNodeHandle>
        {
            state.Syntax.RootHandle
        };

        return BuildXmlVisible(state, expanded);
    }

    public static IReadOnlyList<ExplorerRowProjection> BuildXmlVisible(
        SclDocumentState state,
        IReadOnlySet<SclNodeHandle> expanded)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(expanded);

        var syntax = state.Syntax;
        var rows = new List<ExplorerRowProjection>();
        var stack = new Stack<VisibleXmlItem>();

        stack.Push(new VisibleXmlItem(
            syntax.RootHandle,
            0));

        while (stack.Count != 0)
        {
            var item = stack.Pop();
            var isExpanded = expanded.Contains(item.Handle);

            rows.Add(CreateXmlRow(
                syntax,
                item.Handle,
                item.Depth,
                isExpanded));

            if (!isExpanded)
            {
                continue;
            }

            var children = syntax.GetSelectableChildren(item.Handle);

            for (var i = children.Count - 1; i >= 0; i--)
            {
                stack.Push(new VisibleXmlItem(
                    children[i],
                    item.Depth + 1));
            }
        }

        return rows;
    }

    private static ExplorerRowProjection CreateSemanticRow(
        SclDocumentState state,
        SclSemanticNode node,
        int depth,
        bool isExpanded)
    {
        var label = node.Kind switch
        {
            SclSemanticKind.Document => state.DisplayName,
            SclSemanticKind.Header when
                !string.IsNullOrWhiteSpace(state.Syntax.Metadata.HeaderId) =>
                $"Header — {state.Syntax.Metadata.HeaderId}",
            _ => node.DisplayName
        };

        var badge = node.Kind == SclSemanticKind.Document
            ? ToBadge(state.Syntax.Metadata.FileKindHint)
            : node.Badge;

        return new ExplorerRowProjection(
            node.Handle,
            MapSemanticKind(node.Kind),
            depth,
            label,
            badge,
            true,
            state.SemanticIndex.HasChildren(node.Handle),
            isExpanded);
    }

    private static ExplorerNodeKind MapSemanticKind(SclSemanticKind kind) =>
        kind switch
        {
            SclSemanticKind.Document => ExplorerNodeKind.Document,
            SclSemanticKind.Header => ExplorerNodeKind.Header,
            SclSemanticKind.Substation => ExplorerNodeKind.Substation,
            SclSemanticKind.Communication => ExplorerNodeKind.Communication,
            SclSemanticKind.SubNetwork => ExplorerNodeKind.SubNetwork,
            SclSemanticKind.ConnectedAccessPoint => ExplorerNodeKind.ConnectedAccessPoint,
            SclSemanticKind.Address => ExplorerNodeKind.Address,
            SclSemanticKind.Ied => ExplorerNodeKind.Ied,
            SclSemanticKind.Services => ExplorerNodeKind.Services,
            SclSemanticKind.AccessPoint => ExplorerNodeKind.AccessPoint,
            SclSemanticKind.Server => ExplorerNodeKind.Server,
            SclSemanticKind.LogicalDevice => ExplorerNodeKind.LogicalDevice,
            SclSemanticKind.LogicalNodeZero or
            SclSemanticKind.LogicalNode => ExplorerNodeKind.LogicalNode,
            SclSemanticKind.DataSet => ExplorerNodeKind.DataSet,
            SclSemanticKind.Fcda => ExplorerNodeKind.DataSetMember,
            SclSemanticKind.ReportControl => ExplorerNodeKind.ReportControl,
            SclSemanticKind.LogControl => ExplorerNodeKind.LogControl,
            SclSemanticKind.GseControl => ExplorerNodeKind.GseControl,
            SclSemanticKind.SampledValueControl => ExplorerNodeKind.SampledValueControl,
            SclSemanticKind.Inputs => ExplorerNodeKind.Inputs,
            SclSemanticKind.ExternalReference => ExplorerNodeKind.ExternalReference,
            SclSemanticKind.SettingGroupControl => ExplorerNodeKind.SettingGroupControl,
            SclSemanticKind.Doi => ExplorerNodeKind.DataObjectInstance,
            SclSemanticKind.Sdi => ExplorerNodeKind.SubDataInstance,
            SclSemanticKind.Dai => ExplorerNodeKind.DataAttributeInstance,
            SclSemanticKind.DataTypeTemplates => ExplorerNodeKind.DataTypeTemplates,
            SclSemanticKind.LogicalNodeType => ExplorerNodeKind.LogicalNodeType,
            SclSemanticKind.DataObjectType => ExplorerNodeKind.DataObjectType,
            SclSemanticKind.DataAttributeType => ExplorerNodeKind.DataAttributeType,
            SclSemanticKind.EnumerationType => ExplorerNodeKind.EnumerationType,
            SclSemanticKind.DataObjectDefinition => ExplorerNodeKind.DataObjectDefinition,
            SclSemanticKind.SubDataObjectDefinition => ExplorerNodeKind.SubDataObjectDefinition,
            SclSemanticKind.DataAttributeDefinition => ExplorerNodeKind.DataAttributeDefinition,
            SclSemanticKind.BasicDataAttributeDefinition => ExplorerNodeKind.BasicDataAttributeDefinition,
            SclSemanticKind.Private => ExplorerNodeKind.Private,
            _ => ExplorerNodeKind.XmlOther
        };

    private static ExplorerRowProjection CreateXmlRow(
        SclSyntaxDocument syntax,
        SclNodeHandle handle,
        int depth,
        bool isExpanded)
    {
        if (!syntax.TryGetNodeInfo(handle, out var info) || info is null)
        {
            return new ExplorerRowProjection(
                handle,
                ExplorerNodeKind.XmlOther,
                depth,
                "(unresolved)",
                null,
                false,
                false,
                false);
        }

        var kind = info.Kind switch
        {
            SclSyntaxNodeKind.Element => ExplorerNodeKind.XmlElement,
            SclSyntaxNodeKind.Attribute => ExplorerNodeKind.XmlAttribute,
            SclSyntaxNodeKind.Text or SclSyntaxNodeKind.CData => ExplorerNodeKind.XmlText,
            SclSyntaxNodeKind.Comment => ExplorerNodeKind.XmlComment,
            SclSyntaxNodeKind.ProcessingInstruction => ExplorerNodeKind.XmlProcessingInstruction,
            _ => ExplorerNodeKind.XmlOther
        };

        return new ExplorerRowProjection(
            handle,
            kind,
            depth,
            CreateXmlLabel(syntax, info),
            null,
            true,
            syntax.HasSelectableChildren(handle),
            isExpanded);
    }

    private static string CreateXmlLabel(
        SclSyntaxDocument syntax,
        SclSyntaxNodeInfo info)
    {
        var qualifiedName = string.IsNullOrWhiteSpace(info.Prefix)
            ? info.LocalName
            : $"{info.Prefix}:{info.LocalName}";

        switch (info.Kind)
        {
            case SclSyntaxNodeKind.Element:
                if (syntax.TryGetAttributeValue(info.Handle, "name", out var name) &&
                    !string.IsNullOrWhiteSpace(name))
                {
                    return $"<{qualifiedName}>  name=\"{name}\"";
                }

                if (syntax.TryGetAttributeValue(info.Handle, "id", out var id) &&
                    !string.IsNullOrWhiteSpace(id))
                {
                    return $"<{qualifiedName}>  id=\"{id}\"";
                }

                return $"<{qualifiedName}>";

            case SclSyntaxNodeKind.Attribute:
                return $"@{qualifiedName} = \"{TrimValue(info.Value)}\"";

            case SclSyntaxNodeKind.Comment:
                return $"<!-- {TrimValue(info.Value)} -->";

            case SclSyntaxNodeKind.ProcessingInstruction:
                return $"<?{qualifiedName} {TrimValue(info.Value)}?>";

            case SclSyntaxNodeKind.Text:
            case SclSyntaxNodeKind.CData:
                return TrimValue(info.Value);

            default:
                return qualifiedName;
        }
    }

    private static string TrimValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        const int maximumLength = 96;
        var trimmed = value.Trim();

        return trimmed.Length <= maximumLength
            ? trimmed
            : string.Concat(
                trimmed.AsSpan(0, maximumLength - 1),
                "…");
    }

    private static string? ToBadge(Scl.Documents.SclFileKind fileKind) =>
        fileKind == Scl.Documents.SclFileKind.Unknown
            ? null
            : fileKind.ToString().ToUpperInvariant();

    private readonly record struct VisibleSemanticItem(
        SclSemanticNode Node,
        int Depth);

    private readonly record struct VisibleXmlItem(
        SclNodeHandle Handle,
        int Depth);
}
