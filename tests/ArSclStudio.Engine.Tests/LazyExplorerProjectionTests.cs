using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class LazyExplorerProjectionTests
{
    [TestMethod]
    public async Task XmlProjectionOnlyMaterializesExpandedBranches()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="Relay_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="" />
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

            var state = result.State;
            var expanded = new HashSet<SclNodeHandle>
            {
                state.Syntax.RootHandle
            };

            var rootOnly = SclExplorerProjector.BuildXmlVisible(
                state,
                expanded);

            var ied = FindSemanticNode(
                state,
                SclSemanticKind.Ied);

            Assert.IsTrue(rootOnly.Any(row => row.Handle == ied.Handle));

            var accessPoint = FindSemanticNode(
                state,
                SclSemanticKind.AccessPoint);

            Assert.IsFalse(
                rootOnly.Any(row => row.Handle == accessPoint.Handle));

            expanded.Add(ied.Handle);

            var iedExpanded = SclExplorerProjector.BuildXmlVisible(
                state,
                expanded);

            Assert.IsTrue(
                iedExpanded.Any(row => row.Handle == accessPoint.Handle));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task EngineeringProjectionUsesDeepSemanticHierarchyLazily()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="Relay_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="Events">
                          <FCDA ldInst="LD0" lnClass="LLN0" doName="Beh" fc="ST" />
                        </DataSet>
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

            var state = result.State;
            var ied = FindSemanticNode(state, SclSemanticKind.Ied);
            var accessPoint = FindSemanticNode(state, SclSemanticKind.AccessPoint);
            var dataSet = FindSemanticNode(state, SclSemanticKind.DataSet);

            var expanded = new HashSet<SclNodeHandle>
            {
                state.Syntax.RootHandle
            };

            var initial = SclExplorerProjector.BuildEngineering(
                state,
                expanded);

            Assert.IsTrue(initial.Any(row => row.Handle == ied.Handle));
            Assert.IsFalse(initial.Any(row => row.Handle == accessPoint.Handle));
            Assert.IsFalse(initial.Any(row => row.Handle == dataSet.Handle));

            expanded.Add(ied.Handle);
            expanded.Add(accessPoint.Handle);

            var partial = SclExplorerProjector.BuildEngineering(
                state,
                expanded);

            Assert.IsTrue(partial.Any(row => row.Handle == accessPoint.Handle));
            Assert.IsFalse(partial.Any(row => row.Handle == dataSet.Handle));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static SclSemanticNode FindSemanticNode(
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

        Assert.Fail($"Semantic node {kind} was not found.");
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
