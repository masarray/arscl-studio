using ArSclStudio.Engine.Documents;

namespace ArSclStudio.Engine.Workers;

public enum WorkResultStatus
{
    Published = 0,
    Superseded,
    Cancelled,
    StaleRevision
}

public sealed record WorkResult<T>(
    WorkResultStatus Status,
    DocumentRevision SourceRevision,
    T? Value)
{
    public bool CanPublish => Status == WorkResultStatus.Published;
}
