namespace ArSclStudio.Engine.Workers;

public enum WorkKind
{
    Parse = 0,
    Index,
    ValidateFast,
    ValidateFull,
    Search,
    Compatibility,
    Diff,
    MergePreview
}
