using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Editing;
using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Engine.Tests;

internal sealed class EditingFixture : IAsyncDisposable
{
    internal const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <?vendor preserve?>
        <SCL xmlns="http://www.iec.ch/61850/2003/SCL" xmlns:v="urn:vendor" version="2007" revision="B">
          <!-- commissioning note -->
          <IED name="Relay_A" desc="Original" v:desc="Vendor description">
            <Private type="vendor"><v:data key="a &amp; b"><![CDATA[keep <raw>]]></v:data></Private>
            <AccessPoint name="P1"><Server><LDevice inst="LD0">
              <LN0 lnClass="LLN0" inst="" lnType="LT"><DataSet name="Events"/>
                <ReportControl name="BRCB01" datSet="Events"/></LN0>
            </LDevice></Server></AccessPoint>
          </IED>
          <IED name="Relay_B" />
          <DataTypeTemplates><LNodeType id="LT" lnClass="LLN0"/></DataTypeTemplates>
        </SCL>
        """;

    private EditingFixture(string directory, SclDocumentSession session)
    {
        DirectoryPath = directory;
        Session = session;
    }

    internal string DirectoryPath { get; }
    internal string Path => System.IO.Path.Combine(DirectoryPath, "station.scd");
    internal SclDocumentSession Session { get; }
    internal SclDocumentState State => Session.CurrentState!;
    internal SclNodeHandle FirstIed => State.TopLevelIndex.Ieds[0].Handle;

    internal static async Task<EditingFixture> CreateAsync(ISclTransactionValidator? validator = null,
        string? xml = null)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "arscl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var fixture = new EditingFixture(directory, new SclDocumentSession(transactionValidator: validator));
        await File.WriteAllTextAsync(fixture.Path, xml ?? Xml);
        var opened = await fixture.Session.OpenFileAsync(fixture.Path);
        Assert.IsTrue(opened.Succeeded);
        return fixture;
    }

    internal string? Description(SclNodeHandle handle)
    {
        State.Syntax.TryGetAttributeValue(handle, "desc", out var value);
        return value;
    }

    internal Task<SclEditResult> EditAsync(string? value) => Session.ExecuteAsync(
        new SetIedDescriptionCommand(FirstIed, Description(FirstIed), value), Session.CurrentRevision);

    public async ValueTask DisposeAsync()
    {
        await Session.DisposeAsync();
        Directory.Delete(DirectoryPath, recursive: true);
    }
}
