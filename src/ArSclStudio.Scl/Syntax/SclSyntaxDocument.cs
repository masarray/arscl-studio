using System.Xml;
using ArSclStudio.Scl.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Source;

namespace ArSclStudio.Scl.Syntax;

public sealed class SclSyntaxDocument
{
    private readonly SclNodeRegistry _registry;

    internal SclSyntaxDocument(
        XmlDocument document,
        SclNodeRegistry registry,
        SclDocumentMetadata metadata,
        SclNodeHandle rootHandle)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(metadata);

        Document = document;
        _registry = registry;
        Metadata = metadata;
        RootHandle = rootHandle;
    }

    internal XmlDocument Document { get; }

    public SclDocumentMetadata Metadata { get; }

    public SclNodeHandle RootHandle { get; }

    public int IndexedNodeCount => _registry.Count;

    public bool TryGetNodeInfo(
        SclNodeHandle handle,
        out SclSyntaxNodeInfo? nodeInfo)
    {
        if (!_registry.TryGetNode(handle, out var node) || node is null)
        {
            nodeInfo = null;
            return false;
        }

        var parentNode = node is XmlAttribute attribute
            ? attribute.OwnerElement
            : node.ParentNode;

        _registry.TryGetHandle(parentNode, out var parent);

        nodeInfo = new SclSyntaxNodeInfo(
            handle,
            parent,
            GetKind(node),
            node.Prefix ?? string.Empty,
            node.LocalName ?? string.Empty,
            node.NamespaceURI ?? string.Empty,
            GetDisplayValue(node),
            _registry.GetSourceSpan(handle));

        return true;
    }

    public IReadOnlyList<SclNodeHandle> GetSelectableChildren(SclNodeHandle handle)
    {
        if (!_registry.TryGetNode(handle, out var node) || node is null)
        {
            return Array.Empty<SclNodeHandle>();
        }

        var result = new List<SclNodeHandle>();

        if (node is XmlElement element && element.HasAttributes)
        {
            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (_registry.TryGetHandle(attribute, out var attributeHandle))
                {
                    result.Add(attributeHandle);
                }
            }
        }

        foreach (XmlNode child in node.ChildNodes)
        {
            if (_registry.TryGetHandle(child, out var childHandle))
            {
                result.Add(childHandle);
            }
        }

        return result;
    }

    public SclSourceSpan GetSourceSpan(SclNodeHandle handle) =>
        _registry.GetSourceSpan(handle);

    public string CreateXmlSnapshot() => Document.OuterXml;

    internal bool TryGetNode(
        SclNodeHandle handle,
        out XmlNode? node) =>
        _registry.TryGetNode(handle, out node);

    internal bool TryGetHandle(
        XmlNode? node,
        out SclNodeHandle handle) =>
        _registry.TryGetHandle(node, out handle);

    private static SclSyntaxNodeKind GetKind(XmlNode node) =>
        node.NodeType switch
        {
            XmlNodeType.Element => SclSyntaxNodeKind.Element,
            XmlNodeType.Attribute => SclSyntaxNodeKind.Attribute,
            XmlNodeType.Text => SclSyntaxNodeKind.Text,
            XmlNodeType.CDATA => SclSyntaxNodeKind.CData,
            XmlNodeType.Comment => SclSyntaxNodeKind.Comment,
            XmlNodeType.ProcessingInstruction => SclSyntaxNodeKind.ProcessingInstruction,
            XmlNodeType.XmlDeclaration => SclSyntaxNodeKind.XmlDeclaration,
            _ => SclSyntaxNodeKind.Unknown
        };

    private static string? GetDisplayValue(XmlNode node) =>
        node.NodeType switch
        {
            XmlNodeType.Attribute or
            XmlNodeType.Text or
            XmlNodeType.CDATA or
            XmlNodeType.Comment or
            XmlNodeType.ProcessingInstruction => node.Value,
            _ => null
        };
}
