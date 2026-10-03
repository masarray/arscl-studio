using ArSclStudio.Engine.Documents;

namespace ArSclStudio.Engine.Editing;

public enum SclEditStatus { Committed, NoChange, Rejected, Cancelled, StaleRevision }

public sealed record SclEditResult(SclEditStatus Status, DocumentRevision Revision, string Message)
{
    public bool Succeeded => Status is SclEditStatus.Committed or SclEditStatus.NoChange;
}

public sealed record SclChangeJournalEntry(
    DocumentRevision Revision, DateTimeOffset Timestamp, string Action, string Description,
    IReadOnlyList<DescriptionChange> Changes);

/// <summary>Optional additional fast checks over the isolated candidate; throwing vetoes commit.</summary>
public interface ISclTransactionValidator
{
    void Validate(SclDocumentState candidate, CancellationToken cancellationToken);
}
