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
    private IReadOnlyList<SclGooseWorkspaceProjection> _gooseWorkspaceRows =
        Array.Empty<SclGooseWorkspaceProjection>();

    [ObservableProperty]
    private SclGooseWorkspaceProjection? _selectedGooseWorkspace;

    [ObservableProperty]
    private IReadOnlyList<SclGooseSignalProjection> _gooseSignalRows =
        Array.Empty<SclGooseSignalProjection>();

    [ObservableProperty]
    private SclGooseSignalProjection? _selectedGooseSignal;

    [ObservableProperty]
    private IReadOnlyList<SclGooseSubscriberProjection> _gooseSubscriberRows =
        Array.Empty<SclGooseSubscriberProjection>();

    [ObservableProperty]
    private SclGooseSubscriberProjection? _selectedGooseSubscriber;

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

    public string GooseWorkspaceHeader =>
        GooseWorkspaceRows.Count == 0
            ? "GOOSE"
            : $"GOOSE ({GooseWorkspaceRows.Count})";

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

    partial void OnSelectedEngineeringWorkspaceIndexChanged(int value)
    {
        if (_synchronizingSelection ||
            _session.CurrentState is null)
        {
            return;
        }

        switch (value)
        {
            case 0:
                if (SelectedIedWorkspace is { } ied)
                {
                    _selectionService.Select(ied.Handle);
                }
                break;

            case 1:
                var network = SelectedNetworkWorkspaceRow;

                if (SelectedIedWorkspace is { } selectedIed &&
                    (network is null ||
                     !string.Equals(
                         network.IedName,
                         selectedIed.Name,
                         StringComparison.Ordinal)))
                {
                    network = NetworkWorkspaceRows.FirstOrDefault(row =>
                        string.Equals(
                            row.IedName,
                            selectedIed.Name,
                            StringComparison.Ordinal));
                    SelectedNetworkWorkspaceRow = network;
                }

                if (network is not null)
                {
                    _selectionService.Select(network.Handle);
                }
                break;

            case 2:
                if (SelectedGooseWorkspace is { } goose)
                {
                    _selectionService.Select(goose.Handle);
                }
                break;

            case 3:
                if (SelectedDataSetWorkspace is { } dataSet)
                {
                    _selectionService.Select(dataSet.Handle);
                }
                break;

            case 4:
                if (SelectedReportWorkspace is { } report)
                {
                    _selectionService.Select(report.Handle);
                }
                break;
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

    partial void OnSelectedGooseWorkspaceChanged(
        SclGooseWorkspaceProjection? value)
    {
        if (_session.CurrentState is { } state)
        {
            GooseSignalRows = value is null
                ? Array.Empty<SclGooseSignalProjection>()
                : SclGooseWorkspaceProjector.BuildSignals(
                    state,
                    value.Handle);

            GooseSubscriberRows = value is null
                ? Array.Empty<SclGooseSubscriberProjection>()
                : SclGooseWorkspaceProjector.BuildSubscribers(
                    state,
                    value.Handle);
        }

        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    partial void OnSelectedGooseSignalChanged(
        SclGooseSignalProjection? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    partial void OnSelectedGooseSubscriberChanged(
        SclGooseSubscriberProjection? value)
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

            OnPropertyChanged(nameof(GooseWorkspaceHeader));
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

        var selectedGooseHandle = SelectedGooseWorkspace?.Handle;
        var selectedDataSetHandle = SelectedDataSetWorkspace?.Handle;
        var selectedReportHandle = SelectedReportWorkspace?.Handle;

        var gooseControls = SclGooseWorkspaceProjector.BuildCatalog(
            state,
            iedHandle);

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
            GooseWorkspaceRows = gooseControls;
            DataSetWorkspaceRows = dataSets;
            ReportWorkspaceRows = reports;

            OnPropertyChanged(nameof(GooseWorkspaceHeader));
            OnPropertyChanged(nameof(DataSetWorkspaceHeader));
            OnPropertyChanged(nameof(ReportWorkspaceHeader));

            var nextGoose = selectedGooseHandle is { } gooseHandle
                ? gooseControls.FirstOrDefault(row => row.Handle == gooseHandle)
                : null;

            SelectedGooseWorkspace =
                nextGoose ?? gooseControls.FirstOrDefault();

            var nextDataSet = selectedDataSetHandle is { } dataSetHandle
                ? dataSets.FirstOrDefault(row => row.Handle == dataSetHandle)
                : null;

            SelectedDataSetWorkspace =
                nextDataSet ?? dataSets.FirstOrDefault();

            var nextReport = selectedReportHandle is { } reportHandle
                ? reports.FirstOrDefault(row => row.Handle == reportHandle)
                : null;

            SelectedReportWorkspace =
                nextReport ?? reports.FirstOrDefault();
        }
        finally
        {
            _synchronizingSelection = wasSynchronizing;
        }
    }

    private void ClearIedScopedWorkspaces()
    {
        _workspaceIedHandle = SclNodeHandle.None;
        GooseWorkspaceRows = Array.Empty<SclGooseWorkspaceProjection>();
        SelectedGooseWorkspace = null;
        GooseSignalRows = Array.Empty<SclGooseSignalProjection>();
        SelectedGooseSignal = null;
        GooseSubscriberRows = Array.Empty<SclGooseSubscriberProjection>();
        SelectedGooseSubscriber = null;
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
        if (!state.SemanticIndex.TryGetNode(
                selected,
                out var selectedNode) ||
            selectedNode is null)
        {
            return;
        }

        switch (selectedNode.Kind)
        {
            case SclSemanticKind.ConnectedAccessPoint:
                SelectedNetworkWorkspaceRow = NetworkWorkspaceRows.FirstOrDefault(
                    row => row.Handle == selected);
                break;

            case SclSemanticKind.GseControl:
                SelectedGooseWorkspace = GooseWorkspaceRows.FirstOrDefault(
                    row => row.Handle == selected);
                SelectedGooseSignal = null;
                SelectedGooseSubscriber = null;
                break;

            case SclSemanticKind.DataSet:
                SelectedDataSetWorkspace = DataSetWorkspaceRows.FirstOrDefault(
                    row => row.Handle == selected);
                SelectedDataSetMember = null;
                break;

            case SclSemanticKind.Fcda:
                if (state.SemanticIndex.TryFindAncestor(
                        selected,
                        SclSemanticKind.DataSet,
                        out var dataSetAncestor) &&
                    dataSetAncestor is not null)
                {
                    SelectedDataSetWorkspace = DataSetWorkspaceRows.FirstOrDefault(
                        row => row.Handle == dataSetAncestor.Handle);

                    SelectedDataSetMember = DataSetMemberRows.FirstOrDefault(
                        row => row.Handle == selected);

                    SelectedGooseSignal = GooseSignalRows.FirstOrDefault(
                        row => row.Handle == selected);
                }
                break;

            case SclSemanticKind.ExternalReference:
                SelectedGooseSubscriber = GooseSubscriberRows.FirstOrDefault(
                    row => row.Handle == selected);
                break;

            case SclSemanticKind.ReportControl:
            case SclSemanticKind.LogControl:
                SelectedReportWorkspace = ReportWorkspaceRows.FirstOrDefault(
                    row => row.Handle == selected);
                break;
        }
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
