using ArSclStudio.Engine.Workers;

namespace ArSclStudio.Engine.Documents;

public sealed class SclDocumentSession : IAsyncDisposable
{
    private readonly LatestWorkCoordinator _latestWork;
    private long _revision;
    private int _disposeStarted;

    public SclDocumentSession(int? maxWorkerConcurrency = null)
    {
        var concurrency = maxWorkerConcurrency ??
            Math.Clamp(Environment.ProcessorCount / 2, 1, 4);

        _latestWork = new LatestWorkCoordinator(concurrency);
    }

    public Guid SessionId { get; } = Guid.NewGuid();

    public DocumentRevision CurrentRevision => new(Interlocked.Read(ref _revision));

    public DocumentRevision AdvanceRevision() =>
        new(Interlocked.Increment(ref _revision));

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

        await _latestWork.DisposeAsync().ConfigureAwait(false);
    }
}
