using System.Text;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class DeepLazyProjectionRegressionTests
{
    [TestMethod]
    [Timeout(20_000)]
    public async Task CollapsedDeepModelDoesNotMaterializeAllSemanticRows()
    {
        const int logicalDeviceCount = 100;
        const int logicalNodesPerDevice = 100;

        var builder = new StringBuilder(1_000_000);
        builder.Append(
            "<SCL xmlns=\"http://www.iec.ch/61850/2003/SCL\">" +
            "<IED name=\"Synthetic\"><AccessPoint name=\"P1\"><Server>");

        for (var ld = 0; ld < logicalDeviceCount; ld++)
        {
            builder
                .Append("<LDevice inst=\"LD")
                .Append(ld)
                .Append("\"><LN0 lnClass=\"LLN0\" inst=\"\"/>");

            for (var ln = 0; ln < logicalNodesPerDevice; ln++)
            {
                builder
                    .Append("<LN lnClass=\"GGIO\" inst=\"")
                    .Append(ln)
                    .Append("\"/>");
            }

            builder.Append("</LDevice>");
        }

        builder.Append(
            "</Server></AccessPoint></IED></SCL>");

        var path = Path.Combine(
            Path.GetTempPath(),
            string.Concat(Guid.NewGuid().ToString("N"), ".scd"));

        await File.WriteAllTextAsync(path, builder.ToString());

        try
        {
            await using var session = new SclDocumentSession();
            var result = await session.OpenFileAsync(path);
            Assert.IsNotNull(result.State);

            Assert.IsTrue(
                result.State.SemanticIndex.NodeCount >
                logicalDeviceCount * logicalNodesPerDevice);

            var expanded = new HashSet<SclNodeHandle>
            {
                result.State.Syntax.RootHandle
            };

            var visible = SclExplorerProjector.BuildEngineering(
                result.State,
                expanded);

            Assert.IsTrue(
                visible.Count < 10,
                $"Collapsed model unexpectedly materialized {visible.Count} rows.");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
