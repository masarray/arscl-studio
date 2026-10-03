using System.Text;
using System.Xml;
using ArSclStudio.Scl.Documents;

namespace ArSclStudio.Scl.Tests;

[TestClass]
public sealed class SclDocumentProbeTests
{
    [TestMethod]
    public async Task ProbeAsyncRecognizesSclAndFileRoleHint()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL" version="2007" revision="B" release="4">
              <Header id="Station-A" />
            </SCL>
            """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var result = await new SclDocumentProbe()
            .ProbeAsync(stream, "station.scd");

        Assert.IsTrue(result.IsScl);
        Assert.AreEqual(SclFileKind.Scd, result.FileKindHint);
        Assert.AreEqual("Station-A", result.HeaderId);
        Assert.AreEqual("2007", result.SchemaRevision.Version);
        Assert.AreEqual("B", result.SchemaRevision.Revision);
        Assert.AreEqual("4", result.SchemaRevision.Release);
    }

    [TestMethod]
    public async Task ProbeAsyncRejectsDtd()
    {
        const string xml = """
            <?xml version="1.0"?>
            <!DOCTYPE SCL [<!ENTITY x "unsafe">]>
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Header id="&x;" />
            </SCL>
            """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        try
        {
            await new SclDocumentProbe().ProbeAsync(stream, "unsafe.scd");
            Assert.Fail("Expected XmlException because DTD processing is prohibited.");
        }
        catch (XmlException)
        {
            // Expected: secure XML defaults reject DTD input before entity expansion.
        }
    }
}
