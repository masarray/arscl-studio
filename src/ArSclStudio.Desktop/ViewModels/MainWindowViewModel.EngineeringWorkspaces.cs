using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArSclStudio.Desktop.ViewModels;

public sealed partial class MainWindowViewModel
{
    private SclNodeHandle _workspaceIedHandle;
    private IReadOnlyList<SclNetworkWorkspaceProjection> _allNetworkWorkspaceRows =
        Array.Empty<SclNetworkWorkspaceProjection>();
    private readonly HashSet<string> _collapsedDataModelPaths =
        new(StringComparer.Ordinal);
    private bool _synchronizingEngineeringNavigation;
    private CancellationTokenSource? _engineeringDetailLoadCancellation;
    private Task _engineeringDetailLoadTask = Task.CompletedTask;
    private long _engineeringDetailLoadSequence;

    private static readonly string[] EngineeringWorkspaceNames =
    [
        "Devices",
        "Network",
        "GOOSE",
        "DataSets",
        "Reports",
        "MMS Data",
        "Settings"
    ];

    [ObservableProperty]
    private int _selectedEngineeringWorkspaceIndex;

    [ObservableProperty]
    private IReadOnlyList<EngineeringWorkspaceNavigationRow> _engineeringWorkspaceNavigationRows =
        Array.Empty<EngineeringWorkspaceNavigationRow>();

    [ObservableProperty]
    private EngineeringWorkspaceNavigationRow? _selectedEngineeringNavigationRow;

    [ObservableProperty]
    private IReadOnlyList<SclServiceCapabilityProjection> _serviceCapabilityRows =
        Array.Empty<SclServiceCapabilityProjection>();

    [ObservableProperty]
    private SclServiceCapabilityProjection? _selectedServiceCapability;

    [ObservableProperty]
    private IReadOnlyList<SclNetworkWorkspaceProjection> _stationNetworkWorkspaceRows =
        Array.Empty<SclNetworkWorkspaceProjection>();

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
    private IReadOnlyList<SclDataSetMemberProjection> _reportDataSetMemberRows =
        Array.Empty<SclDataSetMemberProjection>();

    [ObservableProperty]
    private SclDataSetMemberProjection? _selectedReportDataSetMember;

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
    private IReadOnlyList<DataModelDisplayRow> _dataModelDisplayRows =
        Array.Empty<DataModelDisplayRow>();

    [ObservableProperty]
    private DataModelDisplayRow? _selectedDataModelDisplayRow;

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

    [ObservableProperty]
    private bool _isEngineeringWorkspaceBusy;

    [ObservableProperty]
    private string _engineeringWorkspaceStatus = string.Empty;

    public Task WaitForEngineeringWorkspaceIdleAsync() =>
        _engineeringDetailLoadTask;

    public string SelectedEngineeringWorkspaceName =>
        SelectedEngineeringWorkspaceIndex >= 0 &&
        SelectedEngineeringWorkspaceIndex < EngineeringWorkspaceNames.Length
            ? EngineeringWorkspaceNames[SelectedEngineeringWorkspaceIndex]
            : "Overview";

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

    public bool HasSettingGroups =>
        SettingGroupControls.Count > 0;

    public bool HasNoSettingGroups =>
        !HasSettingGroups;

    partial void OnSettingGroupControlsChanged(
        IReadOnlyList<SclSettingGroupControlProjection> value)
    {
        OnPropertyChanged(nameof(HasSettingGroups));
        OnPropertyChanged(nameof(HasNoSettingGroups));
    }

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
        OnPropertyChanged(nameof(SelectedEngineeringWorkspaceName));
        RefreshEngineeringWorkspaceNavigationRows();

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
                var network =
                    SelectedNetworkWorkspaceRow ??
                    StationNetworkWorkspaceRows.FirstOrDefault();

                SelectedNetworkWorkspaceRow = network;

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

        QueueSelectedEngineeringWorkspaceDetails(value);
    }

    partial void OnSelectedEngineeringNavigationRowChanged(
        EngineeringWorkspaceNavigationRow? value)
    {
        if (_synchronizingEngineeringNavigation ||
            value is null)
        {
            return;
        }

        _synchronizingEngineeringNavigation = true;

        try
        {
            if (value.WorkspaceIndex != SelectedEngineeringWorkspaceIndex)
            {
                SelectedEngineeringWorkspaceIndex = value.WorkspaceIndex;
            }

            if (!value.Handle.IsNone)
            {
                _selectionService.Select(value.Handle);
            }
        }
        finally
        {
            _synchronizingEngineeringNavigation = false;
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
        GooseSignalRows = Array.Empty<SclGooseSignalProjection>();
        GooseSubscriberRows = Array.Empty<SclGooseSubscriberProjection>();

        if (SelectedEngineeringWorkspaceIndex == 2 &&
            value is not null &&
            !value.Handle.IsNone)
        {
            QueueGooseDetails(value.Handle);
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
        DataSetMemberRows = Array.Empty<SclDataSetMemberProjection>();
        OnPropertyChanged(nameof(DataSetWorkspaceHeader));

        if (SelectedEngineeringWorkspaceIndex == 3 &&
            value is not null &&
            !value.Handle.IsNone)
        {
            QueueDataSetDetails(value.Handle);
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
        ReportDataSetMemberRows = Array.Empty<SclDataSetMemberProjection>();
        SelectedReportDataSetMember = null;

        if (SelectedEngineeringWorkspaceIndex == 4 &&
            value is not null &&
            !value.Handle.IsNone)
        {
            QueueReportDetails(value.Handle);
        }

        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    partial void OnSelectedReportDataSetMemberChanged(
        SclDataSetMemberProjection? value)
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
        DataModelRows = Array.Empty<SclDataModelRowProjection>();
        DataModelDisplayRows = Array.Empty<DataModelDisplayRow>();
        _collapsedDataModelPaths.Clear();
        SelectedDataModelRow = null;
        SelectedDataModelDisplayRow = null;

        if (SelectedEngineeringWorkspaceIndex == 5 &&
            value is not null &&
            !value.Handle.IsNone)
        {
            QueueDataModelDetails(value.Handle);
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
        if (value is not null)
        {
            EnsureDataModelRowVisible(value.Path);
            SelectedDataModelDisplayRow =
                DataModelDisplayRows.FirstOrDefault(
                    row => row.Handle == value.Handle);
        }
        else
        {
            SelectedDataModelDisplayRow = null;
        }

        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    partial void OnSelectedDataModelDisplayRowChanged(
        DataModelDisplayRow? value)
    {
        if (value is not null &&
            (SelectedDataModelRow is null ||
             SelectedDataModelRow.Handle != value.Handle))
        {
            SelectedDataModelRow = value.Model;
        }
    }

    public void ToggleDataModelRow(DataModelDisplayRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!row.HasChildren)
        {
            return;
        }

        if (_collapsedDataModelPaths.Contains(row.Path))
        {
            _collapsedDataModelPaths.Remove(row.Path);
        }
        else
        {
            _collapsedDataModelPaths.Add(row.Path);

            if (SelectedDataModelRow is { } selected &&
                selected.Path.StartsWith(
                    string.Concat(row.Path, "/"),
                    StringComparison.Ordinal))
            {
                SelectedDataModelRow = row.Model;
            }
        }

        RefreshDataModelDisplayRows();
    }

    private void EnsureDataModelRowVisible(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            _collapsedDataModelPaths.Count == 0)
        {
            return;
        }

        var changed = _collapsedDataModelPaths.RemoveWhere(
            collapsed =>
                path.Length > collapsed.Length &&
                path.StartsWith(collapsed, StringComparison.Ordinal) &&
                path[collapsed.Length] == '/') > 0;

        if (changed)
        {
            RefreshDataModelDisplayRows();
        }
    }

    private void RefreshDataModelDisplayRows()
    {
        if (DataModelRows.Count == 0)
        {
            DataModelDisplayRows = Array.Empty<DataModelDisplayRow>();
            SelectedDataModelDisplayRow = null;
            return;
        }

        var visible = new List<DataModelDisplayRow>(DataModelRows.Count);
        var hiddenBelowDepth = -1;

        for (var i = 0; i < DataModelRows.Count; i++)
        {
            var row = DataModelRows[i];

            if (hiddenBelowDepth >= 0)
            {
                if (row.Depth > hiddenBelowDepth)
                {
                    continue;
                }

                hiddenBelowDepth = -1;
            }

            var hasChildren =
                i + 1 < DataModelRows.Count &&
                DataModelRows[i + 1].Depth > row.Depth;
            var isExpanded =
                hasChildren &&
                !_collapsedDataModelPaths.Contains(row.Path);

            visible.Add(
                new DataModelDisplayRow(
                    row,
                    hasChildren,
                    isExpanded));

            if (hasChildren && !isExpanded)
            {
                hiddenBelowDepth = row.Depth;
            }
        }

        DataModelDisplayRows = visible;

        if (SelectedDataModelRow is { } selected)
        {
            SelectedDataModelDisplayRow =
                visible.FirstOrDefault(
                    row => row.Handle == selected.Handle);
        }
    }

    partial void OnSelectedSettingGroupControlChanged(
        SclSettingGroupControlProjection? value)
    {
        SettingGroupSettings = Array.Empty<SclSettingGroupSettingProjection>();
        SelectedSettingGroupSetting = null;

        if (SelectedEngineeringWorkspaceIndex == 6 &&
            value is not null &&
            !value.Handle.IsNone)
        {
            QueueSettingGroupDetails(value.Handle);
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

    private void QueueSelectedEngineeringWorkspaceDetails(int workspaceIndex)
    {
        switch (workspaceIndex)
        {
            case 2 when SelectedGooseWorkspace is { } goose:
                QueueGooseDetails(goose.Handle);
                break;

            case 3 when SelectedDataSetWorkspace is { } dataSet:
                QueueDataSetDetails(dataSet.Handle);
                break;

            case 4 when SelectedReportWorkspace is { } report:
                QueueReportDetails(report.Handle);
                break;

            case 5 when SelectedDataModelLogicalNode is { } logicalNode:
                QueueDataModelDetails(logicalNode.Handle);
                break;

            case 6 when SelectedSettingGroupControl is { } settingControl:
                QueueSettingGroupDetails(settingControl.Handle);
                break;
        }
    }

    private void QueueGooseDetails(SclNodeHandle handle) =>
        QueueEngineeringDetailLoad(
            handle,
            "GOOSE signals",
            static (state, selected, _) =>
                new GooseDetailSnapshot(
                    SclGooseWorkspaceProjector.BuildSignals(state, selected),
                    SclGooseWorkspaceProjector.BuildSubscribers(state, selected)),
            snapshot =>
            {
                GooseSignalRows = snapshot.Signals;
                GooseSubscriberRows = snapshot.Subscribers;
            });

    private void QueueDataSetDetails(SclNodeHandle handle) =>
        QueueEngineeringDetailLoad(
            handle,
            "DataSet members",
            static (state, selected, _) =>
                SclDataSetWorkspaceProjector.BuildMembers(state, selected),
            rows => DataSetMemberRows = rows);

    private void QueueReportDetails(SclNodeHandle handle) =>
        QueueEngineeringDetailLoad(
            handle,
            "report DataSet members",
            static (state, selected, _) =>
                BuildReportDataSetMembers(state, selected),
            rows =>
            {
                ReportDataSetMemberRows = rows;
                SelectedReportDataSetMember = null;
            });

    private void QueueDataModelDetails(SclNodeHandle handle) =>
        QueueEngineeringDetailLoad(
            handle,
            "MMS data model",
            static (state, selected, _) =>
                SclDataModelWorkspaceProjector.BuildRows(state, selected),
            rows =>
            {
                DataModelRows = rows;
                _collapsedDataModelPaths.Clear();
                SelectedDataModelRow = null;
                SelectedDataModelDisplayRow = null;
                RefreshDataModelDisplayRows();
            });

    private void QueueSettingGroupDetails(SclNodeHandle handle) =>
        QueueEngineeringDetailLoad(
            handle,
            "setting values",
            static (state, selected, token) =>
                SclSettingGroupWorkspaceProjector.BuildSettings(
                    state,
                    selected,
                    token),
            rows =>
            {
                SettingGroupSettings = rows;
                SelectedSettingGroupSetting = null;
            });

    private void QueueEngineeringDetailLoad<T>(
        SclNodeHandle handle,
        string description,
        Func<SclDocumentState, SclNodeHandle, CancellationToken, T> build,
        Action<T> publish)
        where T : class
    {
        if (handle.IsNone ||
            _session.CurrentState is not { } state ||
            Volatile.Read(ref _disposeStarted) != 0)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        var previous = Interlocked.Exchange(
            ref _engineeringDetailLoadCancellation,
            cancellation);

        previous?.Cancel();

        var sequence = Interlocked.Increment(
            ref _engineeringDetailLoadSequence);

        var task = RunEngineeringDetailLoadAsync(
            state,
            handle,
            description,
            sequence,
            cancellation,
            build,
            publish);

        _engineeringDetailLoadTask = task;
    }

    private async Task RunEngineeringDetailLoadAsync<T>(
        SclDocumentState state,
        SclNodeHandle handle,
        string description,
        long sequence,
        CancellationTokenSource cancellation,
        Func<SclDocumentState, SclNodeHandle, CancellationToken, T> build,
        Action<T> publish)
        where T : class
    {
        if (sequence == Volatile.Read(ref _engineeringDetailLoadSequence))
        {
            IsEngineeringWorkspaceBusy = true;
            EngineeringWorkspaceStatus = $"Loading {description}...";
        }

        try
        {
            var result = await _session.RunLatestAsync(
                WorkKind.EngineeringDetails,
                token => Task.Run(
                    () => build(state, handle, token),
                    token),
                cancellation.Token);

            if (!result.CanPublish ||
                result.Value is null ||
                result.SourceRevision != state.Revision ||
                result.SourceRevision != _session.CurrentRevision ||
                sequence != Volatile.Read(ref _engineeringDetailLoadSequence) ||
                Volatile.Read(ref _disposeStarted) != 0)
            {
                return;
            }

            publish(result.Value);
            EngineeringWorkspaceStatus = string.Empty;
        }
        catch (OperationCanceledException)
        {
            // Latest-wins navigation or window disposal.
        }
        catch (Exception exception)
        {
            if (sequence == Volatile.Read(ref _engineeringDetailLoadSequence))
            {
                EngineeringWorkspaceStatus =
                    $"Unable to load {description}: {exception.Message}";
                StatusText =
                    $"Workspace detail failed safely — {exception.GetType().Name}";
            }
        }
        finally
        {
            if (sequence == Volatile.Read(ref _engineeringDetailLoadSequence))
            {
                IsEngineeringWorkspaceBusy = false;
            }

            Interlocked.CompareExchange(
                ref _engineeringDetailLoadCancellation,
                null,
                cancellation);

            cancellation.Dispose();
        }
    }

    private void RefreshEngineeringWorkspaces(
        SclDocumentState state,
        bool forceIedRefresh = false)
    {
        _allNetworkWorkspaceRows = SclNetworkWorkspaceProjector.Build(state);
        StationNetworkWorkspaceRows = _allNetworkWorkspaceRows;

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

        var selectedNetworkHandle = SelectedNetworkWorkspaceRow?.Handle;
        var selectedServiceHandle = SelectedServiceCapability?.Handle;
        var selectedGooseHandle = SelectedGooseWorkspace?.Handle;
        var selectedDataSetHandle = SelectedDataSetWorkspace?.Handle;
        var selectedReportHandle = SelectedReportWorkspace?.Handle;
        var selectedDataModelNodeHandle =
            SelectedDataModelLogicalNode?.Handle;
        var selectedSettingControlHandle =
            SelectedSettingGroupControl?.Handle;

        var iedName = IedWorkspaceRows.FirstOrDefault(
            row => row.Handle == iedHandle)?.Name;

        var networkRows = string.IsNullOrWhiteSpace(iedName)
            ? Array.Empty<SclNetworkWorkspaceProjection>()
            : _allNetworkWorkspaceRows
                .Where(row =>
                    string.Equals(
                        row.IedName,
                        iedName,
                        StringComparison.Ordinal))
                .ToArray();

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
            NetworkWorkspaceRows = networkRows;
            SelectedNetworkWorkspaceRow =
                selectedNetworkHandle is { } networkHandle
                    ? networkRows.FirstOrDefault(
                        row => row.Handle == networkHandle) ??
                      networkRows.FirstOrDefault()
                    : networkRows.FirstOrDefault();

            ServiceCapabilityRows = serviceCapabilities;
            GooseWorkspaceRows = gooseControls;
            DataSetWorkspaceRows = dataSets;
            ReportWorkspaceRows = reports;
            DataModelLogicalNodes = dataModelLogicalNodes;
            SettingGroupControls = settingControls;

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

            RefreshEngineeringWorkspaceNavigationRows();
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
        ReportDataSetMemberRows = Array.Empty<SclDataSetMemberProjection>();
        SelectedReportDataSetMember = null;
        DataModelLogicalNodes = Array.Empty<SclDataModelLogicalNodeProjection>();
        SelectedDataModelLogicalNode = null;
        DataModelRows = Array.Empty<SclDataModelRowProjection>();
        SelectedDataModelRow = null;
        DataModelDisplayRows = Array.Empty<DataModelDisplayRow>();
        SelectedDataModelDisplayRow = null;
        _collapsedDataModelPaths.Clear();
        SettingGroupControls = Array.Empty<SclSettingGroupControlProjection>();
        SelectedSettingGroupControl = null;
        SettingGroupSettings = Array.Empty<SclSettingGroupSettingProjection>();
        SelectedSettingGroupSetting = null;

        OnPropertyChanged(nameof(ServicesWorkspaceHeader));
        OnPropertyChanged(nameof(GooseWorkspaceHeader));
        OnPropertyChanged(nameof(DataSetWorkspaceHeader));
        OnPropertyChanged(nameof(ReportWorkspaceHeader));
        OnPropertyChanged(nameof(DataModelWorkspaceHeader));
        OnPropertyChanged(nameof(SettingGroupsWorkspaceHeader));
        RefreshEngineeringWorkspaceNavigationRows();
    }

    private void RefreshEngineeringWorkspaceNavigationRows()
    {
        var rows = new List<EngineeringWorkspaceNavigationRow>();

        switch (SelectedEngineeringWorkspaceIndex)
        {
            case 0:
                AddIedNavigationRows(rows, 0);
                break;

            case 1:
                rows.Capacity = StationNetworkWorkspaceRows.Count;

                foreach (var endpoint in StationNetworkWorkspaceRows)
                {
                    rows.Add(new EngineeringWorkspaceNavigationRow(
                        1,
                        $"{endpoint.IedName} / {endpoint.AccessPoint}",
                        endpoint.IpAddress,
                        "AP",
                        0,
                        false,
                        endpoint.Handle));
                }
                break;

            case 2:
                AddIedNavigationRows(
                    rows,
                    2,
                    static (target, vm) =>
                    {
                        foreach (var control in vm.GooseWorkspaceRows)
                        {
                            target.Add(new EngineeringWorkspaceNavigationRow(
                                2,
                                $"{control.LogicalDevice} / {control.Name}",
                                string.IsNullOrWhiteSpace(control.DataSet)
                                    ? control.ServiceType
                                    : $"{control.ServiceType} · {control.DataSet}",
                                "G",
                                1,
                                false,
                                control.Handle));
                        }
                    });
                break;

            case 3:
                AddIedNavigationRows(
                    rows,
                    3,
                    static (target, vm) =>
                    {
                        foreach (var dataSet in vm.DataSetWorkspaceRows)
                        {
                            target.Add(new EngineeringWorkspaceNavigationRow(
                                3,
                                $"{dataSet.LogicalDevice} / {dataSet.Name}",
                                $"{dataSet.MemberCount} members · {dataSet.UsedByCount} used",
                                "DS",
                                1,
                                false,
                                dataSet.Handle));
                        }
                    });
                break;

            case 4:
                AddIedNavigationRows(rows, 4);
                break;

            case 5:
                AddIedNavigationRows(
                    rows,
                    5,
                    static (target, vm) =>
                    {
                        foreach (var logicalNode in vm.DataModelLogicalNodes)
                        {
                            target.Add(new EngineeringWorkspaceNavigationRow(
                                5,
                                $"{logicalNode.LogicalDevice} / {logicalNode.LogicalNode}",
                                $"{logicalNode.LnClass} · {logicalNode.DataObjectCount} DO",
                                "LN",
                                1,
                                false,
                                logicalNode.Handle));
                        }
                    });
                break;

            case 6:
                AddIedNavigationRows(
                    rows,
                    6,
                    static (target, vm) =>
                    {
                        foreach (var settingControl in vm.SettingGroupControls)
                        {
                            target.Add(new EngineeringWorkspaceNavigationRow(
                                6,
                                $"{settingControl.LogicalDevice} / {settingControl.LogicalNode}",
                                $"{settingControl.NumberOfGroups} groups · active {settingControl.ActiveGroup}",
                                "SG",
                                1,
                                false,
                                settingControl.Handle));
                        }
                    });
                break;
        }

        EngineeringWorkspaceNavigationRows = rows;
        SynchronizeEngineeringNavigationSelection(
            _selectionService.SelectedNode);
    }

    private void AddIedNavigationRows(
        List<EngineeringWorkspaceNavigationRow> rows,
        int workspaceIndex,
        Action<List<EngineeringWorkspaceNavigationRow>, MainWindowViewModel>? addSelectedChildren = null)
    {
        for (var i = 0; i < IedWorkspaceRows.Count; i++)
        {
            var ied = IedWorkspaceRows[i];

            rows.Add(new EngineeringWorkspaceNavigationRow(
                workspaceIndex,
                ied.Name,
                string.IsNullOrWhiteSpace(ied.Manufacturer)
                    ? $"LD {ied.LogicalDeviceCount} · LN {ied.LogicalNodeCount}"
                    : ied.Manufacturer,
                "IED",
                0,
                false,
                ied.Handle));

            if (SelectedIedWorkspace?.Handle == ied.Handle)
            {
                addSelectedChildren?.Invoke(rows, this);
            }
        }
    }

    private void SynchronizeEngineeringNavigationSelection(
        SclNodeHandle selected)
    {
        var targetHandle = SelectedEngineeringWorkspaceIndex switch
        {
            0 => SelectedIedWorkspace?.Handle ?? selected,
            1 => SelectedNetworkWorkspaceRow?.Handle ?? selected,
            2 => SelectedGooseWorkspace?.Handle ??
                 SelectedIedWorkspace?.Handle ??
                 selected,
            3 => SelectedDataSetWorkspace?.Handle ??
                 SelectedIedWorkspace?.Handle ??
                 selected,
            4 => SelectedIedWorkspace?.Handle ?? selected,
            5 => SelectedDataModelLogicalNode?.Handle ??
                 SelectedIedWorkspace?.Handle ??
                 selected,
            6 => SelectedSettingGroupControl?.Handle ??
                 SelectedIedWorkspace?.Handle ??
                 selected,
            _ => selected
        };

        var next = EngineeringWorkspaceNavigationRows.FirstOrDefault(
            row =>
                row.WorkspaceIndex == SelectedEngineeringWorkspaceIndex &&
                !row.IsWorkspace &&
                row.Handle == targetHandle);

        next ??= EngineeringWorkspaceNavigationRows.FirstOrDefault();

        if (ReferenceEquals(next, SelectedEngineeringNavigationRow) ||
            next == SelectedEngineeringNavigationRow)
        {
            return;
        }

        _synchronizingEngineeringNavigation = true;

        try
        {
            SelectedEngineeringNavigationRow = next;
        }
        finally
        {
            _synchronizingEngineeringNavigation = false;
        }
    }

    private sealed record GooseDetailSnapshot(
        SclGooseSignalProjection[] Signals,
        SclGooseSubscriberProjection[] Subscribers);

    private static SclDataSetMemberProjection[] BuildReportDataSetMembers(
        SclDocumentState state,
        SclNodeHandle reportHandle)
    {
        var outgoing = state.SemanticIndex.References.GetOutgoing(reportHandle);
        SclNodeHandle dataSetHandle = SclNodeHandle.None;

        for (var i = 0; i < outgoing.Count; i++)
        {
            if (outgoing[i].Kind != SclReferenceKind.DataSetBinding)
            {
                continue;
            }

            if (!dataSetHandle.IsNone)
            {
                return [];
            }

            dataSetHandle = outgoing[i].Target;
        }

        return dataSetHandle.IsNone
            ? []
            : SclDataSetWorkspaceProjector.BuildMembers(state, dataSetHandle);
    }

    private void ActivateEngineeringWorkspaceForExternalNavigation(
        SclNodeHandle selected)
    {
        if (selected.IsNone ||
            _session.CurrentState is not { } state)
        {
            return;
        }

        var nextIndex = ResolveEngineeringWorkspaceIndex(state, selected);

        if (nextIndex < 0 ||
            nextIndex == SelectedEngineeringWorkspaceIndex)
        {
            return;
        }

        var wasSynchronizing = _synchronizingSelection;
        _synchronizingSelection = true;

        try
        {
            SelectedEngineeringWorkspaceIndex = nextIndex;
            RefreshEngineeringWorkspaceNavigationRows();
        }
        finally
        {
            _synchronizingSelection = wasSynchronizing;
        }
    }

    private int ResolveEngineeringWorkspaceIndex(
        SclDocumentState state,
        SclNodeHandle selected)
    {
        if (SettingGroupSettings.Any(row => row.Handle == selected))
        {
            return 6;
        }

        if (!state.SemanticIndex.TryGetNode(selected, out var node) ||
            node is null)
        {
            return -1;
        }

        return node.Kind switch
        {
            SclSemanticKind.Ied or
            SclSemanticKind.Services => 0,

            SclSemanticKind.Communication or
            SclSemanticKind.SubNetwork or
            SclSemanticKind.ConnectedAccessPoint or
            SclSemanticKind.GseCommunication or
            SclSemanticKind.SmvCommunication or
            SclSemanticKind.Address => 1,

            SclSemanticKind.GseControl or
            SclSemanticKind.ExternalReference => 2,

            SclSemanticKind.DataSet or
            SclSemanticKind.Fcda => 3,

            SclSemanticKind.ReportControl or
            SclSemanticKind.LogControl => 4,

            SclSemanticKind.LogicalNodeZero or
            SclSemanticKind.LogicalNode or
            SclSemanticKind.Doi or
            SclSemanticKind.Sdi or
            SclSemanticKind.Dai or
            SclSemanticKind.DataObjectDefinition or
            SclSemanticKind.SubDataObjectDefinition or
            SclSemanticKind.DataAttributeDefinition or
            SclSemanticKind.BasicDataAttributeDefinition => 5,

            SclSemanticKind.SettingGroupControl => 6,
            _ => -1
        };
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

                    SelectedReportDataSetMember =
                        ReportDataSetMemberRows.FirstOrDefault(
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
        _allNetworkWorkspaceRows = Array.Empty<SclNetworkWorkspaceProjection>();
        StationNetworkWorkspaceRows = Array.Empty<SclNetworkWorkspaceProjection>();
        SelectedEngineeringWorkspaceIndex = 0;
        NetworkWorkspaceRows = Array.Empty<SclNetworkWorkspaceProjection>();
        SelectedNetworkWorkspaceRow = null;
        ClearIedScopedWorkspaces();
    }
}
