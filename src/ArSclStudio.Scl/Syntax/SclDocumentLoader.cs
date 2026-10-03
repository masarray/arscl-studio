using System.Xml;
using ArSclStudio.Scl.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Source;
using ArSclStudio.Scl.Xml;

namespace ArSclStudio.Scl.Syntax;

public sealed class SclDocumentLoader
{
    private readonly SecureXmlReaderOptions _readerOptions;

    public SclDocumentLoader(SecureXmlReaderOptions? readerOptions = null)
    {
        _readerOptions = readerOptions ?? new SecureXmlReaderOptions();
    }

    public async ValueTask<SclSyntaxDocument> LoadFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 128 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await LoadAsync(
            stream,
            Path.GetFileName(path),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<SclSyntaxDocument> LoadAsync(
        Stream stream,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = SecureXmlReaderFactory.Create(stream, _readerOptions);
        var document = new XmlDocument
        {
            PreserveWhitespace = true,
            XmlResolver = null
        };

        var registry = new SclNodeRegistry();
        var elementStack = new Stack<XmlElement>();
        var rootHandle = SclNodeHandle.None;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parent = elementStack.Count == 0
                ? (XmlNode)document
                : elementStack.Peek();

            switch (reader.NodeType)
            {
                case XmlNodeType.XmlDeclaration:
                {
                    var declaration = document.CreateXmlDeclaration(
                        reader.GetAttribute("version") ?? "1.0",
                        reader.GetAttribute("encoding"),
                        reader.GetAttribute("standalone"));

                    document.AppendChild(declaration);
                    registry.Register(declaration, GetSourceSpan(reader));
                    break;
                }

                case XmlNodeType.Element:
                {
                    var element = document.CreateElement(
                        reader.Prefix,
                        reader.LocalName,
                        reader.NamespaceURI);

                    if (reader.HasAttributes)
                    {
                        while (reader.MoveToNextAttribute())
                        {
                            var attribute = document.CreateAttribute(
                                reader.Prefix,
                                reader.LocalName,
                                reader.NamespaceURI);

                            attribute.Value = reader.Value;
                            element.Attributes.Append(attribute);
                            registry.Register(attribute, GetSourceSpan(reader));
                        }

                        reader.MoveToElement();
                    }

                    parent.AppendChild(element);
                    var elementHandle = registry.Register(
                        element,
                        GetSourceSpan(reader));

                    if (rootHandle.IsNone)
                    {
                        rootHandle = elementHandle;
                    }

                    if (!reader.IsEmptyElement)
                    {
                        elementStack.Push(element);
                    }

                    break;
                }

                case XmlNodeType.EndElement:
                    if (elementStack.Count == 0)
                    {
                        throw new XmlException("Unexpected end element while building the SCL document.");
                    }

                    elementStack.Pop();
                    break;

                case XmlNodeType.Text:
                    AppendSelectable(
                        document.CreateTextNode(reader.Value),
                        parent,
                        registry,
                        reader);
                    break;

                case XmlNodeType.CDATA:
                    AppendSelectable(
                        document.CreateCDataSection(reader.Value),
                        parent,
                        registry,
                        reader);
                    break;

                case XmlNodeType.Comment:
                    AppendSelectable(
                        document.CreateComment(reader.Value),
                        parent,
                        registry,
                        reader);
                    break;

                case XmlNodeType.ProcessingInstruction:
                    AppendSelectable(
                        document.CreateProcessingInstruction(
                            reader.Name,
                            reader.Value),
                        parent,
                        registry,
                        reader);
                    break;

                case XmlNodeType.Whitespace:
                    parent.AppendChild(document.CreateWhitespace(reader.Value));
                    break;

                case XmlNodeType.SignificantWhitespace:
                    parent.AppendChild(document.CreateSignificantWhitespace(reader.Value));
                    break;

                case XmlNodeType.DocumentType:
                    throw new XmlException("DTD content is not allowed.");

                default:
                    break;
            }
        }

        if (elementStack.Count != 0)
        {
            throw new XmlException("Unexpected end of document while building the SCL element stack.");
        }

        var root = document.DocumentElement;

        if (root is null ||
            !string.Equals(root.LocalName, "SCL", StringComparison.Ordinal))
        {
            throw new XmlException("The document root is not an IEC 61850 SCL element.");
        }

        var header = FindDirectChild(root, "Header");

        var metadata = new SclDocumentMetadata(
            SclDocumentProbe.ClassifyFromFileName(fileName),
            root.NamespaceURI ?? string.Empty,
            new SclSchemaRevision(
                root.GetAttribute("version"),
                root.GetAttribute("revision"),
                root.GetAttribute("release")),
            header?.GetAttribute("id"));

        return new SclSyntaxDocument(
            document,
            registry,
            metadata,
            rootHandle);
    }

    private static void AppendSelectable(
        XmlNode node,
        XmlNode parent,
        SclNodeRegistry registry,
        XmlReader reader)
    {
        parent.AppendChild(node);
        registry.Register(node, GetSourceSpan(reader));
    }

    private static SclSourceSpan GetSourceSpan(XmlReader reader)
    {
        if (reader is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            return new SclSourceSpan(
                lineInfo.LineNumber,
                lineInfo.LinePosition);
        }

        return SclSourceSpan.Unknown;
    }

    private static XmlElement? FindDirectChild(
        XmlElement parent,
        string localName)
    {
        foreach (XmlNode child in parent.ChildNodes)
        {
            if (child is XmlElement element &&
                string.Equals(
                    element.LocalName,
                    localName,
                    StringComparison.Ordinal))
            {
                return element;
            }
        }

        return null;
    }
}
