namespace ArSclStudio.Engine.Documents;

public enum SclSaveStatus { Saved, Failed, Cancelled, StaleRevision }

public sealed record SclSaveResult(SclSaveStatus Status, DocumentRevision Revision, string Message)
{
    public bool Succeeded => Status == SclSaveStatus.Saved;
}
