namespace ArSclStudio.Engine.Documents;

public readonly record struct DocumentRevision(long Value)
{
    public override string ToString() =>
        Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
