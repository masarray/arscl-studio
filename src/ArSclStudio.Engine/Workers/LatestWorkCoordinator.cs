using System.Collections.Concurrent;
using ArSclStudio.Engine.Documents;

namespace ArSclStudio.Engine.Workers;

public sealed class LatestWorkCoordinator : IAsyncDisposable
{
    private sealed class Slot
    {
        public object Gate { get; } = new();

        public long RequestId { get; set; }

        public CancellationTokenSource? Cancellation { get; set; }
    }

    private readonly ConcurrentDictionary<WorkKind, Slot> _slots = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _capacity;
    private readonly object _lifecycleGate = new();
    private readonly TaskCompletionSource _drained =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private long _requestSequence;
    private int _inFlight;
    private bool _disposeStarted;

    public LatestWorkCoordinator(int maxConcurrency)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);
        _capacity = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public async Task<WorkResult<T>> RunAsync<T>(
        WorkKind kind,
        DocumentRevision sourceRevision,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        RegisterRun();

        var requestId = Interlocked.Increment(ref _requestSequence);
        var linked = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetime.Token,
            cancellationToken);

        var slot = _slots.GetOrAdd(kind, static _ => new Slot());

        lock (slot.Gate)
        {
            slot.Cancellation?.Cancel();
            slot.Cancellation = linked;
            slot.RequestId = requestId;
        }


        try
        {
            await _capacity.WaitAsync(linked.Token).ConfigureAwait(false);

            try
            {
                var value = await operation(linked.Token).ConfigureAwait(false);

                lock (slot.Gate)
                {
                    if (slot.RequestId != requestId)
                    {
                        return new WorkResult<T>(
                            WorkResultStatus.Superseded,
                            sourceRevision,
                            default);
                    }
                }

                return new WorkResult<T>(
                    WorkResultStatus.Published,
                    sourceRevision,
                    value);
            }
            finally
            {
                _capacity.Release();
            }
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
            var superseded = false;

            lock (slot.Gate)
            {
                superseded = slot.RequestId != requestId;
            }

            return new WorkResult<T>(
                superseded ? WorkResultStatus.Superseded : WorkResultStatus.Cancelled,
                sourceRevision,
                default);
        }
        finally
        {
            lock (slot.Gate)
            {
                if (slot.RequestId == requestId)
                {
                    slot.Cancellation = null;
                }
            }

            linked.Dispose();
            CompleteRun();
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_lifecycleGate)
        {
            if (_disposeStarted)
            {
                return;
            }

            _disposeStarted = true;

            if (_inFlight == 0)
            {
                _drained.TrySetResult();
            }
        }

        _lifetime.Cancel();

        foreach (var slot in _slots.Values)
        {
            lock (slot.Gate)
            {
                slot.Cancellation?.Cancel();
            }
        }

        await _drained.Task.ConfigureAwait(false);

        _slots.Clear();
        _capacity.Dispose();
        _lifetime.Dispose();
    }

    private void RegisterRun()
    {
        lock (_lifecycleGate)
        {
            ObjectDisposedException.ThrowIf(_disposeStarted, this);
            _inFlight++;
        }
    }

    private void CompleteRun()
    {
        lock (_lifecycleGate)
        {
            _inFlight--;

            if (_disposeStarted && _inFlight == 0)
            {
                _drained.TrySetResult();
            }
        }
    }
}

