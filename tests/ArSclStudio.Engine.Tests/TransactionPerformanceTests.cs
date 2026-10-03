using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Editing;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class TransactionPerformanceTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [Timeout(90_000)]
    public async Task HundredThousandAttributesStageOnceAndKeepIndexesAndTreeLazy()
    {
        var xml = new StringBuilder(5_000_000);
        xml.Append("<SCL xmlns=\"http://www.iec.ch/61850/2003/SCL\"><IED name=\"Large\" desc=\"Original\"><AccessPoint name=\"P1\"><Server><LDevice inst=\"LD0\">");
        for (var ln = 0; ln < 1000; ln++)
        {
            xml.Append("<LN lnClass=\"GGIO\" inst=\"").Append(ln).Append("\"><DOI name=\"Signals\">");
            for (var da = 0; da < 100; da++)
            {
                xml.Append("<DAI name=\"v").Append(da).Append("\"/>");
            }
            xml.Append("</DOI></LN>");
        }
        xml.Append("</LDevice></Server></AccessPoint></IED></SCL>");
        await using var f = await EditingFixture.CreateAsync(xml: xml.ToString());
        var semantic = f.State.SemanticIndex;
        var allocationBefore = GC.GetTotalAllocatedBytes(precise: true);
        var timer = Stopwatch.StartNew();
        var result = await f.EditAsync("Large station");
        timer.Stop();
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocationBefore;
        TestContext.WriteLine($"M2A 100k DAI: edit={timer.Elapsed.TotalMilliseconds:F1} ms; allocated={allocated:N0} bytes; nodes={f.State.Syntax.IndexedNodeCount:N0}");
        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.AreSame(semantic, f.State.SemanticIndex);
        Assert.IsTrue(timer.Elapsed < TimeSpan.FromSeconds(30));
        Assert.IsTrue(allocated < 512L * 1024 * 1024, $"Staging allocated {allocated:N0} bytes.");
        var rows = SclExplorerProjector.BuildEngineering(f.State, []);
        Assert.IsTrue(rows.Count < 10);
        Assert.IsTrue((await f.Session.UndoAsync(f.Session.CurrentRevision)).Succeeded);
    }

    [TestMethod]
    public async Task PatchHistoryDoesNotRetainOldSyntaxSnapshots()
    {
        await using var f = await EditingFixture.CreateAsync();
        var weak = EditAndCapturePrevious(f.Session);
        Collect();
        Assert.IsFalse(HasTarget(weak), "Undo history retained an entire old syntax document.");
        Assert.IsTrue(f.Session.CanUndo);
        Assert.IsTrue((await f.Session.UndoAsync(f.Session.CurrentRevision)).Succeeded);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<SclSyntaxDocument> EditAndCapturePrevious(SclDocumentSession session)
    {
        var state = session.CurrentState!;
        var weak = new WeakReference<SclSyntaxDocument>(state.Syntax);
        var result = session.ExecuteAsync(new SetIedDescriptionCommand(state.TopLevelIndex.Ieds[0].Handle,
            "Original", "New"), state.Revision).GetAwaiter().GetResult();
        Assert.IsTrue(result.Succeeded);
        return weak;
    }

    private static void Collect()
    {
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool HasTarget(WeakReference<SclSyntaxDocument> weak) => weak.TryGetTarget(out _);
}
