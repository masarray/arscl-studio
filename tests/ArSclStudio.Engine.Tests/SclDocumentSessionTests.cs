using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Workers;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class SclDocumentSessionTests
{
    [TestMethod]
    public void AdvanceRevisionIsMonotonic()
    {
        var session = new SclDocumentSession(maxWorkerConcurrency: 1);

        var first = session.AdvanceRevision();
        var second = session.AdvanceRevision();

        Assert.IsTrue(second.Value > first.Value);
        session.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [TestMethod]
    public async Task RunLatestAsyncSupersedesPreviousRequestOfSameKind()
    {
        await using var session = new SclDocumentSession(maxWorkerConcurrency: 1);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = session.RunLatestAsync(
            WorkKind.Search,
            async cancellationToken =>
            {
                firstStarted.TrySetResult();
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                return "first";
            });

        await firstStarted.Task;

        var second = session.RunLatestAsync(
            WorkKind.Search,
            _ => Task.FromResult("second"));

        var firstResult = await first;
        var secondResult = await second;

        Assert.AreEqual(WorkResultStatus.Superseded, firstResult.Status);
        Assert.AreEqual(WorkResultStatus.Published, secondResult.Status);
        Assert.AreEqual("second", secondResult.Value);
    }

    [TestMethod]
    public async Task RunLatestAsyncRejectsResultFromOldRevision()
    {
        await using var session = new SclDocumentSession(maxWorkerConcurrency: 1);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueWork = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var work = session.RunLatestAsync(
            WorkKind.ValidateFast,
            async _ =>
            {
                started.TrySetResult();
                await continueWork.Task;
                return 42;
            });

        await started.Task;
        session.AdvanceRevision();
        continueWork.TrySetResult();

        var result = await work;

        Assert.AreEqual(WorkResultStatus.StaleRevision, result.Status);
    }
}
