using ArSclStudio.Engine.Diagnostics;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Identity;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArSclStudio.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IAsyncDisposable
{
    private readonly SclDocumentSession _session;
    private readonly SclSelectionService _selectionService;
    private readonly Dictionary<SclNodeHandle, ExplorerRow> _engineeringIndex = [];
    private readonly Dictionary<SclNodeHandle, ExplorerRow> _xmlIndex = [];

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
    private ExplorerRow? _selectedEngineeringRow;

    [ObservableProperty]
    private ExplorerRow? _selectedXmlRow;

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
                        $"Loaded {result.State.Syntax.IndexedNodeCount:N0} indexed XML nodes";
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

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        _selectionService.SelectionChanged -= SelectionChanged;
        _selectionService.Clear();

        _engineeringIndex.Clear();
        _xmlIndex.Clear();

        EngineeringRows = Array.Empty<ExplorerRow>();
        XmlRows = Array.Empty<ExplorerRow>();
        Problems = Array.Empty<ProblemRow>();

        await _session.DisposeAsync();
    }

    partial void OnSelectedEngineeringRowChanged(ExplorerRow? value) =>
        SelectFromRow(value);

    partial void OnSelectedXmlRowChanged(ExplorerRow? value) =>
        SelectFromRow(value);

    private void PublishDocument(SclDocumentState state)
    {
        var engineering = MapRows(
            SclExplorerProjector.BuildEngineering(state));

        var xml = MapRows(
            SclExplorerProjector.BuildXmlRoot(state));

        _engineeringIndex.Clear();
        _xmlIndex.Clear();

        IndexRows(engineering, _engineeringIndex);
        IndexRows(xml, _xmlIndex);

        EngineeringRows = engineering;
        XmlRows = xml;

        DocumentDisplayName = state.DisplayName;
        DocumentKind = state.Syntax.Metadata.FileKindHint.ToString().ToUpperInvariant();
        DocumentNamespace = string.IsNullOrWhiteSpace(state.Syntax.Metadata.RootNamespace)
            ? "(no namespace)"
            : state.Syntax.Metadata.RootNamespace;
        DocumentRevision = state.Syntax.Metadata.SchemaRevision.ToString();
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

        _synchronizingSelection = true;

        try
        {
            SelectedEngineeringRow =
                _engineeringIndex.GetValueOrDefault(args.SelectedNode);

            SelectedXmlRow =
                _xmlIndex.GetValueOrDefault(args.SelectedNode);
        }
        finally
        {
            _synchronizingSelection = false;
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
        IReadOnlyList<ExplorerRow> rows,
        Dictionary<SclNodeHandle, ExplorerRow> destination)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];

            if (!row.Handle.IsNone)
            {
                destination[row.Handle] = row;
            }
        }
    }
}
