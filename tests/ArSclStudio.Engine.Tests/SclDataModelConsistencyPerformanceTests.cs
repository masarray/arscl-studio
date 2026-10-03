using System.Diagnostics;
using System.Text;
using ArSclStudio.Engine.Diagnostics;
using ArSclStudio.Engine.Documents;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class SclDataModelConsistencyPerformanceTests
{
    [TestMethod]
    [Timeout(30_000)]
    public async Task SharedTypeContextsValidateLargeInstanceSetWithinRegressionBudget()
    {
        const int logicalNodeCount = 10_000;

        var builder = new StringBuilder(
            logicalNodeCount * 150);

        builder.Append(
            "<SCL xmlns=\"http://www.iec.ch/61850/2003/SCL\">" +
            "<IED name=\"Synthetic\">" +
            "<AccessPoint name=\"P1\"><Server>" +
            "<LDevice inst=\"LD0\">");

        for (var i = 0; i < logicalNodeCount; i++)
        {
            builder
                .Append("<LN lnClass=\"GGIO\" inst=\"")
                .Append(i)
                .Append("\" lnType=\"LT_GGIO\">")
                .Append("<DOI name=\"Ind1\">")
                .Append("<DAI name=\"stVal\"><Val>true</Val></DAI>")
                .Append("</DOI></LN>");
        }

        builder.Append(
            "</LDevice></Server></AccessPoint></IED>" +
            "<DataTypeTemplates>" +
            "<LNodeType id=\"LT_GGIO\" lnClass=\"GGIO\">" +
            "<DO name=\"Ind1\" type=\"DOT_SPS\"/>" +
            "</LNodeType>" +
            "<DOType id=\"DOT_SPS\" cdc=\"SPS\">" +
            "<DA name=\"stVal\" fc=\"ST\" bType=\"BOOLEAN\"/>" +
            "</DOType>" +
            "</DataTypeTemplates></SCL>");

        var path = Path.Combine(
            Path.GetTempPath(),
            string.Concat(
                Guid.NewGuid().ToString("N"),
                ".scd"));

        await File.WriteAllTextAsync(
            path,
            builder.ToString());

        try
        {
            await using var session =
                new SclDocumentSession();

            var open = await session.OpenFileAsync(path);
            Assert.IsTrue(open.Succeeded);
            Assert.IsNotNull(open.State);

            var stopwatch = Stopwatch.StartNew();
            var result = await session.ValidateFastAsync();
            stopwatch.Stop();

            Assert.IsNotNull(result.Value);
            Assert.IsFalse(result.Value.Diagnostics.Any(
                diagnostic =>
                    diagnostic.Domain ==
                        DiagnosticDomain.Semantic));

            Assert.IsTrue(
                stopwatch.Elapsed <
                    TimeSpan.FromSeconds(10),
                $"Data Model consistency validation regression: {stopwatch.Elapsed} for {logicalNodeCount} logical nodes.");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
