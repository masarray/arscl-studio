using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Engine.Navigation;

public sealed class SclSelectionChangedEventArgs : EventArgs
{
    public SclSelectionChangedEventArgs(SclNodeHandle selectedNode)
    {
        SelectedNode = selectedNode;
    }

    public SclNodeHandle SelectedNode { get; }
}

public sealed class SclSelectionService
{
    public SclNodeHandle SelectedNode { get; private set; }

    public event EventHandler<SclSelectionChangedEventArgs>? SelectionChanged;

    public bool Select(SclNodeHandle handle)
    {
        if (handle.IsNone || handle == SelectedNode)
        {
            return false;
        }

        SelectedNode = handle;
        SelectionChanged?.Invoke(
            this,
            new SclSelectionChangedEventArgs(handle));

        return true;
    }

    public void Clear()
    {
        SelectedNode = SclNodeHandle.None;
    }
}
