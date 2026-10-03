namespace ArSclStudio.Scl.Xml;

public sealed record SecureXmlReaderOptions
{
    public const long DefaultMaxCharactersInDocument = 536_870_912;

    public long MaxCharactersInDocument { get; init; } = DefaultMaxCharactersInDocument;

    public bool Async { get; init; } = true;
}
