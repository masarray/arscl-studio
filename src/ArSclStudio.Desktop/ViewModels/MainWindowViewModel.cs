using ArSclStudio.Engine.Diagnostics;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Editing;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Engine.Search;
using ArSclStudio.Engine.Workers;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArSclStudio.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IAsyncDisposable
{
    private readonly SclDocumentSession _session;
    private readonly SclSelectionService _selectionService;
    private readonly Dictionary<SclNodeHandle, ExplorerRow> _engineeringIndex = [];
    private readonly Dictionary<SclNodeHandle, ExplorerRow> _xmlIndex = [];
    private readonly HashSet<SclNodeHandle> _engineeringExpanded = [];
    private readonly HashSet<SclNodeHandle> _xmlExpanded = [];
    private ProblemRow[] _allProblems = [];

    private CancellationTokenSource? _searchDebounce;
    private bool _synchronizingSelection;
    private int _disposeStarted;
    private SclNodeHandle _editTarget;
    private DocumentRevision _editRevision;
    private string? _editOriginal;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _descriptionDraft = string.Empty;

    [ObservableProperty]
    private bool _removeDescription;

    [ObservableProperty]
    private string _editingTargetName = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChangeRow> _changeRows = Array.Empty<ChangeRow>();

    public bool HasUnsavedChanges => IsDirty || (IsEditing &&
        (RemoveDescription ? null : DescriptionDraft) != _editOriginal);
    public bool CanOpen => !IsBusy;
    public bool CanSave => !IsBusy && !IsEditing && _session.CurrentState is not null;
    public bool CanUndo => !IsBusy && !IsEditing && _session.CanUndo;
    public bool CanRedo => !IsBusy && !IsEditing && _session.CanRedo;
    public bool CanEdit => !IsBusy && !IsEditing && _session.CurrentState is { } state &&
        SclEditPolicy.CanEditDescription(state, _selectionService.SelectedNode);
    public bool CanApply => !IsBusy && IsEditing;
    public string SaveStateText => IsDirty ? "Unsaved changes" : "Saved";
    public string CurrentFileName => _session.CurrentState?.DisplayName ?? "Station.scd";

    partial void OnDescriptionDraftChanged(string value)
    {
        if (IsEditing) { RemoveDescription = false; }
    }

    partial void OnIsBusyChanged(bool value) => RefreshEditCommands();
    partial void OnIsEditingChanged(bool value) => RefreshEditCommands();
    partial void OnIsDirtyChanged(bool value) => OnPropertyChanged(nameof(SaveStateText));

    private void RefreshEditCommands()
    {
        OnPropertyChanged(nameof(CanOpen));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanApply));
    }

    public void BeginDescriptionEdit()
    {
        if (!CanEdit || _session.CurrentState is not { } state) { return; }
        _editTarget = _selectionService.SelectedNode;
        _editRevision = state.Revision;
        state.Syntax.TryGetAttributeValue(_editTarget, "desc", out _editOriginal);
        DescriptionDraft = _editOriginal ?? string.Empty;
        RemoveDescription = _editOriginal is null;
        EditingTargetName = DetailTitle;
        IsEditing = true;
    }

    public void CancelDescriptionEdit() => IsEditing = false;

    public async Task ApplyDescriptionAsync()
    {
        if (!CanApply) { return; }
        IsBusy = true;
        try
        {
            var result = await _session.ExecuteAsync(new SetIedDescriptionCommand(_editTarget,
                _editOriginal, RemoveDescription ? null : DescriptionDraft), _editRevision);
            StatusText = result.Message;
            if (result.Succeeded)
            {
                IsEditing = false;
                RefreshAfterOperation();
                await RefreshValidationAsync();
            }
        }
        finally { IsBusy = false; }
    }

    public async Task UndoAsync()
    {
        if (!CanUndo) { return; }
        IsBusy = true;
        try
        {
            var result = await _session.UndoAsync(_session.CurrentRevision);
            StatusText = result.Message;
            if (result.Succeeded)
            {
                RefreshAfterOperation();
                await RefreshValidationAsync();
            }
        }
        finally { IsBusy = false; }
    }

    public async Task RedoAsync()
    {
        if (!CanRedo) { return; }
        IsBusy = true;
        try
        {
            var result = await _session.RedoAsync(_session.CurrentRevision);
            StatusText = result.Message;
            if (result.Succeeded)
            {
                RefreshAfterOperation();
                await RefreshValidationAsync();
            }
        }
        finally { IsBusy = false; }
    }

    public async Task<bool> SaveAsync(string? path = null, bool overwrite = false)
    {
        if (!CanSave) { return false; }
        IsBusy = true;
        StatusText = "Writing temporary file and verifying XML...";
        try
        {
            var result = await _session.SaveAsync(_session.CurrentRevision, path, overwrite);
            StatusText = result.Message;
            if (result.Succeeded)
            {
                RefreshAfterOperation();
                await RefreshValidationAsync();
            }
            return result.Succeeded;
        }
        finally { IsBusy = false; }
    }

    private void RefreshAfterOperation()
    {
        if (Volatile.Read(ref _disposeStarted) != 0 || _session.CurrentState is not { } state) { return; }
        SynchronizeRows(() =>
        {
            RefreshEngineeringProjection(state);
            RefreshXmlProjection(state);
        });
        SearchResults = Array.Empty<SclSearchResultProjection>();
        SearchResultsHeader = "Search Results";
        RefreshIedWorkspace(state);
        RefreshEngineeringWorkspaces(state, forceIedRefresh: true);
        DocumentDisplayName = state.DisplayName;
        IsDirty = _session.IsDirty;
        ChangeRows = _session.ChangeJournal.SelectMany(entry => entry.Changes.Select(change =>
            new ChangeRow(entry.Revision.ToString(), entry.Action,
                state.SemanticIndex.TryGetNode(change.Target, out var node) && node is not null
                    ? node.DisplayName : change.Target.ToString(),
                change.Before ?? "(absent)", change.After ?? "(absent)"))).Reverse().ToArray();
        SelectionChanged(this, new SclSelectionChangedEventArgs(_selectionService.SelectedNode));
        RefreshEditCommands();
    }

    private async Task RefreshValidationAsync(
        CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _disposeStarted) != 0 ||
            _session.CurrentState is not { } state)
        {
            return;
        }

        var result = await _session
            .ValidateFastAsync(cancellationToken)
            .ConfigureAwait(true);

        if (!result.CanPublish ||
            result.Value is null ||
            result.SourceRevision != state.Revision ||
            result.Value.Revision != state.Revision ||
            _session.CurrentState?.Revision != state.Revision ||
            Volatile.Read(ref _disposeStarted) != 0)
        {
            return;
        }

        PublishDiagnostics(result.Value.Diagnostics, state);
    }

    [ObservableProperty]
    private string _targetProfile = "SICAM SCC";

    [ObservableProperty]
    private IReadOnlyList<ExplorerRow> _engineeringRows = Array.Empty<ExplorerRow>();

    [ObservableProperty]
    private IReadOnlyList<ExplorerRow> _xmlRows = Array.Empty<ExplorerRow>();

    [ObservableProperty]
    private IReadOnlyList<ProblemRow> _problems = Array.Empty<ProblemRow>();

    [ObservableProperty]
    private ProblemRow? _selectedProblemRow;

    [ObservableProperty]
    private string _problemFilterText = string.Empty;

    [ObservableProperty]
    private string _selectedProblemSeverityFilter = "All severities";

    [ObservableProperty]
    private string _selectedProblemDomainFilter = "All domains";

    public IReadOnlyList<string> ProblemSeverityFilters { get; } =
    [
        "All severities",
        "Blocker",
        "Error",
        "Warning",
        "Info"
    ];

    public IReadOnlyList<string> ProblemDomainFilters { get; } =
    [
        "All domains",
        "Reference",
        "Semantic",
        "Engineering",
        "Schema",
        "Xml",
        "Compatibility",
        "Runtime"
    ];

    public string ProblemsHeader =>
        Problems.Count == _allProblems.Length
            ? $"Problems ({Problems.Count})"
            : $"Problems ({Problems.Count}/{_allProblems.Length})";

    [ObservableProperty]
    private IReadOnlyList<SclReferenceProjection> _whereUsedRows =
        Array.Empty<SclReferenceProjection>();

    [ObservableProperty]
    private IReadOnlyList<SclSearchResultProjection> _searchResults =
        Array.Empty<SclSearchResultProjection>();

    [ObservableProperty]
    private ExplorerRow? _selectedEngineeringRow;

    [ObservableProperty]
    private ExplorerRow? _selectedXmlRow;

    [ObservableProperty]
    private SclReferenceProjection? _selectedWhereUsedRow;

    [ObservableProperty]
    private SclSearchResultProjection? _selectedSearchResult;

    [ObservableProperty]
    private string _documentDisplayName = "No document open";

    [ObservableProperty]
    private string _documentKind = "—";

    [ObservableProperty]
    private string _documentNamespace = "—";

    [ObservableProperty]
    private string _documentRevision = "—";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _problemSummary = "No diagnostics";

    [ObservableProperty]
    private string _whereUsedHeader = "Where Used (0)";

    [ObservableProperty]
    private string _searchResultsHeader = "Search Results";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _detailTitle = "Open an SCL file";

    [ObservableProperty]
    private string _detailKind = "IEC 61850 SCL";

    [ObservableProperty]
    private string _detailPath = string.Empty;

    [ObservableProperty]
    private string _engineeringBreadcrumb = string.Empty;

    [ObservableProperty]
    private string _detailSource = string.Empty;

    [ObservableProperty]
    private string _detailNamespace = string.Empty;

    [ObservableProperty]
    private string _attributeDescription = string.Empty;

    [ObservableProperty]
    private string _detailValue = string.Empty;

    [ObservableProperty]
    private string _detailDescription =
        "Open an ICD, IID, CID, SCD, SSD, or SED file to inspect its engineering and XML structure.";

    public MainWindowViewModel()
    {
        _session = new SclDocumentSession();
        _selectionService = new SclSelectionService();
        _selectionService.SelectionChanged += SelectionChanged;
    }

    public string WindowTitle { get; } =
        "ARSCL Studio — IEC 61850 SCL Editor & Configurator";

    public IReadOnlyList<string> TargetProfiles { get; } =
    [
        "Generic MMS Client",
        "SICAM SCC"
    ];

    public async Task OpenFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposeStarted) != 0,
            this);

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusText = $"Opening {Path.GetFileName(path)}...";

        try
        {
            var result = await _session.OpenFileAsync(
                path,
                cancellationToken);

            switch (result.Status)
            {
                case SclOpenStatus.Opened when result.State is not null:
                    IsEditing = false;
                    IsDirty = false;
                    ChangeRows = Array.Empty<ChangeRow>();
                    PublishDocument(result.State);
                    PublishDiagnostics(result.Diagnostics, result.State);
                    _selectionService.Select(
                        SelectedIedWorkspace?.Handle ??
                        result.State.Syntax.RootHandle);
                    await RefreshValidationAsync(cancellationToken);
                    StatusText =
                        $"Loaded {result.State.Syntax.IndexedNodeCount:N0} XML nodes • " +
                        $"{result.State.SemanticIndex.NodeCount:N0} IEC objects • " +
                        $"{result.State.SemanticIndex.References.EdgeCount:N0} references";
                    break;

                case SclOpenStatus.Failed:
                    PublishDiagnostics(result.Diagnostics);
                    StatusText = "Open failed — active document was not changed";
                    break;

                case SclOpenStatus.Superseded:
                    StatusText = "Open request superseded by a newer request";
                    break;

                case SclOpenStatus.Cancelled:
                    StatusText = "Open cancelled";
                    break;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ToggleEngineering(ExplorerRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!row.HasChildren || row.Handle.IsNone)
        {
            return;
        }

        ToggleExpanded(_engineeringExpanded, row.Handle);

        var state = _session.CurrentState;

        if (state is null)
        {
            return;
        }

        SynchronizeRows(
            () => RefreshEngineeringProjection(state));
    }

    public void ToggleXml(ExplorerRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!row.HasChildren || row.Handle.IsNone)
        {
            return;
        }

        ToggleExpanded(_xmlExpanded, row.Handle);

        var state = _session.CurrentState;

        if (state is null)
        {
            return;
        }

        SynchronizeRows(
            () => RefreshXmlProjection(state));
    }

    public async Task SearchAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposeStarted) != 0,
            this);

        var debounce = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);

        var previous = Interlocked.Exchange(
            ref _searchDebounce,
            debounce);

        previous?.Cancel();

        try
        {
            var trimmed = query?.Trim() ?? string.Empty;

            if (trimmed.Length == 0)
            {
                SearchResults = Array.Empty<SclSearchResultProjection>();
                SearchResultsHeader = "Search Results";
                return;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(180),
                debounce.Token);

            var result = await _session.SearchAsync(
                trimmed,
                maximumResults: 250,
                debounce.Token);

            if (!result.CanPublish ||
                result.SourceRevision != _session.CurrentRevision ||
                result.Value is null ||
                Volatile.Read(ref _disposeStarted) != 0)
            {
                return;
            }

            SearchResults = result.Value;
            SearchResultsHeader =
                $"Search Results ({result.Value.Length})";

            StatusText = result.Value.Length == 250
                ? "Search capped at 250 results — refine the query"
                : $"Search found {result.Value.Length:N0} IEC objects";
        }
        catch (OperationCanceledException)
        {
            // Debounce/newer search or window lifetime cancellation.
        }
        finally
        {
            Interlocked.CompareExchange(
                ref _searchDebounce,
                null,
                debounce);

            debounce.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        var search = Interlocked.Exchange(
            ref _searchDebounce,
            null);

        search?.Cancel();

        _selectionService.SelectionChanged -= SelectionChanged;
        _selectionService.Clear();

        _engineeringIndex.Clear();
        _xmlIndex.Clear();
        _engineeringExpanded.Clear();
        _xmlExpanded.Clear();

        EngineeringRows = Array.Empty<ExplorerRow>();
        XmlRows = Array.Empty<ExplorerRow>();
        IedWorkspaceRows = Array.Empty<SclIedWorkspaceProjection>();
        SelectedIedWorkspace = null;
        OnPropertyChanged(nameof(IedWorkspaceHeader));
        ClearEngineeringWorkspaces();
        _allProblems = [];
        Problems = Array.Empty<ProblemRow>();
        SelectedProblemRow = null;
        OnPropertyChanged(nameof(ProblemsHeader));
        WhereUsedRows = Array.Empty<SclReferenceProjection>();
        SearchResults = Array.Empty<SclSearchResultProjection>();

        ChangeRows = Array.Empty<ChangeRow>();
        await _session.DisposeAsync();
    }

    partial void OnSelectedEngineeringRowChanged(ExplorerRow? value) =>
        SelectFromRow(value);

    partial void OnSelectedXmlRowChanged(ExplorerRow? value) =>
        SelectFromRow(value);

    partial void OnProblemFilterTextChanged(string value) =>
        ApplyProblemFilters();

    partial void OnSelectedProblemSeverityFilterChanged(string value) =>
        ApplyProblemFilters();

    partial void OnSelectedProblemDomainFilterChanged(string value) =>
        ApplyProblemFilters();

    partial void OnSelectedProblemRowChanged(ProblemRow? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Node.IsNone)
        {
            ActivateEngineeringWorkspaceForExternalNavigation(value.Node);
            _selectionService.Select(value.Node);
        }
    }

    partial void OnSelectedWhereUsedRowChanged(SclReferenceProjection? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Source.IsNone)
        {
            ActivateEngineeringWorkspaceForExternalNavigation(value.Source);
            _selectionService.Select(value.Source);
        }
    }

    partial void OnSelectedSearchResultChanged(SclSearchResultProjection? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            ActivateEngineeringWorkspaceForExternalNavigation(value.Handle);
            _selectionService.Select(value.Handle);
        }
    }

    private void PublishDocument(SclDocumentState state)
    {
        _selectionService.Clear();
        _engineeringExpanded.Clear();
        _xmlExpanded.Clear();
        _engineeringExpanded.Add(state.Syntax.RootHandle);
        _xmlExpanded.Add(state.Syntax.RootHandle);

        RefreshEngineeringProjection(state);
        RefreshXmlProjection(state);
        RefreshIedWorkspace(state);
        RefreshEngineeringWorkspaces(state, forceIedRefresh: true);

        SearchResults = Array.Empty<SclSearchResultProjection>();
        SearchResultsHeader = "Search Results";
        WhereUsedRows = Array.Empty<SclReferenceProjection>();
        WhereUsedHeader = "Where Used (0)";

        DocumentDisplayName = state.DisplayName;
        DocumentKind = state.Syntax.Metadata.FileKindHint.ToString().ToUpperInvariant();
        DocumentNamespace = string.IsNullOrWhiteSpace(state.Syntax.Metadata.RootNamespace)
            ? "(no namespace)"
            : state.Syntax.Metadata.RootNamespace;
        DocumentRevision = state.Syntax.Metadata.SchemaRevision.ToString();
    }

    private void RefreshEngineeringProjection(SclDocumentState state)
    {
        var rows = MapRows(
            SclExplorerProjector.BuildEngineering(
                state,
                _engineeringExpanded));

        _engineeringIndex.Clear();
        IndexRows(rows, _engineeringIndex);
        EngineeringRows = rows;
    }

    private void RefreshXmlProjection(SclDocumentState state)
    {
        var rows = MapRows(
            SclExplorerProjector.BuildXmlVisible(
                state,
                _xmlExpanded));

        _xmlIndex.Clear();
        IndexRows(rows, _xmlIndex);
        XmlRows = rows;
    }

    private void PublishDiagnostics(
        IReadOnlyList<Diagnostic> diagnostics,
        SclDocumentState? state = null)
    {
        SelectedProblemRow = null;

        if (diagnostics.Count == 0)
        {
            _allProblems = [];
            Problems = Array.Empty<ProblemRow>();
            ProblemSummary = "No diagnostics";
            OnPropertyChanged(nameof(ProblemsHeader));
            return;
        }

        var rows = new ProblemRow[diagnostics.Count];
        var errors = 0;
        var warnings = 0;
        var info = 0;

        for (var i = 0; i < diagnostics.Count; i++)
        {
            var diagnostic = diagnostics[i];
            var objectName = state is not null &&
                !diagnostic.Node.IsNone &&
                state.SemanticIndex.TryGetNode(diagnostic.Node, out var semanticNode) &&
                semanticNode is not null
                    ? semanticNode.DisplayName
                    : null;

            rows[i] = ProblemRow.FromDiagnostic(diagnostic, objectName);

            if (diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Blocker)
            {
                errors++;
            }
            else if (diagnostic.Severity == DiagnosticSeverity.Warning)
            {
                warnings++;
            }
            else
            {
                info++;
            }
        }

        _allProblems = rows;
        ProblemSummary =
            $"{errors} Errors  •  {warnings} Warnings  •  {info} Info";
        ApplyProblemFilters();
    }

    private void ApplyProblemFilters()
    {
        if (_allProblems.Length == 0)
        {
            Problems = Array.Empty<ProblemRow>();
            SelectedProblemRow = null;
            OnPropertyChanged(nameof(ProblemsHeader));
            return;
        }

        var severity = SelectedProblemSeverityFilter;
        var domain = SelectedProblemDomainFilter;
        var query = ProblemFilterText.Trim();

        var filtered = _allProblems.Where(row =>
            (string.Equals(
                 severity,
                 "All severities",
                 StringComparison.Ordinal) ||
             string.Equals(
                 row.Severity,
                 severity,
                 StringComparison.OrdinalIgnoreCase)) &&
            (string.Equals(
                 domain,
                 "All domains",
                 StringComparison.Ordinal) ||
             string.Equals(
                 row.Domain,
                 domain,
                 StringComparison.OrdinalIgnoreCase)) &&
            (query.Length == 0 ||
             row.Code.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             row.Domain.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             row.ObjectName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             row.Message.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             row.Source.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Problems = filtered;
        SelectedProblemRow = null;
        OnPropertyChanged(nameof(ProblemsHeader));
    }

    private void SelectFromRow(ExplorerRow? row)
    {
        if (_synchronizingSelection ||
            row is null ||
            !row.IsSelectable ||
            row.Handle.IsNone)
        {
            return;
        }

        _selectionService.Select(row.Handle);
    }

    private void SelectionChanged(
        object? sender,
        SclSelectionChangedEventArgs args)
    {
        var state = _session.CurrentState;

        if (state is null)
        {
            return;
        }

        _synchronizingSelection = true;

        try
        {
            if (ExpandSemanticAncestors(
                    state,
                    args.SelectedNode))
            {
                RefreshEngineeringProjection(state);
            }

            if (ExpandXmlAncestors(
                    state,
                    args.SelectedNode))
            {
                RefreshXmlProjection(state);
            }

            var details = SclNodeDetailsProjector.Create(
                state,
                args.SelectedNode);

            DetailTitle = details.Title;
            DetailKind = details.Kind;
            DetailPath = details.Path;
            DetailSource = details.SourceLocation;
            DetailNamespace = string.IsNullOrWhiteSpace(details.NamespaceUri)
                ? "(no namespace)"
                : details.NamespaceUri;
            DetailValue = details.Value ?? string.Empty;
            DetailDescription = details.Description;
            AttributeDescription = state.Syntax.TryGetAttributeValue(args.SelectedNode, "desc", out var description)
                ? description ?? string.Empty : "(not set)";

            var whereUsed = SclReferenceProjector.BuildWhereUsed(
                state,
                args.SelectedNode);

            WhereUsedRows = whereUsed;
            WhereUsedHeader = $"Where Used ({whereUsed.Length})";

            SelectedEngineeringRow =
                _engineeringIndex.GetValueOrDefault(args.SelectedNode);

            SynchronizeIedWorkspaceSelection(state, args.SelectedNode);
            SynchronizeEngineeringWorkspaceSelection(state, args.SelectedNode);
            SynchronizeEngineeringNavigationSelection(args.SelectedNode);
            EngineeringBreadcrumb = BuildEngineeringBreadcrumb(
                state,
                args.SelectedNode);

            SelectedXmlRow =
                _xmlIndex.GetValueOrDefault(args.SelectedNode);

            SelectedProblemRow = null;
            SelectedWhereUsedRow = null;
            SelectedSearchResult = null;
            RefreshEditCommands();
        }
        finally
        {
            _synchronizingSelection = false;
        }
    }

    private string BuildEngineeringBreadcrumb(
        SclDocumentState state,
        SclNodeHandle selected)
    {
        var parts = new List<string>(5);

        if (!string.IsNullOrWhiteSpace(SelectedIedWorkspace?.Name))
        {
            parts.Add(SelectedIedWorkspace.Name);
        }

        var logicalDevice = FindSelfOrAncestorName(
            state,
            selected,
            SclSemanticKind.LogicalDevice);

        if (!string.IsNullOrWhiteSpace(logicalDevice) &&
            !parts.Contains(logicalDevice, StringComparer.Ordinal))
        {
            parts.Add(logicalDevice);
        }

        var logicalNode = FindSelfOrAncestorName(
            state,
            selected,
            SclSemanticKind.LogicalNodeZero,
            SclSemanticKind.LogicalNode);

        if (!string.IsNullOrWhiteSpace(logicalNode) &&
            !parts.Contains(logicalNode, StringComparer.Ordinal))
        {
            parts.Add(logicalNode);
        }

        var workspace = SelectedEngineeringWorkspaceName;

        if (!string.IsNullOrWhiteSpace(workspace))
        {
            parts.Add(workspace);
        }

        if (state.SemanticIndex.TryGetNode(selected, out var node) &&
            node is not null &&
            node.Kind is not (
                SclSemanticKind.Ied or
                SclSemanticKind.LogicalDevice or
                SclSemanticKind.LogicalNodeZero or
                SclSemanticKind.LogicalNode) &&
            !string.IsNullOrWhiteSpace(node.DisplayName) &&
            !parts.Contains(node.DisplayName, StringComparer.Ordinal))
        {
            parts.Add(node.DisplayName);
        }

        return string.Join(" › ", parts);
    }

    private static string FindSelfOrAncestorName(
        SclDocumentState state,
        SclNodeHandle selected,
        params SclSemanticKind[] kinds)
    {
        if (state.SemanticIndex.TryGetNode(selected, out var selectedNode) &&
            selectedNode is not null &&
            kinds.Contains(selectedNode.Kind))
        {
            return selectedNode.DisplayName;
        }

        for (var i = 0; i < kinds.Length; i++)
        {
            if (state.SemanticIndex.TryFindAncestor(
                    selected,
                    kinds[i],
                    out var ancestor) &&
                ancestor is not null)
            {
                return ancestor.DisplayName;
            }
        }

        return string.Empty;
    }

    private bool ExpandSemanticAncestors(
        SclDocumentState state,
        SclNodeHandle selected)
    {
        if (!state.SemanticIndex.TryGetNode(
                selected,
                out var node) ||
            node is null)
        {
            return false;
        }

        var changed = false;
        var parent = node.Parent;

        for (var depth = 0; depth < 256 && !parent.IsNone; depth++)
        {
            changed |= _engineeringExpanded.Add(parent);

            if (!state.SemanticIndex.TryGetNode(
                    parent,
                    out var parentNode) ||
                parentNode is null)
            {
                break;
            }

            parent = parentNode.Parent;
        }

        return changed;
    }

    private bool ExpandXmlAncestors(
        SclDocumentState state,
        SclNodeHandle selected)
    {
        if (!state.Syntax.TryGetNodeInfo(
                selected,
                out var node) ||
            node is null)
        {
            return false;
        }

        var changed = false;
        var parent = node.Parent;

        for (var depth = 0; depth < 256 && !parent.IsNone; depth++)
        {
            changed |= _xmlExpanded.Add(parent);

            if (!state.Syntax.TryGetNodeInfo(
                    parent,
                    out var parentNode) ||
                parentNode is null)
            {
                break;
            }

            parent = parentNode.Parent;
        }

        return changed;
    }

    private void SynchronizeRows(Action refresh)
    {
        ArgumentNullException.ThrowIfNull(refresh);

        var selected = _selectionService.SelectedNode;
        _synchronizingSelection = true;

        try
        {
            refresh();

            SelectedEngineeringRow =
                _engineeringIndex.GetValueOrDefault(selected);

            SelectedXmlRow =
                _xmlIndex.GetValueOrDefault(selected);
        }
        finally
        {
            _synchronizingSelection = false;
        }
    }

    private static void ToggleExpanded(
        HashSet<SclNodeHandle> expanded,
        SclNodeHandle handle)
    {
        if (!expanded.Remove(handle))
        {
            expanded.Add(handle);
        }
    }

    private static ExplorerRow[] MapRows(
        IReadOnlyList<ExplorerRowProjection> projections)
    {
        var rows = new ExplorerRow[projections.Count];

        for (var i = 0; i < projections.Count; i++)
        {
            var projection = projections[i];

            rows[i] = new ExplorerRow(
                projection.Handle,
                projection.Kind,
                projection.Depth,
                projection.Label,
                projection.Badge,
                projection.IsSelectable,
                projection.HasChildren,
                projection.IsExpanded);
        }

        return rows;
    }

    private static void IndexRows(
        ExplorerRow[] rows,
        Dictionary<SclNodeHandle, ExplorerRow> destination)
    {
        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];

            if (!row.Handle.IsNone)
            {
                destination[row.Handle] = row;
            }
        }
    }
}

