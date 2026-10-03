using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArSclStudio.Desktop.ViewModels;

public sealed partial class MainWindowViewModel
{
    private SclNodeHandle _workspaceIedHandle;

    [ObservableProperty]
    private int _selectedEngineeringWorkspaceIndex;

    [ObservableProperty]
    private IReadOnlyList<SclNetworkWorkspaceProjection> _networkWorkspaceRows =
        Array.Empty<SclNetworkWorkspaceProjection>();

    [ObservableProperty]
    private SclNetworkWorkspaceProjection? _selectedNetworkWorkspaceRow;

    [ObservableProperty]
    private IReadOnlyList<SclDataSetWorkspaceProjection> _dataSetWorkspaceRows =
        Array.Empty<SclDataSetWorkspaceProjection>();

    [ObservableProperty]
    private SclDataSetWorkspaceProjection? _selectedDataSetWorkspace;

    [ObservableProperty]
    private IReadOnlyList<SclDataSetMemberProjection> _dataSetMemberRows =
        Array.Empty<SclDataSetMemberProjection>();

    [ObservableProperty]
    private SclDataSetMemberProjection? _selectedDataSetMember;

    [ObservableProperty]
    private IReadOnlyList<SclReportWorkspaceProjection> _reportWorkspaceRows =
        Array.Empty<SclReportWorkspaceProjection>();

    [ObservableProperty]
    private SclReportWorkspaceProjection? _selectedReportWorkspace;

    public string DataSetWorkspaceHeader =>
        DataSetWorkspaceRows.Count == 0
            ? "DataSets"
            : $"DataSets ({DataSetWorkspaceRows.Count})";

    public string ReportWorkspaceHeader =>
        ReportWorkspaceRows.Count == 0
            ? "Reports & Logs"
            : $"Reports & Logs ({ReportWorkspaceRows.Count})";

    partial void OnSelectedIedWorkspaceChanged(SclIedWorkspaceProjection? value)
    {
        if (value is not null &&
            !value.Handle.IsNone &&
            _session.CurrentState is { } state)
        {
            RefreshIedScopedWorkspaces(state, value.Handle);

            if (!_synchronizingSelection)
            {
                _selectionService.Select(value.Handle);
            }
        }
    }

    partial void OnSelectedNetworkWorkspaceRowChanged(
        SclNetworkWorkspaceProjection? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    partial void OnSelectedDataSetWorkspaceChanged(
        SclDataSetWorkspaceProjection? value)
    {
        if (_session.CurrentState is { } state)
        {
            DataSetMemberRows = value is null
                ? Array.Empty<SclDataSetMemberProjection>()
                : SclDataSetWorkspaceProjector.BuildMembers(
                    state,
                    value.Handle);

            OnPropertyChanged(nameof(DataSetWorkspaceHeader));
        }

        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    partial void OnSelectedDataSetMemberChanged(
        SclDataSetMemberProjection? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    partial void OnSelectedReportWorkspaceChanged(
        SclReportWorkspaceProjection? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    private void RefreshEngineeringWorkspaces(
        SclDocumentState state,
        bool forceIedRefresh = false)
    {
        NetworkWorkspaceRows = SclNetworkWorkspaceProjector.Build(state);

        if (SelectedIedWorkspace is { } ied)
        {
            RefreshIedScopedWorkspaces(
                state,
                ied.Handle,
                forceIedRefresh);
        }
        else
        {
            ClearIedScopedWorkspaces();
        }
    }

    private void RefreshIedScopedWorkspaces(
        SclDocumentState state,
        SclNodeHandle iedHandle,
        bool force = false)
    {
        if (!force && _workspaceIedHandle == iedHandle)
        {
            return;
        }

        _workspaceIedHandle = iedHandle;

        var selectedDataSetHandle = SelectedDataSetWorkspace?.Handle;
        var selectedReportHandle = SelectedReportWorkspace?.Handle;

        var dataSets = SclDataSetWorkspaceProjector.BuildCatalog(
            state,
            iedHandle);

        var reports = SclReportWorkspaceProjector.Build(
            state,
            iedHandle);

        var wasSynchronizing = _synchronizingSelection;
        _synchronizingSelection = true;

        try
        {
            DataSetWorkspaceRows = dataSets;
            ReportWorkspaceRows = reports;

            OnPropertyChanged(nameof(DataSetWorkspaceHeader));
            OnPropertyChanged(nameof(ReportWorkspaceHeader));

            SelectedDataSetWorkspace = selectedDataSetHandle is { } dataSetHandle
                ? dataSets.FirstOrDefault(row => row.Handle == dataSetHandle)
                : dataSets.FirstOrDefault();

            SelectedReportWorkspace = selectedReportHandle is { } reportHandle
                ? reports.FirstOrDefault(row => row.Handle == reportHandle)
                : reports.FirstOrDefault();
        }
        finally
        {
            _synchronizingSelection = wasSynchronizing;
        }
    }

    private void ClearIedScopedWorkspaces()
    {
        _workspaceIedHandle = SclNodeHandle.None;
        DataSetWorkspaceRows = Array.Empty<SclDataSetWorkspaceProjection>();
        SelectedDataSetWorkspace = null;
        DataSetMemberRows = Array.Empty<SclDataSetMemberProjection>();
        SelectedDataSetMember = null;
        ReportWorkspaceRows = Array.Empty<SclReportWorkspaceProjection>();
        SelectedReportWorkspace = null;

        OnPropertyChanged(nameof(DataSetWorkspaceHeader));
        OnPropertyChanged(nameof(ReportWorkspaceHeader));
    }

    private void SynchronizeEngineeringWorkspaceSelection(
        SclDocumentState state,
        SclNodeHandle selected)
    {
        SelectedNetworkWorkspaceRow = NetworkWorkspaceRows.FirstOrDefault(
            row => row.Handle == selected);

        SclNodeHandle dataSetHandle = selected;

        if (state.SemanticIndex.TryGetNode(selected, out var selectedNode) &&
            selectedNode is not null &&
            selectedNode.Kind == SclSemanticKind.Fcda &&
            state.SemanticIndex.TryFindAncestor(
                selected,
                SclSemanticKind.DataSet,
                out var dataSetAncestor) &&
            dataSetAncestor is not null)
        {
            dataSetHandle = dataSetAncestor.Handle;
        }

        SelectedDataSetWorkspace = DataSetWorkspaceRows.FirstOrDefault(
            row => row.Handle == dataSetHandle);

        SelectedDataSetMember = DataSetMemberRows.FirstOrDefault(
            row => row.Handle == selected);

        SelectedReportWorkspace = ReportWorkspaceRows.FirstOrDefault(
            row => row.Handle == selected);
    }

    private void ClearEngineeringWorkspaces()
    {
        _workspaceIedHandle = SclNodeHandle.None;
        SelectedEngineeringWorkspaceIndex = 0;
        NetworkWorkspaceRows = Array.Empty<SclNetworkWorkspaceProjection>();
        SelectedNetworkWorkspaceRow = null;
        ClearIedScopedWorkspaces();
    }
}
