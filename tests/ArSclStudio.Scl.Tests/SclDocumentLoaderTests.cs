using System.Text;
using ArSclStudio.Scl.Semantics;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Scl.Tests;

[TestClass]
public sealed class SclDocumentLoaderTests
{
    [TestMethod]
    public async Task LoadAsyncPreservesVendorElementsCommentsAndPrefixes()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL"
                 xmlns:vendor="urn:example:vendor"
                 version="2007"
                 revision="B"
                 release="4">
              <!-- keep this engineering note -->
              <Header id="Station-A" />
              <IED name="Relay_A" manufacturer="Example">
                <Private type="vendor-extension">
                  <vendor:Config vendor:mode="special">value</vendor:Config>
                </Private>
              </IED>
            </SCL>
            """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var document = await new SclDocumentLoader()
            .LoadAsync(stream, "station.scd");

        var outerXml = document.Document.OuterXml;

        StringAssert.Contains(outerXml, "keep this engineering note");
        StringAssert.Contains(outerXml, "vendor:Config");
        StringAssert.Contains(outerXml, "vendor:mode");
        StringAssert.Contains(outerXml, "vendor-extension");
        Assert.IsTrue(document.IndexedNodeCount > 0);
        Assert.IsTrue(document.GetSourceSpan(document.RootHandle).IsKnown);
    }

    [TestMethod]
    public async Task TopLevelIndexerFindsIedsAndMajorSections()
    {
        const string xml = """
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Header id="Station-A" />
              <Substation name="S1" />
              <Communication />
              <IED name="Relay_A" manufacturer="Example" />
              <IED name="Gateway_B" />
              <DataTypeTemplates />
              <Private type="tool" />
            </SCL>
            """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var document = await new SclDocumentLoader()
            .LoadAsync(stream, "station.scd");

        var index = SclTopLevelIndexer.Build(document);

        Assert.IsFalse(index.Header.IsNone);
        Assert.HasCount(1, index.Substations);
        Assert.HasCount(1, index.Communications);
        Assert.HasCount(2, index.Ieds);
        Assert.AreEqual("Relay_A", index.Ieds[0].Name);
        Assert.AreEqual("Example", index.Ieds[0].Manufacturer);
        Assert.IsFalse(index.DataTypeTemplates.IsNone);
        Assert.HasCount(1, index.PrivateElements);
    }

    [TestMethod]
    public async Task SelectableNodeHandlesRoundTripToNodeInfo()
    {
        const string xml = """
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Header id="Station-A" />
            </SCL>
            """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var document = await new SclDocumentLoader()
            .LoadAsync(stream, "station.scd");

        Assert.IsTrue(
            document.TryGetNodeInfo(
                document.RootHandle,
                out var rootInfo));

        Assert.IsNotNull(rootInfo);
        Assert.AreEqual("SCL", rootInfo.LocalName);

        var children = document.GetSelectableChildren(document.RootHandle);
        Assert.IsTrue(children.Count >= 1);
    }
}
