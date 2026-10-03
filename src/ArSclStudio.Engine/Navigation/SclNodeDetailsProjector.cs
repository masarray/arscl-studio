using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Navigation;

public static class SclNodeDetailsProjector
{
    public static SclNodeDetailsProjection Create(
        SclDocumentState state,
        SclNodeHandle handle)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.Syntax.TryGetNodeInfo(handle, out var info) ||
            info is null)
        {
            return new SclNodeDetailsProjection(
                handle,
                "Unresolved node",
                "Unknown",
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                "The selected node is no longer available in this document revision.");
        }

        var title = GetPrimaryName(state.Syntax, info);
        var kind = GetKindName(info);
        var path = BuildPath(state.Syntax, info);
        var sourceLocation = info.SourceSpan.IsKnown
            ? $"{state.DisplayName}:{info.SourceSpan}"
            : state.DisplayName;

        return new SclNodeDetailsProjection(
            handle,
            title,
            kind,
            path,
            sourceLocation,
            info.NamespaceUri,
            info.Value,
            GetDescription(info));
    }

    private static string GetPrimaryName(
        SclSyntaxDocument syntax,
        SclSyntaxNodeInfo info)
    {
        if (info.Kind == SclSyntaxNodeKind.Attribute)
        {
            return string.IsNullOrWhiteSpace(info.Prefix)
                ? $"@{info.LocalName}"
                : $"@{info.Prefix}:{info.LocalName}";
        }

        if (info.Kind != SclSyntaxNodeKind.Element)
        {
            return info.LocalName;
        }

        if (syntax.TryGetAttributeValue(info.Handle, "name", out var name) &&
            !string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (syntax.TryGetAttributeValue(info.Handle, "id", out var id) &&
            !string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        if (syntax.TryGetAttributeValue(info.Handle, "type", out var type) &&
            !string.IsNullOrWhiteSpace(type))
        {
            return type;
        }

        return info.LocalName;
    }

    private static string GetKindName(SclSyntaxNodeInfo info) =>
        info.Kind == SclSyntaxNodeKind.Element
            ? info.LocalName
            : info.Kind.ToString();

    private static string BuildPath(
        SclSyntaxDocument syntax,
        SclSyntaxNodeInfo selected)
    {
        var segments = new List<string>(12);
        var current = selected;

        for (var depth = 0; depth < 128; depth++)
        {
            segments.Add(GetPathSegment(syntax, current));

            if (current.Parent.IsNone ||
                !syntax.TryGetNodeInfo(current.Parent, out var parent) ||
                parent is null)
            {
                break;
            }

            current = parent;
        }

        segments.Reverse();
        return string.Join(" / ", segments);
    }

    private static string GetPathSegment(
        SclSyntaxDocument syntax,
        SclSyntaxNodeInfo info)
    {
        if (info.Kind == SclSyntaxNodeKind.Attribute)
        {
            return $"@{info.LocalName}";
        }

        if (info.Kind != SclSyntaxNodeKind.Element)
        {
            return info.Kind.ToString();
        }

        if (syntax.TryGetAttributeValue(info.Handle, "name", out var name) &&
            !string.IsNullOrWhiteSpace(name))
        {
            return $"{info.LocalName}[{name}]";
        }

        if (syntax.TryGetAttributeValue(info.Handle, "id", out var id) &&
            !string.IsNullOrWhiteSpace(id))
        {
            return $"{info.LocalName}[{id}]";
        }

        return info.LocalName;
    }

    private static string GetDescription(SclSyntaxNodeInfo info) =>
        info.Kind == SclSyntaxNodeKind.Element
            ? info.LocalName switch
            {
                "SCL" => "Root of the IEC 61850 System Configuration Language document.",
                "Header" => "Document identification and engineering history metadata.",
                "Substation" => "Primary-system structure and its logical-node mappings.",
                "Communication" => "Communication-network configuration and IED access-point bindings.",
                "IED" => "An IED definition containing capabilities, access points, servers, logical devices, and configured services.",
                "DataTypeTemplates" => "Shared IEC 61850 logical-node, data-object, data-attribute, and enumeration type definitions.",
                "Private" => "Vendor or tool-specific extension content. ARSCL preserves this content even when its semantics are unknown.",
                _ => "IEC 61850 SCL structure. Deeper semantic explanation will be added as the model index expands."
            }
            : "XML syntax node belonging to the same authoritative SCL document.";
}
