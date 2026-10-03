using ArSclStudio.Engine.Diagnostics;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Engine.Search;
using ArSclStudio.Engine.Workers;
using ArSclStudio.Scl.Identity;
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

    private CancellationTokenSource? _searchDebounce;
    private bool _synchronizingSelection;
    private int _disposeStarted;

    [ObservableProperty]
    private string _targetProfile = "SICAM SCC";

    [ObservableProperty]
    private IReadOnlyList<ExplorerRow> _engineeringRows = Array.Empty<ExplorerRow>();

    [ObservableProperty]
    private IReadOnlyList<ExplorerRow> _xmlRows = Array.Empty<ExplorerRow>();

    [ObservableProperty]
    private IReadOnlyList<ProblemRow> _problems = Array.Empty<ProblemRow>();

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
    private string _detailSource = string.Empty;

    [ObservableProperty]
    private string _detailNamespace = string.Empty;

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
                    PublishDocument(result.State);
                    PublishDiagnostics(result.Diagnostics);
                    _selectionService.Select(result.State.Syntax.RootHandle);
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
        Problems = Array.Empty<ProblemRow>();
        WhereUsedRows = Array.Empty<SclReferenceProjection>();
        SearchResults = Array.Empty<SclSearchResultProjection>();

        await _session.DisposeAsync();
    }

    partial void OnSelectedEngineeringRowChanged(ExplorerRow? value) =>
        SelectFromRow(value);

    partial void OnSelectedXmlRowChanged(ExplorerRow? value) =>
        SelectFromRow(value);

    partial void OnSelectedWhereUsedRowChanged(SclReferenceProjection? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Source.IsNone)
        {
            _selectionService.Select(value.Source);
        }
    }

    partial void OnSelectedSearchResultChanged(SclSearchResultProjection? value)
    {
        if (!_synchronizingSelection &&
            value is not null &&
            !value.Handle.IsNone)
        {
            _selectionService.Select(value.Handle);
        }
    }

    private void PublishDocument(SclDocumentState state)
    {
        _engineeringExpanded.Clear();
        _xmlExpanded.Clear();
        _engineeringExpanded.Add(state.Syntax.RootHandle);
        _xmlExpanded.Add(state.Syntax.RootHandle);

        RefreshEngineeringProjection(state);
        RefreshXmlProjection(state);

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

    private void PublishDiagnostics(IReadOnlyList<Diagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            Problems = Array.Empty<ProblemRow>();
            ProblemSummary = "No diagnostics";
            return;
        }

        var rows = new ProblemRow[diagnostics.Count];
        var errors = 0;
        var warnings = 0;

        for (var i = 0; i < diagnostics.Count; i++)
        {
            var diagnostic = diagnostics[i];
            rows[i] = ProblemRow.FromDiagnostic(diagnostic);

            if (diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Blocker)
            {
                errors++;
            }
            else if (diagnostic.Severity == DiagnosticSeverity.Warning)
            {
                warnings++;
            }
        }

        Problems = rows;
        ProblemSummary = $"{errors} Errors  •  {warnings} Warnings";
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

            var whereUsed = SclReferenceProjector.BuildWhereUsed(
                state,
                args.SelectedNode);

            WhereUsedRows = whereUsed;
            WhereUsedHeader = $"Where Used ({whereUsed.Length})";

            SelectedEngineeringRow =
                _engineeringIndex.GetValueOrDefault(args.SelectedNode);

            SelectedXmlRow =
                _xmlIndex.GetValueOrDefault(args.SelectedNode);

            SelectedWhereUsedRow = null;
            SelectedSearchResult = null;
        }
        finally
        {
            _synchronizingSelection = false;
        }
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
