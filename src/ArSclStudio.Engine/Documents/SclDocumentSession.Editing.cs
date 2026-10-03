using System.Xml;
using ArSclStudio.Engine.Editing;
using ArSclStudio.Engine.Workers;

namespace ArSclStudio.Engine.Documents;

public sealed partial class SclDocumentSession
{
    private const int HistoryLimit = 128;
    private const int HistoryCharacterLimit = 1_048_576;
    private readonly ExclusiveWorkQueue _exclusiveWork = new();
    private readonly List<HistoryEntry> _undo = [];
    private readonly List<HistoryEntry> _redo = [];
    private readonly List<SclChangeJournalEntry> _journal = [];
    private readonly ISclTransactionValidator? _transactionValidator;
    private long _contentId;
    private long _nextContentId;
    private long _savedContentId;

    public bool IsDirty { get { lock (_stateGate) { return _contentId != _savedContentId; } } }
    public bool CanUndo { get { lock (_stateGate) { return _undo.Count != 0; } } }
    public bool CanRedo { get { lock (_stateGate) { return _redo.Count != 0; } } }
    public IReadOnlyList<SclChangeJournalEntry> ChangeJournal
    {
        get { lock (_stateGate) { return _journal.ToArray(); } }
    }

    public Task<SclEditResult> ExecuteAsync(ISclEditCommand command, DocumentRevision expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return EditAsync(command, expectedRevision, "Edit", cancellationToken);
    }

    public Task<SclEditResult> UndoAsync(DocumentRevision expectedRevision, CancellationToken cancellationToken = default) =>
        EditAsync(null, expectedRevision, "Undo", cancellationToken);

    public Task<SclEditResult> RedoAsync(DocumentRevision expectedRevision, CancellationToken cancellationToken = default) =>
        EditAsync(null, expectedRevision, "Redo", cancellationToken);

    private async Task<SclEditResult> EditAsync(ISclEditCommand? command, DocumentRevision expectedRevision,
        string action, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeStarted) != 0, this);
        try
        {
            return await _exclusiveWork.RunAsync(token => Task.Run(() =>
                PrepareAndCommitEdit(command, expectedRevision, action, token), token), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return new(SclEditStatus.Cancelled, CurrentRevision, "Edit cancelled; document unchanged.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or XmlException)
        {
            return new(SclEditStatus.Rejected, CurrentRevision, exception.Message);
        }
    }

    private SclEditResult PrepareAndCommitEdit(ISclEditCommand? command, DocumentRevision expectedRevision,
        string action, CancellationToken cancellationToken)
    {
        SclDocumentState state;
        HistoryEntry? history = null;
        lock (_stateGate)
        {
            if (expectedRevision != CurrentRevision)
            {
                return new(SclEditStatus.StaleRevision, CurrentRevision, "The document revision changed; reload the property.");
            }

            state = _currentState ?? throw new InvalidOperationException("No document is open.");
            if (command is null)
            {
                var entries = action == "Undo" ? _undo : _redo;
                if (entries.Count == 0)
                {
                    return new(SclEditStatus.NoChange, CurrentRevision, $"Nothing to {action.ToLowerInvariant()}.");
                }

                history = entries[^1];
            }
        }

        var description = command?.Description ?? history!.Description;
        if (string.IsNullOrWhiteSpace(description) || description.Length > 256)
        {
            throw new InvalidOperationException("Transaction description requires 1–256 characters.");
        }

        var prepared = command?.Prepare(state) ?? history!.Changes;
        if (prepared.Count > 64)
        {
            throw new InvalidOperationException("A transaction is limited to 64 property changes.");
        }

        var changes = new DescriptionChange[prepared.Count];
        for (var i = 0; i < changes.Length; i++)
        {
            var change = prepared[i];
            changes[i] = action == "Undo" ? new(change.Target, change.After, change.Before) : change;
        }

        SclEditPolicy.Validate(state, changes, after: false, cancellationToken);
        changes = Array.FindAll(changes, static change => change.Before != change.After);
        if (changes.Length == 0)
        {
            return new(SclEditStatus.NoChange, CurrentRevision, "No property changed.");
        }

        // One staging clone per compound transaction, never one whole-document snapshot per undo entry.
        var syntax = state.Syntax.CreateEditableCopy(cancellationToken);
        foreach (var change in changes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            syntax.SetDescription(change.Target, change.After);
        }

        // desc is deliberately absent from semantic identity/display/reference indexes; reuse safely.
        var candidate = state with { Syntax = syntax };
        SclEditPolicy.Validate(candidate, changes, after: true, cancellationToken);
        _transactionValidator?.Validate(candidate, cancellationToken);

        lock (_stateGate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeStarted) != 0, this);
            if (expectedRevision != CurrentRevision || !ReferenceEquals(state, _currentState))
            {
                return new(SclEditStatus.StaleRevision, CurrentRevision, "The prepared edit was superseded.");
            }

            if (command is not null)
            {
                history = new HistoryEntry(description, Array.AsReadOnly(changes), _contentId, ++_nextContentId);
                _undo.Add(history);
                _redo.Clear();
                _contentId = history.AfterId;
                TrimHistory(_undo);
            }
            else if (action == "Undo")
            {
                _undo.RemoveAt(_undo.Count - 1);
                _redo.Add(history!);
                _contentId = history!.BeforeId;
            }
            else
            {
                _redo.RemoveAt(_redo.Count - 1);
                _undo.Add(history!);
                _contentId = history!.AfterId;
            }

            var revision = AdvanceRevision();
            _currentState = candidate with { Revision = revision };
            _journal.Add(new(revision, DateTimeOffset.UtcNow, action, description, Array.AsReadOnly(changes)));
            TrimJournal();
            return new(SclEditStatus.Committed, revision, $"{action}: {description}");
        }
    }

    private static int CharacterCount(IReadOnlyList<DescriptionChange> changes)
    {
        var count = 0;
        foreach (var change in changes)
        {
            count += (change.Before?.Length ?? 0) + (change.After?.Length ?? 0);
        }

        return count;
    }

    private static void TrimHistory(List<HistoryEntry> entries)
    {
        var characters = 0;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            characters += CharacterCount(entries[i].Changes);
            if (entries.Count - i > HistoryLimit || characters > HistoryCharacterLimit)
            {
                entries.RemoveRange(0, i + 1);
                break;
            }
        }
    }

    private void TrimJournal()
    {
        var characters = 0;
        for (var i = _journal.Count - 1; i >= 0; i--)
        {
            characters += CharacterCount(_journal[i].Changes);
            if (_journal.Count - i > 256 || characters > HistoryCharacterLimit)
            {
                _journal.RemoveRange(0, i + 1);
                break;
            }
        }
    }

    private void ResetEditingState()
    {
        _undo.Clear();
        _redo.Clear();
        _journal.Clear();
        _contentId = ++_nextContentId;
        _savedContentId = _contentId;
    }

    private sealed record HistoryEntry(string Description, IReadOnlyList<DescriptionChange> Changes,
        long BeforeId, long AfterId);
}
