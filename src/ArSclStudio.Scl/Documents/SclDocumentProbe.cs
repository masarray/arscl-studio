using System.Xml;
using ArSclStudio.Scl.Xml;

namespace ArSclStudio.Scl.Documents;

public sealed class SclDocumentProbe
{
    public async ValueTask<SclProbeResult> ProbeFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await ProbeAsync(
            stream,
            Path.GetFileName(path),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<SclProbeResult> ProbeAsync(
        Stream stream,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = SecureXmlReaderFactory.Create(stream);

        cancellationToken.ThrowIfCancellationRequested();
        var nodeType = await reader.MoveToContentAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (nodeType != XmlNodeType.Element ||
            !string.Equals(reader.LocalName, "SCL", StringComparison.Ordinal))
        {
            return new SclProbeResult(
                false,
                SclFileKind.Unknown,
                reader.NamespaceURI ?? string.Empty,
                default,
                null);
        }

        var rootNamespace = reader.NamespaceURI ?? string.Empty;
        var revision = new SclSchemaRevision(
            reader.GetAttribute("version"),
            reader.GetAttribute("revision"),
            reader.GetAttribute("release"));

        string? headerId = null;

        if (!reader.IsEmptyElement)
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == XmlNodeType.Element &&
                    reader.Depth == 1 &&
                    string.Equals(reader.LocalName, "Header", StringComparison.Ordinal))
                {
                    headerId = reader.GetAttribute("id");
                    break;
                }

                if (reader.NodeType == XmlNodeType.EndElement &&
                    reader.Depth == 0)
                {
                    break;
                }
            }
        }

        return new SclProbeResult(
            true,
            ClassifyFromFileName(fileName),
            rootNamespace,
            revision,
            headerId);
    }

    public static SclFileKind ClassifyFromFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return SclFileKind.Unknown;
        }

        return Path.GetExtension(fileName).ToUpperInvariant() switch
        {
            ".ICD" => SclFileKind.Icd,
            ".IID" => SclFileKind.Iid,
            ".CID" => SclFileKind.Cid,
            ".SCD" => SclFileKind.Scd,
            ".SSD" => SclFileKind.Ssd,
            ".SED" => SclFileKind.Sed,
            _ => SclFileKind.Unknown
        };
    }
}
