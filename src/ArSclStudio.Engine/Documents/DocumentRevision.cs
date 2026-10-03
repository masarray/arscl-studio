namespace ArSclStudio.Engine.Documents;

public readonly record struct DocumentRevision(long Value) : IComparable<DocumentRevision>
{
    public int CompareTo(DocumentRevision other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
