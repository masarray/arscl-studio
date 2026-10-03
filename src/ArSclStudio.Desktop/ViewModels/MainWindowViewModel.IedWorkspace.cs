using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArSclStudio.Desktop.ViewModels;

public sealed partial class MainWindowViewModel
{
    [ObservableProperty]
    private IReadOnlyList<SclIedWorkspaceProjection> _iedWorkspaceRows =
        Array.Empty<SclIedWorkspaceProjection>();

    [ObservableProperty]
    private SclIedWorkspaceProjection? _selectedIedWorkspace;

    public string IedWorkspaceHeader =>
        IedWorkspaceRows.Count == 0
            ? "IEDs"
            : $"IEDs ({IedWorkspaceRows.Count})";

    partial void OnSelectedIedWorkspaceChanged(SclIedWorkspaceProjection? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    private void RefreshIedWorkspace(SclDocumentState state)
    {
        var previous = SelectedIedWorkspace?.Handle;
        var rows = SclIedWorkspaceProjector.Build(state);

        IedWorkspaceRows = rows;
        OnPropertyChanged(nameof(IedWorkspaceHeader));

        SelectedIedWorkspace = previous is { } previousHandle
            ? rows.FirstOrDefault(row => row.Handle == previousHandle)
            : null;

        if (SelectedIedWorkspace is null && rows.Length != 0)
        {
            SelectedIedWorkspace = rows[0];
        }
    }

    private void SynchronizeIedWorkspaceSelection(
        SclDocumentState state,
        SclNodeHandle selected)
    {
        if (IedWorkspaceRows.Count == 0)
        {
            SelectedIedWorkspace = null;
            return;
        }

        SclNodeHandle iedHandle;

        if (state.SemanticIndex.TryGetNode(selected, out var selectedNode) &&
            selectedNode is not null &&
            selectedNode.Kind == SclSemanticKind.Ied)
        {
            iedHandle = selectedNode.Handle;
        }
        else if (state.SemanticIndex.TryFindAncestor(
                     selected,
                     SclSemanticKind.Ied,
                     out var ied) &&
                 ied is not null)
        {
            iedHandle = ied.Handle;
        }
        else
        {
            return;
        }

        SelectedIedWorkspace = IedWorkspaceRows.FirstOrDefault(
            row => row.Handle == iedHandle);
    }
}
