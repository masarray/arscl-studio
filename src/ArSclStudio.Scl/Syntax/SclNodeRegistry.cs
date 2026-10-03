using System.Xml;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Source;

namespace ArSclStudio.Scl.Syntax;

internal sealed class SclNodeRegistry
{
    private readonly Dictionary<XmlNode, long> _handles =
        new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<long, XmlNode> _nodes = [];
    private readonly Dictionary<long, SclSourceSpan> _spans = [];
    private long _nextHandle = 1;

    public int Count => _nodes.Count;

    internal SclNodeRegistry CreateSuccessor()
    {
        var successor = new SclNodeRegistry { _nextHandle = _nextHandle };
        successor._handles.EnsureCapacity(Count + 1);
        successor._nodes.EnsureCapacity(Count + 1);
        successor._spans.EnsureCapacity(Count + 1);
        return successor;
    }

    internal void RegisterAt(XmlNode node, SclNodeHandle handle, SclSourceSpan span)
    {
        _handles.Add(node, handle.Value);
        _nodes.Add(handle.Value, node);
        _spans.Add(handle.Value, span);
        _nextHandle = Math.Max(_nextHandle, handle.Value + 1);
    }

    internal void Remove(XmlNode node)
    {
        if (_handles.Remove(node, out var handle))
        {
            _nodes.Remove(handle);
            _spans.Remove(handle);
        }
    }

    public SclNodeHandle Register(XmlNode node, SclSourceSpan span)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_handles.TryGetValue(node, out var existing))
        {
            return new SclNodeHandle(existing);
        }

        var value = _nextHandle++;
        _nodes.Add(value, node);
        _spans.Add(value, span);
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
        if (_nodes.TryGetValue(handle.Value, out node))
        {
            return true;
        }

        node = null;
        return false;
    }

    public SclSourceSpan GetSourceSpan(SclNodeHandle handle)
    {
        if (_spans.TryGetValue(handle.Value, out var span))
        {
            return span;
        }

        return SclSourceSpan.Unknown;
    }
}

