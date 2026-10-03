using System.Diagnostics;
using System.Text;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class LargeExplorerProjectionTests
{
    [TestMethod]
    [Timeout(20_000)]
    public async Task LargeTopLevelIedSetBuildsWithinRegressionBudget()
    {
        const int iedCount = 5_000;
        var builder = new StringBuilder(iedCount * 64);

        builder.Append("<SCL xmlns=\"http://www.iec.ch/61850/2003/SCL\"><Header id=\"Perf\"/>");

        for (var i = 0; i < iedCount; i++)
        {
            builder
                .Append("<IED name=\"IED_")
                .Append(i)
                .Append("\" manufacturer=\"Synthetic\"/>");
        }

        builder.Append("<DataTypeTemplates/></SCL>");

        var path = Path.Combine(
            Path.GetTempPath(),
            string.Concat(Guid.NewGuid().ToString("N"), ".scd"));

        await File.WriteAllTextAsync(path, builder.ToString());

        try
        {
            var stopwatch = Stopwatch.StartNew();

            await using var session = new SclDocumentSession();
            var result = await session.OpenFileAsync(path);

            Assert.IsNotNull(result.State);
            var rows = SclExplorerProjector.BuildEngineering(result.State);

            stopwatch.Stop();

            Assert.IsTrue(rows.Count >= iedCount);
            Assert.IsTrue(
                stopwatch.Elapsed < TimeSpan.FromSeconds(10),
                $"Large projection regression: {stopwatch.Elapsed}.");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
