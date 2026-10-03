using ArSclStudio.Scl.Documents;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Editing;

if (args.Length == 5 && args[0] == "set-description")
{
    await using var session = new SclDocumentSession();
    var opened = await session.OpenFileAsync(args[1]);
    if (!opened.Succeeded || opened.State is null)
    {
        foreach (var diagnostic in opened.Diagnostics) { Console.Error.WriteLine(diagnostic.Message); }
        return 1;
    }

    var ieds = opened.State.TopLevelIndex.Ieds.Where(ied => ied.Name == args[2]).ToArray();
    if (ieds.Length != 1)
    {
        Console.Error.WriteLine("IED name must resolve to exactly one IED.");
        return 1;
    }

    opened.State.Syntax.TryGetAttributeValue(ieds[0].Handle, "desc", out var before);
    var edit = await session.ExecuteAsync(new SetIedDescriptionCommand(ieds[0].Handle, before, args[3]),
        session.CurrentRevision);
    if (!edit.Succeeded) { Console.Error.WriteLine(edit.Message); return 1; }
    var saved = await session.SaveAsync(session.CurrentRevision, args[4]);
    Console.WriteLine(saved.Message);
    return saved.Succeeded ? 0 : 1;
}

if (args.Length != 2 || !string.Equals(args[0], "probe", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("ARSCL Studio CLI");
    Console.WriteLine("Usage: arscl probe <file.icd|iid|cid|scd|ssd|sed>");
    Console.WriteLine("       arscl set-description <input.scd> <IED-name> <description> <output.scd>");
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

