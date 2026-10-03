using System.Xml;

namespace ArSclStudio.Scl.Xml;

public static class SecureXmlReaderFactory
{
    public static XmlReader Create(
        Stream stream,
        SecureXmlReaderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options ??= new SecureXmlReaderOptions();

        if (options.MaxCharactersInDocument <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "MaxCharactersInDocument must be greater than zero.");
        }

        var settings = new XmlReaderSettings
        {
            Async = options.Async,
            CloseInput = false,
            ConformanceLevel = ConformanceLevel.Document,
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = false,
            IgnoreProcessingInstructions = false,
            IgnoreWhitespace = false,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = options.MaxCharactersInDocument,
            XmlResolver = null
        };

        return XmlReader.Create(stream, settings);
    }
}
