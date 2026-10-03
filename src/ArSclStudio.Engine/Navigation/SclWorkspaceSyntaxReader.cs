using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Navigation;

internal static class SclWorkspaceSyntaxReader
{
    public static string Attribute(
        SclSyntaxDocument syntax,
        SclNodeHandle element,
        string name)
    {
        ArgumentNullException.ThrowIfNull(syntax);

        return syntax.TryGetAttributeValue(element, name, out var value)
            ? value ?? string.Empty
            : string.Empty;
    }

    public static bool TryFindDirectElement(
        SclSyntaxDocument syntax,
        SclNodeHandle parent,
        string localName,
        out SclNodeHandle handle)
    {
        ArgumentNullException.ThrowIfNull(syntax);
        ArgumentException.ThrowIfNullOrWhiteSpace(localName);

        var children = syntax.GetSelectableChildren(parent);

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];

            if (syntax.TryGetNodeInfo(child, out var info) &&
                info is not null &&
                info.Kind == SclSyntaxNodeKind.Element &&
                string.Equals(info.LocalName, localName, StringComparison.Ordinal))
            {
                handle = child;
                return true;
            }
        }

        handle = SclNodeHandle.None;
        return false;
    }

    public static Dictionary<string, string> ReadPValues(
        SclSyntaxDocument syntax,
        SclNodeHandle address)
    {
        ArgumentNullException.ThrowIfNull(syntax);

        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        var children = syntax.GetSelectableChildren(address);

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];

            if (!syntax.TryGetNodeInfo(child, out var info) ||
                info is null ||
                info.Kind != SclSyntaxNodeKind.Element ||
                !string.Equals(info.LocalName, "P", StringComparison.Ordinal))
            {
                continue;
            }

            var type = Attribute(syntax, child, "type");
            if (string.IsNullOrWhiteSpace(type))
            {
                continue;
            }

            result[type] = ReadElementText(syntax, child);
        }

        return result;
    }

    public static string ReadElementText(
        SclSyntaxDocument syntax,
        SclNodeHandle element)
    {
        ArgumentNullException.ThrowIfNull(syntax);

        var children = syntax.GetSelectableChildren(element);

        for (var i = 0; i < children.Count; i++)
        {
            if (syntax.TryGetNodeInfo(children[i], out var info) &&
                info is not null &&
                info.Kind is SclSyntaxNodeKind.Text or SclSyntaxNodeKind.CData)
            {
                return info.Value?.Trim() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    public static string ChildAttribute(
        SclSyntaxDocument syntax,
        SclNodeHandle parent,
        string childLocalName,
        string attribute)
    {
        return TryFindDirectElement(
                   syntax,
                   parent,
                   childLocalName,
                   out var child)
            ? Attribute(syntax, child, attribute)
            : string.Empty;
    }
}
