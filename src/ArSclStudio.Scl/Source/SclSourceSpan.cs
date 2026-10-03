namespace ArSclStudio.Scl.Source;

public readonly record struct SclSourceSpan(int Line, int Column)
{
    public static SclSourceSpan Unknown => default;

    public bool IsKnown => Line > 0 && Column > 0;

    public override string ToString() =>
        IsKnown
            ? $"{Line}:{Column}"
            : "unknown";
}
