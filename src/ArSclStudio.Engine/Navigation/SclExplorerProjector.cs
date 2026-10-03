using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Navigation;

public static class SclExplorerProjector
{
    public static IReadOnlyList<ExplorerRowProjection> BuildEngineering(
        SclDocumentState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var syntax = state.Syntax;
        var index = state.TopLevelIndex;
        var capacity = 6 +
            index.Substations.Count +
            index.Communications.Count +
            index.Ieds.Count +
            index.PrivateElements.Count;

        var rows = new List<ExplorerRowProjection>(capacity)
        {
            CreateRealRow(
                syntax,
                syntax.RootHandle,
                ExplorerNodeKind.Document,
                0,
                state.DisplayName,
                ToBadge(syntax.Metadata.FileKindHint),
                isExpanded: true)
        };

        if (!index.Header.IsNone)
        {
            rows.Add(CreateRealRow(
                syntax,
                index.Header,
                ExplorerNodeKind.Header,
                1,
                "Header",
                syntax.Metadata.HeaderId,
                isExpanded: false));
        }

        for (var i = 0; i < index.Substations.Count; i++)
        {
            var handle = index.Substations[i];
            rows.Add(CreateRealRow(
                syntax,
                handle,
                ExplorerNodeKind.Substation,
                1,
                CreateNamedLabel(syntax, handle, "Substation"),
                null,
                isExpanded: false));
        }

        for (var i = 0; i < index.Communications.Count; i++)
        {
            var handle = index.Communications[i];
            rows.Add(CreateRealRow(
                syntax,
                handle,
                ExplorerNodeKind.Communication,
                1,
                index.Communications.Count == 1
                    ? "Communication"
                    : $"Communication {i + 1}",
                null,
                isExpanded: false));
        }

        if (index.Ieds.Count > 0)
        {
            rows.Add(new ExplorerRowProjection(
                SclNodeHandle.None,
                ExplorerNodeKind.Group,
                1,
                $"IEDs ({index.Ieds.Count})",
                null,
                false,
                true,
                true));

            for (var i = 0; i < index.Ieds.Count; i++)
            {
                var ied = index.Ieds[i];

                rows.Add(CreateRealRow(
                    syntax,
                    ied.Handle,
                    ExplorerNodeKind.Ied,
                    2,
                    string.IsNullOrWhiteSpace(ied.Name)
                        ? "IED"
                        : ied.Name,
                    ied.Manufacturer,
                    isExpanded: false));
            }
        }

        if (!index.DataTypeTemplates.IsNone)
        {
            rows.Add(CreateRealRow(
                syntax,
                index.DataTypeTemplates,
                ExplorerNodeKind.DataTypeTemplates,
                1,
                "DataTypeTemplates",
                null,
                isExpanded: false));
        }

        if (index.PrivateElements.Count > 0)
        {
            rows.Add(new ExplorerRowProjection(
                SclNodeHandle.None,
                ExplorerNodeKind.Group,
                1,
                $"Private & Extensions ({index.PrivateElements.Count})",
                null,
                false,
                true,
                true));

            for (var i = 0; i < index.PrivateElements.Count; i++)
            {
                var handle = index.PrivateElements[i];
                syntax.TryGetAttributeValue(handle, "type", out var type);

                rows.Add(CreateRealRow(
                    syntax,
                    handle,
                    ExplorerNodeKind.Private,
                    2,
                    string.IsNullOrWhiteSpace(type)
                        ? "Private"
                        : $"Private — {type}",
                    null,
                    isExpanded: false));
            }
        }

        return rows;
    }

    public static IReadOnlyList<ExplorerRowProjection> BuildXmlRoot(
        SclDocumentState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var syntax = state.Syntax;
        var root = CreateXmlRow(
            syntax,
            syntax.RootHandle,
            0,
            isExpanded: true);

        var children = syntax.GetSelectableChildren(syntax.RootHandle);
        var rows = new List<ExplorerRowProjection>(children.Count + 1)
        {
            root
        };

        for (var i = 0; i < children.Count; i++)
        {
            rows.Add(CreateXmlRow(
                syntax,
                children[i],
                1,
                isExpanded: false));
        }

        return rows;
    }

    private static ExplorerRowProjection CreateRealRow(
        SclSyntaxDocument syntax,
        SclNodeHandle handle,
        ExplorerNodeKind kind,
        int depth,
        string label,
        string? badge,
        bool isExpanded) =>
        new(
            handle,
            kind,
            depth,
            label,
            badge,
            true,
            syntax.HasSelectableChildren(handle),
            isExpanded);

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

    private static string CreateNamedLabel(
        SclSyntaxDocument syntax,
        SclNodeHandle handle,
        string fallback)
    {
        if (syntax.TryGetAttributeValue(handle, "name", out var name) &&
            !string.IsNullOrWhiteSpace(name))
        {
            return $"{fallback} — {name}";
        }

        return fallback;
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
            : string.Concat(trimmed.AsSpan(0, maximumLength - 1), "…");
    }

    private static string? ToBadge(Scl.Documents.SclFileKind fileKind) =>
        fileKind == Scl.Documents.SclFileKind.Unknown
            ? null
            : fileKind.ToString().ToUpperInvariant();
}
