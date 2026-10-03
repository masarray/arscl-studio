using System.Xml;
using ArSclStudio.Scl.Documents;

namespace ArSclStudio.Engine.Documents;

public sealed partial class SclDocumentSession
{
    private string? _sourceFingerprint;

    public async Task<SclSaveResult> SaveAsync(DocumentRevision expectedRevision, string? destinationPath = null,
        bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeStarted) != 0, this);
        try
        {
            return await _exclusiveWork.RunAsync(token => Task.Run(async () =>
            {
                SclDocumentState state;
                string? sourceFingerprint;
                lock (_stateGate)
                {
                    if (CurrentRevision != expectedRevision)
                    {
                        return new SclSaveResult(SclSaveStatus.StaleRevision, CurrentRevision, "Save revision is stale.");
                    }

                    state = _currentState ?? throw new InvalidOperationException("No document is open.");
                    sourceFingerprint = _sourceFingerprint;
                }

                var destination = Path.GetFullPath(destinationPath ?? state.SourcePath);
                if (SclDocumentProbe.ClassifyFromFileName(destination) != state.Syntax.Metadata.FileKindHint)
                {
                    throw new InvalidOperationException("Save As preserves the file role. Keep the current SCL extension; role conversion requires export.");
                }

                var samePath = string.Equals(destination, state.SourcePath,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                var expected = samePath ? sourceFingerprint :
                    await SclFileFingerprint.ReadAsync(destination, token).ConfigureAwait(false);
                if (!samePath && expected is not null && !overwrite)
                {
                    throw new IOException("Save As destination already exists; explicit overwrite is required.");
                }

                using var prepared = await SclAtomicWriter.PrepareAsync(state, destination, expected, token).ConfigureAwait(false);
                if (await SclFileFingerprint.ReadAsync(destination, token).ConfigureAwait(false) != expected)
                {
                    throw new IOException("Destination changed during save; original destination was left untouched.");
                }

                lock (_stateGate)
                {
                    token.ThrowIfCancellationRequested();
                    ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeStarted) != 0, this);
                    if (CurrentRevision != expectedRevision || !ReferenceEquals(state, _currentState))
                    {
                        return new SclSaveResult(SclSaveStatus.StaleRevision, CurrentRevision, "Save revision changed; destination untouched.");
                    }

                    // The rename is the commit point. Do not report cancellation after this point.
                    prepared.Commit();
                    var revision = AdvanceRevision();
                    _currentState = state with
                    {
                        Syntax = prepared.Syntax, SourcePath = destination,
                        DisplayName = Path.GetFileName(destination), Revision = revision
                    };
                    _sourceFingerprint = prepared.OutputFingerprint;
                    _savedContentId = _contentId;
                    return new SclSaveResult(SclSaveStatus.Saved, revision, "Saved and reopened successfully; XML content verified.");
                }
            }, token), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return new(SclSaveStatus.Cancelled, CurrentRevision, "Save cancelled; destination unchanged.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            XmlException or InvalidOperationException or ArgumentException or NotSupportedException)
        {
            return new(SclSaveStatus.Failed, CurrentRevision, exception.Message);
        }
    }
}
