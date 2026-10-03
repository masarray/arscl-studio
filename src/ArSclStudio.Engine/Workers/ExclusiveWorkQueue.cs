namespace ArSclStudio.Engine.Workers;

// Explicit edits/saves are must-run, with bounded admission and one active writer.
internal sealed class ExclusiveWorkQueue : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly SemaphoreSlim _capacity = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _inFlight;
    private bool _closing;

    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_closing, this);
            if (_inFlight >= 16)
            {
                throw new InvalidOperationException("Document operation queue is full.");
            }

            _inFlight++;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
        try
        {
            await _capacity.WaitAsync(linked.Token).ConfigureAwait(false);
            try
            {
                return await operation(linked.Token).ConfigureAwait(false);
            }
            finally
            {
                _capacity.Release();
            }
        }
        finally
        {
            lock (_gate)
            {
                _inFlight--;
                if (_closing && _inFlight == 0)
                {
                    _drained.TrySetResult();
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_closing)
            {
                return;
            }

            _closing = true;
            if (_inFlight == 0)
            {
                _drained.TrySetResult();
            }
        }

        _lifetime.Cancel();
        await _drained.Task.ConfigureAwait(false);
        _capacity.Dispose();
        _lifetime.Dispose();
    }
}
