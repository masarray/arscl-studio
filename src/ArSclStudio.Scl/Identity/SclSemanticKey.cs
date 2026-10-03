namespace ArSclStudio.Scl.Identity;

public readonly record struct SclSemanticKey
{
    public SclSemanticKey(string canonicalValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalValue);
        CanonicalValue = canonicalValue;
    }

    public string CanonicalValue { get; }

    public override string ToString() => CanonicalValue;
}
