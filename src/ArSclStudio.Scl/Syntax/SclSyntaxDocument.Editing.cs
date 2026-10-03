using System.Text;
using System.Xml;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Source;

namespace ArSclStudio.Scl.Syntax;

public sealed partial class SclSyntaxDocument
{
    // Friend access is restricted to Engine. Published syntax instances are never mutated.
    internal SclSyntaxDocument CreateEditableCopy(CancellationToken cancellationToken)
    {
        var copy = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        var registry = _registry.CreateSuccessor();
        var stack = new Stack<(XmlNode Source, XmlNode Parent)>();
        PushChildren(Document, copy, stack);
        while (stack.TryPop(out var item))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var node = copy.ImportNode(item.Source, deep: false);
            item.Parent.AppendChild(node);
            CopyIdentity(item.Source, node, registry);
            PushChildren(item.Source, node, stack);
        }

        return new SclSyntaxDocument(copy, registry, Metadata, RootHandle);
    }

    internal void SetDescription(SclNodeHandle handle, string? value)
    {
        if (!TryGetNode(handle, out var node) || node is not XmlElement element)
        {
            throw new InvalidOperationException("Description target no longer exists.");
        }

        var attribute = element.GetAttributeNode("desc", string.Empty);
        if (value is null)
        {
            if (attribute is not null)
            {
                element.Attributes.Remove(attribute);
                _registry.Remove(attribute);
            }
        }
        else if (attribute is not null)
        {
            attribute.Value = value;
        }
        else
        {
            attribute = Document.CreateAttribute("desc");
            attribute.Value = value;
            element.Attributes.Append(attribute);
            _registry.Register(attribute, SclSourceSpan.Unknown);
        }
    }

    // Linear walk with cancellation between nodes; no giant serialized string or recursion.
    internal void WriteTo(Stream stream, CancellationToken cancellationToken)
    {
        var declaration = Document.FirstChild as XmlDeclaration;
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            NewLineHandling = NewLineHandling.Entitize,
            OmitXmlDeclaration = declaration is null,
            CloseOutput = false,
            CheckCharacters = true
        });
        if (declaration?.Standalone == "yes")
        {
            writer.WriteStartDocument(true);
        }
        else if (declaration?.Standalone == "no")
        {
            writer.WriteStartDocument(false);
        }

        var stack = new Stack<(XmlNode Node, bool End)>();
        for (var node = Document.LastChild; node is not null; node = node.PreviousSibling)
        {
            stack.Push((node, false));
        }

        while (stack.TryPop(out var item))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.End)
            {
                writer.WriteFullEndElement();
                continue;
            }

            if (item.Node is XmlDeclaration)
            {
                continue;
            }

            if (item.Node is XmlElement element)
            {
                writer.WriteStartElement(element.Prefix, element.LocalName, element.NamespaceURI);
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    attribute.WriteTo(writer);
                }

                if (element.IsEmpty)
                {
                    writer.WriteEndElement();
                }
                else
                {
                    stack.Push((element, true));
                    for (var child = element.LastChild; child is not null; child = child.PreviousSibling)
                    {
                        stack.Push((child, false));
                    }
                }
            }
            else
            {
                item.Node.WriteTo(writer);
            }
        }

        writer.Flush();
    }

    // Verify all XML content, including vendor data. Declaration encoding becomes UTF-8.
    internal SclSyntaxDocument VerifyAndRebind(SclSyntaxDocument reopened, CancellationToken cancellationToken)
    {
        var registry = _registry.CreateSuccessor();
        var stack = new Stack<(XmlNode Expected, XmlNode Actual)>();
        stack.Push((Document, reopened.Document));
        while (stack.TryPop(out var item))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expected = item.Expected;
            var actual = item.Actual;
            if (expected.NodeType != actual.NodeType || expected.Name != actual.Name ||
                expected.NamespaceURI != actual.NamespaceURI)
            {
                throw new XmlException("Save verification failed: XML structure changed.");
            }

            if (expected is XmlDeclaration left && actual is XmlDeclaration right)
            {
                if (left.Version != right.Version || left.Standalone != right.Standalone)
                {
                    throw new XmlException("Save verification failed: XML declaration changed.");
                }
            }
            else if (expected.Value != actual.Value)
            {
                throw new XmlException("Save verification failed: XML content changed.");
            }

            if ((expected.Attributes?.Count ?? 0) != (actual.Attributes?.Count ?? 0))
            {
                throw new XmlException("Save verification failed: XML attributes changed.");
            }

            if (expected.Attributes is { } attributes && actual.Attributes is { } actualAttributes)
            {
                for (var i = 0; i < attributes.Count; i++)
                {
                    var a = attributes[i];
                    var b = actualAttributes[i];
                    if (a.Name != b.Name || a.NamespaceURI != b.NamespaceURI || a.Value != b.Value)
                    {
                        throw new XmlException("Save verification failed: attribute content changed.");
                    }
                }
            }

            CopyIdentity(expected, actual, registry, reopened);
            var expectedChild = expected.LastChild;
            var actualChild = actual.LastChild;
            while (expectedChild is not null && actualChild is not null)
            {
                stack.Push((expectedChild, actualChild));
                expectedChild = expectedChild.PreviousSibling;
                actualChild = actualChild.PreviousSibling;
            }

            if (expectedChild is not null || actualChild is not null)
            {
                throw new XmlException("Save verification failed: child count changed.");
            }
        }

        return new SclSyntaxDocument(reopened.Document, registry, reopened.Metadata, RootHandle);
    }

    private void CopyIdentity(XmlNode source, XmlNode destination, SclNodeRegistry registry,
        SclSyntaxDocument? reopened = null)
    {
        if (TryGetHandle(source, out var handle))
        {
            var span = reopened is not null && reopened.TryGetHandle(destination, out var newHandle)
                ? reopened.GetSourceSpan(newHandle) : GetSourceSpan(handle);
            registry.RegisterAt(destination, handle, span);
        }

        if (source.Attributes is { } attributes && destination.Attributes is { } copies)
        {
            for (var i = 0; i < attributes.Count; i++)
            {
                if (TryGetHandle(attributes[i], out var attributeHandle))
                {
                    var span = reopened is not null && reopened.TryGetHandle(copies[i], out var newHandle)
                        ? reopened.GetSourceSpan(newHandle) : GetSourceSpan(attributeHandle);
                    registry.RegisterAt(copies[i], attributeHandle, span);
                }
            }
        }
    }

    private static void PushChildren(XmlNode source, XmlNode parent,
        Stack<(XmlNode Source, XmlNode Parent)> stack)
    {
        for (var child = source.LastChild; child is not null; child = child.PreviousSibling)
        {
            stack.Push((child, parent));
        }
    }
}
