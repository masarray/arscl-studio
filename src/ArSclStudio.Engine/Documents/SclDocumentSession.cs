using System.Xml;
using ArSclStudio.Engine.Editing;
using ArSclStudio.Engine.Diagnostics;
using ArSclStudio.Engine.Search;
using ArSclStudio.Engine.Workers;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;
using ArSclStudio.Scl.Source;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Documents;

public sealed partial class SclDocumentSession : IAsyncDisposable
{
    private readonly LatestWorkCoordinator _latestWork;
    private readonly object _stateGate = new();
    private SclDocumentState? _currentState;
    private long _revision;
    private long _openSequence;
    private int _disposeStarted;

    public SclDocumentSession(int? maxWorkerConcurrency = null, ISclTransactionValidator? transactionValidator = null)
    {
        var concurrency = maxWorkerConcurrency ??
            Math.Clamp(Environment.ProcessorCount / 2, 1, 4);

        _latestWork = new LatestWorkCoordinator(concurrency);
        _transactionValidator = transactionValidator;
    }

    public Guid SessionId { get; } = Guid.NewGuid();

    public DocumentRevision CurrentRevision => new(Interlocked.Read(ref _revision));

    public SclDocumentState? CurrentState
    {
        get
        {
            lock (_stateGate)
            {
                return _currentState;
            }
        }
    }

    public DocumentRevision AdvanceRevision()
    {
        lock (_stateGate)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeStarted) != 0, this);
            return new(Interlocked.Increment(ref _revision));
        }
    }

    public async Task<SclOpenResult> OpenFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposeStarted) != 0,
            this);

        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var openSequence = Interlocked.Increment(ref _openSequence);
        try
        {
            var loader = new SclDocumentLoader();

            var workResult = await RunLatestAsync(
                WorkKind.Parse,
                token => Task.Run(async () =>
                {
                    var fingerprint = await SclFileFingerprint.ReadAsync(path, token).ConfigureAwait(false);
                    var syntax = await loader
                        .LoadFileAsync(path, token)
                        .ConfigureAwait(false);

                    token.ThrowIfCancellationRequested();

                    if (fingerprint != await SclFileFingerprint.ReadAsync(path, token).ConfigureAwait(false))
                    {
                        throw new IOException("The source file changed while opening. Try opening it again.");
                    }

                    var topLevel = SclTopLevelIndexer.Build(syntax);
                    var semantic = SclSemanticIndexBuilder.Build(syntax);

                    token.ThrowIfCancellationRequested();

                    return new PendingDocumentState(
                        Path.GetFullPath(path),
                        Path.GetFileName(path),
                        syntax,
                        topLevel,
                        semantic,
                        fingerprint);
                }, token),
                cancellationToken).ConfigureAwait(false);

            switch (workResult.Status)
            {
                case WorkResultStatus.Published when workResult.Value is not null:
                {
                    return await _exclusiveWork.RunAsync(token =>
                    {
                        lock (_stateGate)
                        {
                            token.ThrowIfCancellationRequested();
                            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeStarted) != 0, this);
                            if (workResult.SourceRevision != CurrentRevision ||
                                openSequence != Interlocked.Read(ref _openSequence))
                            {
                                return Task.FromResult(new SclOpenResult(SclOpenStatus.Superseded, null, []));
                            }

                            var revision = AdvanceRevision();
                            var pending = workResult.Value;
                            var state = new SclDocumentState(pending.SourcePath, pending.DisplayName,
                                pending.Syntax, pending.TopLevelIndex, pending.SemanticIndex, revision);
                            _currentState = state;
                            _sourceFingerprint = pending.Fingerprint;
                            ResetEditingState();
                            return Task.FromResult(new SclOpenResult(SclOpenStatus.Opened, state, []));
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }

                case WorkResultStatus.Cancelled:
                    return new SclOpenResult(
                        SclOpenStatus.Cancelled,
                        null,
                        Array.Empty<Diagnostic>());

                case WorkResultStatus.Superseded:
                case WorkResultStatus.StaleRevision:
                    return new SclOpenResult(
                        SclOpenStatus.Superseded,
                        null,
                        Array.Empty<Diagnostic>());

                default:
                    return new SclOpenResult(
                        SclOpenStatus.Failed,
                        null,
                        [CreateRuntimeDiagnostic(
                            path,
                            "SCL-OPEN-0002",
                            "The SCL open operation completed without a publishable document.")]);
            }
        }
        catch (OperationCanceledException)
        {
            return new SclOpenResult(SclOpenStatus.Cancelled, null, []);
        }
        catch (XmlException exception)
        {
            return new SclOpenResult(
                SclOpenStatus.Failed,
                null,
                [new Diagnostic(
                    "SCL-XML-0001",
                    DiagnosticSeverity.Error,
                    DiagnosticDomain.Xml,
                    exception.Message,
                    SclNodeHandle.None,
                    new SclSourceSpan(
                        exception.LineNumber,
                        exception.LinePosition),
                    path,
                    "The file was not committed to the active document session. Correct the XML/SCL syntax and open it again.")]);
        }
        catch (IOException exception)
        {
            return new SclOpenResult(
                SclOpenStatus.Failed,
                null,
                [CreateRuntimeDiagnostic(
                    path,
                    "SCL-IO-0001",
                    exception.Message)]);
        }
        catch (UnauthorizedAccessException exception)
        {
            return new SclOpenResult(
                SclOpenStatus.Failed,
                null,
                [CreateRuntimeDiagnostic(
                    path,
                    "SCL-IO-0002",
                    exception.Message)]);
        }
    }

    public async Task<WorkResult<SclSearchResultProjection[]>> SearchAsync(
        string query,
        int maximumResults = 200,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposeStarted) != 0,
            this);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumResults);

        var state = CurrentState;

        if (state is null || string.IsNullOrWhiteSpace(query))
        {
            return new WorkResult<SclSearchResultProjection[]>(
                WorkResultStatus.Published,
                CurrentRevision,
                []);
        }

        var capturedQuery = query.Trim();

        return await RunLatestAsync(
            WorkKind.Search,
            token => Task.Run(
                () => SclSemanticSearch.Search(
                    state,
                    capturedQuery,
                    maximumResults,
                    token),
                token),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkResult<T>> RunLatestAsync<T>(
        WorkKind kind,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposeStarted) != 0,
            this);

        var sourceRevision = CurrentRevision;

        var result = await _latestWork.RunAsync(
            kind,
            sourceRevision,
            operation,
            cancellationToken).ConfigureAwait(false);

        if (result.Status == WorkResultStatus.Published &&
            result.SourceRevision != CurrentRevision)
        {
            return result with
            {
                Status = WorkResultStatus.StaleRevision,
                Value = default
            };
        }

        return result;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        await _exclusiveWork.DisposeAsync().ConfigureAwait(false);
        await _latestWork.DisposeAsync().ConfigureAwait(false);

        lock (_stateGate)
        {
            _currentState = null;
            _sourceFingerprint = null;
            ResetEditingState();
        }
    }

    private static Diagnostic CreateRuntimeDiagnostic(
        string path,
        string code,
        string message) =>
        new(
            code,
            DiagnosticSeverity.Error,
            DiagnosticDomain.Runtime,
            message,
            SclNodeHandle.None,
            default,
            path,
            "The previously opened document, if any, remains unchanged.");

    private sealed record PendingDocumentState(
        string SourcePath,
        string DisplayName,
        SclSyntaxDocument Syntax,
        SclTopLevelIndex TopLevelIndex,
        SclSemanticIndex SemanticIndex,
        string? Fingerprint);
}

