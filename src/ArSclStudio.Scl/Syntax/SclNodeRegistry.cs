using System.Xml;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Source;

namespace ArSclStudio.Scl.Syntax;

internal sealed class SclNodeRegistry
{
    private readonly Dictionary<XmlNode, long> _handles =
        new(ReferenceEqualityComparer.Instance);

    private readonly List<XmlNode?> _nodes = [null];
    private readonly List<SclSourceSpan> _spans = [SclSourceSpan.Unknown];

    public int Count => _nodes.Count - 1;

    public SclNodeHandle Register(XmlNode node, SclSourceSpan span)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_handles.TryGetValue(node, out var existing))
        {
            return new SclNodeHandle(existing);
        }

        var value = _nodes.Count;
        _nodes.Add(node);
        _spans.Add(span);
        _handles.Add(node, value);

        return new SclNodeHandle(value);
    }

    public bool TryGetHandle(XmlNode? node, out SclNodeHandle handle)
    {
        if (node is not null && _handles.TryGetValue(node, out var value))
        {
            handle = new SclNodeHandle(value);
            return true;
        }

        handle = SclNodeHandle.None;
        return false;
    }

    public bool TryGetNode(SclNodeHandle handle, out XmlNode? node)
    {
        if (handle.Value > 0 && handle.Value < _nodes.Count)
        {
            node = _nodes[(int)handle.Value];
            return node is not null;
        }

        node = null;
        return false;
    }

    public SclSourceSpan GetSourceSpan(SclNodeHandle handle)
    {
        if (handle.Value > 0 && handle.Value < _spans.Count)
        {
            return _spans[(int)handle.Value];
        }

        return SclSourceSpan.Unknown;
    }
}
