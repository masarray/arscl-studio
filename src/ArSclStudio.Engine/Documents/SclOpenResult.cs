using ArSclStudio.Engine.Diagnostics;

namespace ArSclStudio.Engine.Documents;

public enum SclOpenStatus
{
    Opened = 0,
    Cancelled,
    Superseded,
    Failed
}

public sealed record SclOpenResult(
    SclOpenStatus Status,
    SclDocumentState? State,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool Succeeded => Status == SclOpenStatus.Opened && State is not null;
}
