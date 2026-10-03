using ArSclStudio.Scl.Documents;

if (args.Length != 2 || !string.Equals(args[0], "probe", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("ARSCL Studio CLI");
    Console.WriteLine("Usage: arscl probe <file.icd|iid|cid|scd|ssd|sed>");
    return 2;
}

try
{
    var probe = new SclDocumentProbe();
    var result = await probe.ProbeFileAsync(args[1]).ConfigureAwait(false);

    Console.WriteLine($"SCL: {result.IsScl}");
    Console.WriteLine($"Kind hint: {result.FileKindHint}");
    Console.WriteLine($"Namespace: {result.RootNamespace}");
    Console.WriteLine($"Schema revision: {result.SchemaRevision}");
    Console.WriteLine($"Header id: {result.HeaderId ?? "(none)"}");

    return result.IsScl ? 0 : 1;
}
catch (Exception exception) when (
    exception is IOException or
    UnauthorizedAccessException or
    System.Xml.XmlException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
