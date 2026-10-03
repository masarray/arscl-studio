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
    private IReadOnlyList<SclServiceCapabilityProjection> _serviceCapabilityRows =
        Array.Empty<SclServiceCapabilityProjection>();

    [ObservableProperty]
    private SclServiceCapabilityProjection? _selectedServiceCapability;

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

    [ObservableProperty]
    private IReadOnlyList<SclDataModelLogicalNodeProjection> _dataModelLogicalNodes =
        Array.Empty<SclDataModelLogicalNodeProjection>();

    [ObservableProperty]
    private SclDataModelLogicalNodeProjection? _selectedDataModelLogicalNode;

    [ObservableProperty]
    private IReadOnlyList<SclDataModelRowProjection> _dataModelRows =
        Array.Empty<SclDataModelRowProjection>();

    [ObservableProperty]
    private SclDataModelRowProjection? _selectedDataModelRow;

    [ObservableProperty]
    private IReadOnlyList<SclSettingGroupControlProjection> _settingGroupControls =
        Array.Empty<SclSettingGroupControlProjection>();

    [ObservableProperty]
    private SclSettingGroupControlProjection? _selectedSettingGroupControl;

    [ObservableProperty]
    private IReadOnlyList<SclSettingGroupSettingProjection> _settingGroupSettings =
        Array.Empty<SclSettingGroupSettingProjection>();

    [ObservableProperty]
    private SclSettingGroupSettingProjection? _selectedSettingGroupSetting;

    public string ServicesWorkspaceHeader =>
        ServiceCapabilityRows.Count == 0
            ? "Declared Services"
            : $"Declared Services ({ServiceCapabilityRows.Count})";

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

    public string DataModelWorkspaceHeader =>
        DataModelLogicalNodes.Count == 0
            ? "Data Model"
            : $"Data Model ({DataModelLogicalNodes.Count} LN)";

    public string SettingGroupsWorkspaceHeader =>
        SettingGroupControls.Count == 0
            ? "Setting Groups"
            : $"Setting Groups ({SettingGroupControls.Count})";

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

            case 5:
                if (SelectedDataModelLogicalNode is { } logicalNode)
                {
                    _selectionService.Select(logicalNode.Handle);
                }
                break;

            case 6:
                if (SelectedSettingGroupControl is { } settingControl)
                {
                    _selectionService.Select(settingControl.Handle);
                }
                break;
        }
    }

    partial void OnSelectedServiceCapabilityChanged(
        SclServiceCapabilityProjection? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
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

    partial void OnSelectedDataModelLogicalNodeChanged(
        SclDataModelLogicalNodeProjection? value)
    {
        if (_session.CurrentState is { } state)
        {
            DataModelRows = value is null
                ? Array.Empty<SclDataModelRowProjection>()
                : SclDataModelWorkspaceProjector.BuildRows(
                    state,
                    value.Handle);

            SelectedDataModelRow = null;
        }

        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    partial void OnSelectedDataModelRowChanged(
        SclDataModelRowProjection? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    partial void OnSelectedSettingGroupControlChanged(
        SclSettingGroupControlProjection? value)
    {
        if (_session.CurrentState is { } state)
        {
            SettingGroupSettings = value is null
                ? Array.Empty<SclSettingGroupSettingProjection>()
                : SclSettingGroupWorkspaceProjector.BuildSettings(
                    state,
                    value.Handle);

            SelectedSettingGroupSetting = null;
        }

        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    partial void OnSelectedSettingGroupSettingChanged(
        SclSettingGroupSettingProjection? value)
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

        var selectedServiceHandle = SelectedServiceCapability?.Handle;
        var selectedGooseHandle = SelectedGooseWorkspace?.Handle;
        var selectedDataSetHandle = SelectedDataSetWorkspace?.Handle;
        var selectedReportHandle = SelectedReportWorkspace?.Handle;
        var selectedDataModelNodeHandle =
            SelectedDataModelLogicalNode?.Handle;
        var selectedSettingControlHandle =
            SelectedSettingGroupControl?.Handle;

        var serviceCapabilities =
            SclServicesWorkspaceProjector.Build(
                state,
                iedHandle);

        var gooseControls = SclGooseWorkspaceProjector.BuildCatalog(
            state,
            iedHandle);

        var dataSets = SclDataSetWorkspaceProjector.BuildCatalog(
            state,
            iedHandle);

        var reports = SclReportWorkspaceProjector.Build(
            state,
            iedHandle);

        var dataModelLogicalNodes =
            SclDataModelWorkspaceProjector.BuildLogicalNodes(
                state,
                iedHandle);

        var settingControls =
            SclSettingGroupWorkspaceProjector.BuildControls(
                state,
                iedHandle);

        var wasSynchronizing = _synchronizingSelection;
        _synchronizingSelection = true;

        try
        {
            ServiceCapabilityRows = serviceCapabilities;
            GooseWorkspaceRows = gooseControls;
            DataSetWorkspaceRows = dataSets;
            ReportWorkspaceRows = reports;
            DataModelLogicalNodes = dataModelLogicalNodes;
            SettingGroupControls = settingControls;

            OnPropertyChanged(nameof(ServicesWorkspaceHeader));
            OnPropertyChanged(nameof(ServicesWorkspaceHeader));
        OnPropertyChanged(nameof(GooseWorkspaceHeader));
            OnPropertyChanged(nameof(DataSetWorkspaceHeader));
            OnPropertyChanged(nameof(ReportWorkspaceHeader));
            OnPropertyChanged(nameof(DataModelWorkspaceHeader));
            OnPropertyChanged(nameof(SettingGroupsWorkspaceHeader));

            SelectedServiceCapability =
                selectedServiceHandle is { } serviceHandle
                    ? serviceCapabilities.FirstOrDefault(
                        row => row.Handle == serviceHandle)
                    : null;

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

            var nextDataModelNode =
                selectedDataModelNodeHandle is { } dataModelNodeHandle
                    ? dataModelLogicalNodes.FirstOrDefault(
                        row => row.Handle == dataModelNodeHandle)
                    : null;

            SelectedDataModelLogicalNode =
                nextDataModelNode ??
                dataModelLogicalNodes.FirstOrDefault();

            var nextSettingControl =
                selectedSettingControlHandle is { } settingControlHandle
                    ? settingControls.FirstOrDefault(
                        row => row.Handle == settingControlHandle)
                    : null;

            SelectedSettingGroupControl =
                nextSettingControl ??
                settingControls.FirstOrDefault();
        }
        finally
        {
            _synchronizingSelection = wasSynchronizing;
        }
    }

    private void ClearIedScopedWorkspaces()
    {
        _workspaceIedHandle = SclNodeHandle.None;
        ServiceCapabilityRows = Array.Empty<SclServiceCapabilityProjection>();
        SelectedServiceCapability = null;
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
        DataModelLogicalNodes = Array.Empty<SclDataModelLogicalNodeProjection>();
        SelectedDataModelLogicalNode = null;
        DataModelRows = Array.Empty<SclDataModelRowProjection>();
        SelectedDataModelRow = null;
        SettingGroupControls = Array.Empty<SclSettingGroupControlProjection>();
        SelectedSettingGroupControl = null;
        SettingGroupSettings = Array.Empty<SclSettingGroupSettingProjection>();
        SelectedSettingGroupSetting = null;

        OnPropertyChanged(nameof(GooseWorkspaceHeader));
        OnPropertyChanged(nameof(DataSetWorkspaceHeader));
        OnPropertyChanged(nameof(ReportWorkspaceHeader));
        OnPropertyChanged(nameof(DataModelWorkspaceHeader));
        OnPropertyChanged(nameof(SettingGroupsWorkspaceHeader));
    }

    private void SynchronizeEngineeringWorkspaceSelection(
        SclDocumentState state,
        SclNodeHandle selected)
    {
        var selectedService =
            ServiceCapabilityRows.FirstOrDefault(
                row => row.Handle == selected);

        if (selectedService is not null)
        {
            SelectedServiceCapability = selectedService;
        }

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

            case SclSemanticKind.LogicalNodeZero:
            case SclSemanticKind.LogicalNode:
                SelectedDataModelLogicalNode =
                    DataModelLogicalNodes.FirstOrDefault(
                        row => row.Handle == selected);
                SelectedDataModelRow = null;
                break;

            case SclSemanticKind.SettingGroupControl:
                SelectedSettingGroupControl =
                    SettingGroupControls.FirstOrDefault(
                        row => row.Handle == selected);
                SelectedSettingGroupSetting = null;
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

            case SclSemanticKind.Doi:
            case SclSemanticKind.Sdi:
            case SclSemanticKind.Dai:
            case SclSemanticKind.DataObjectDefinition:
            case SclSemanticKind.SubDataObjectDefinition:
            case SclSemanticKind.DataAttributeDefinition:
            case SclSemanticKind.BasicDataAttributeDefinition:
                SelectedDataModelRow = DataModelRows.FirstOrDefault(
                    row => row.Handle == selected);
                SelectedSettingGroupSetting =
                    SettingGroupSettings.FirstOrDefault(
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
