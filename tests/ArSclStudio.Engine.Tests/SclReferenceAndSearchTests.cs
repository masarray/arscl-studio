using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Engine.Search;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class SclReferenceAndSearchTests
{
    [TestMethod]
    public async Task WhereUsedProjectsIncomingDataSetReferences()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="Relay_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="Events" />
                        <ReportControl name="BRCB01" datSet="Events" />
                        <GSEControl name="GO1" datSet="Events" />
                      </LN0>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
            </SCL>
            """);

        try
        {
            await using var session = new SclDocumentSession();
            var result = await session.OpenFileAsync(path);
            Assert.IsNotNull(result.State);

            var dataSet = FindNode(
                result.State,
                SclSemanticKind.DataSet);

            var whereUsed = SclReferenceProjector.BuildWhereUsed(
                result.State,
                dataSet.Handle);

            Assert.AreEqual(2, whereUsed.Length);
            Assert.IsTrue(
                whereUsed.Any(row =>
                    row.SourceLabel == "BRCB01" &&
                    row.Kind == SclReferenceKind.DataSetBinding));
            Assert.IsTrue(
                whereUsed.Any(row =>
                    row.SourceLabel == "GO1" &&
                    row.Kind == SclReferenceKind.DataSetBinding));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task SemanticSearchFindsDeepNodesWithoutUiTraversal()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="Relay_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="Protection">
                      <LN lnClass="XCBR" inst="1" lnType="XCBR_TYPE">
                        <DOI name="Pos" />
                      </LN>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
              <DataTypeTemplates>
                <LNodeType id="XCBR_TYPE" lnClass="XCBR" />
              </DataTypeTemplates>
            </SCL>
            """);

        try
        {
            await using var session = new SclDocumentSession();
            var result = await session.OpenFileAsync(path);
            Assert.IsNotNull(result.State);

            var matches = SclSemanticSearch.Search(
                result.State,
                "XCBR1",
                maximumResults: 50);

            Assert.AreEqual(1, matches.Length);
            Assert.AreEqual("XCBR1", matches[0].Name);
            StringAssert.Contains(matches[0].Path, "LN[XCBR1]");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static SclSemanticNode FindNode(
        SclDocumentState state,
        SclSemanticKind kind)
    {
        foreach (var node in state.SemanticIndex.Nodes)
        {
            if (node.Kind == kind)
            {
                return node;
            }
        }

        Assert.Fail($"Node {kind} was not found.");
        throw new InvalidOperationException();
    }

    private static async Task<string> CreateTempFileAsync(string content)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            string.Concat(Guid.NewGuid().ToString("N"), ".scd"));

        await File.WriteAllTextAsync(path, content);
        return path;
    }
}
