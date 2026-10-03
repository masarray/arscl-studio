using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Editing;
using ArSclStudio.Engine.Workers;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class SclTransactionTests
{
    [TestMethod]
    public async Task EditUndoRedoPreservesPublishedSnapshotAndReferenceIndex()
    {
        await using var f = await EditingFixture.CreateAsync();
        var original = f.State;
        var xml = original.Syntax.CreateXmlSnapshot();
        Assert.AreEqual(SclEditStatus.Committed, (await f.EditAsync("Feeder A")).Status);
        Assert.AreEqual(xml, original.Syntax.CreateXmlSnapshot());
        Assert.AreSame(original.SemanticIndex, f.State.SemanticIndex);
        Assert.AreEqual("Feeder A", f.Description(f.FirstIed));
        Assert.IsTrue(f.Session.IsDirty);
        Assert.IsTrue(f.Session.CanUndo);
        var editedRevision = f.Session.CurrentRevision;
        Assert.IsTrue((await f.Session.UndoAsync(editedRevision)).Succeeded);
        Assert.AreEqual(xml, f.State.Syntax.CreateXmlSnapshot());
        Assert.IsFalse(f.Session.IsDirty);
        Assert.IsTrue(f.Session.CurrentRevision.Value > editedRevision.Value);
        Assert.IsTrue((await f.Session.RedoAsync(f.Session.CurrentRevision)).Succeeded);
        Assert.AreEqual("Feeder A", f.Description(f.FirstIed));
        Assert.AreEqual(3, f.Session.ChangeJournal.Count);
    }

    [TestMethod]
    public async Task CompoundEditHasOneRevisionAndOneUndoEntry()
    {
        await using var f = await EditingFixture.CreateAsync();
        var second = f.State.TopLevelIndex.Ieds[1].Handle;
        var revision = f.Session.CurrentRevision;
        var result = await f.Session.ExecuteAsync(new CompoundEditCommand("Describe two IEDs",
            new SetIedDescriptionCommand(f.FirstIed, "Original", "A"),
            new SetIedDescriptionCommand(second, null, "B")), revision);
        Assert.AreEqual(SclEditStatus.Committed, result.Status);
        Assert.AreEqual(revision.Value + 1, result.Revision.Value);
        Assert.AreEqual(2, f.Session.ChangeJournal[0].Changes.Count);
        Assert.IsTrue((await f.Session.UndoAsync(result.Revision)).Succeeded);
        Assert.AreEqual("Original", f.Description(f.FirstIed));
        Assert.IsNull(f.Description(second));
        Assert.IsFalse(f.Session.CanUndo);
    }

    [TestMethod]
    public async Task InvalidCompoundPreconditionDoesNotPartiallyApply()
    {
        await using var f = await EditingFixture.CreateAsync();
        var original = f.State;
        var result = await f.Session.ExecuteAsync(new CompoundEditCommand("Invalid compound",
            new SetIedDescriptionCommand(f.FirstIed, "Original", "A"),
            new SetIedDescriptionCommand(f.State.TopLevelIndex.Ieds[1].Handle, "wrong", "B")),
            f.Session.CurrentRevision);
        Assert.AreEqual(SclEditStatus.Rejected, result.Status);
        Assert.AreSame(original, f.State);
        Assert.IsFalse(f.Session.CanUndo);
        Assert.AreEqual(0, f.Session.ChangeJournal.Count);
    }

    [TestMethod]
    public async Task ValidationFailureAfterMutationDiscardsCandidate()
    {
        var validator = new RejectValidator();
        await using var f = await EditingFixture.CreateAsync(validator);
        var original = f.State;
        var result = await f.EditAsync("Rejected");
        Assert.IsTrue(validator.SawChangedValue);
        Assert.AreEqual(SclEditStatus.Rejected, result.Status);
        Assert.AreSame(original, f.State);
        Assert.IsFalse(f.Session.IsDirty);
        Assert.IsFalse(f.Session.CanUndo);
    }

    [TestMethod]
    public async Task NewEditInvalidatesRedoButNoOpDoesNot()
    {
        await using var f = await EditingFixture.CreateAsync();
        await f.EditAsync("A");
        await f.Session.UndoAsync(f.Session.CurrentRevision);
        var revision = f.Session.CurrentRevision;
        Assert.AreEqual(SclEditStatus.NoChange, (await f.EditAsync("Original")).Status);
        Assert.AreEqual(revision, f.Session.CurrentRevision);
        Assert.IsTrue(f.Session.CanRedo);
        await f.EditAsync("B");
        Assert.IsFalse(f.Session.CanRedo);
        Assert.AreEqual("B", f.Description(f.FirstIed));
    }

    [TestMethod]
    public async Task NullAndEmptyDescriptionsRemainDistinctAndUndoable()
    {
        await using var f = await EditingFixture.CreateAsync();
        await f.EditAsync(null);
        Assert.IsNull(f.Description(f.FirstIed));
        await f.EditAsync("");
        Assert.AreEqual("", f.Description(f.FirstIed));
        await f.Session.UndoAsync(f.Session.CurrentRevision);
        Assert.IsNull(f.Description(f.FirstIed));
        await f.Session.UndoAsync(f.Session.CurrentRevision);
        Assert.AreEqual("Original", f.Description(f.FirstIed));
        StringAssert.Contains(f.State.Syntax.CreateXmlSnapshot(), "v:desc=\"Vendor description\"");
    }

    [TestMethod]
    public async Task PolicyRejectsUnsupportedTargetsNamespacesInvalidCharactersAndOversize()
    {
        await using var f = await EditingFixture.CreateAsync();
        var rootEdit = await f.Session.ExecuteAsync(new SetIedDescriptionCommand(f.State.Syntax.RootHandle, null, "X"), f.Session.CurrentRevision);
        Assert.AreEqual(SclEditStatus.Rejected, rootEdit.Status);
        Assert.AreEqual(SclEditStatus.Rejected, (await f.EditAsync("bad\u0001")).Status);
        Assert.AreEqual(SclEditStatus.Rejected, (await f.EditAsync(new string('x', 4097))).Status);
        await using var vendor = await EditingFixture.CreateAsync(xml: "<SCL xmlns=\"urn:vendor\"><IED name=\"A\"/></SCL>");
        Assert.IsFalse(SclEditPolicy.CanEditDescription(vendor.State, vendor.FirstIed));
    }

    [TestMethod]
    public async Task StaleRevisionAndDuplicateTargetsAreRejected()
    {
        await using var f = await EditingFixture.CreateAsync();
        var revision = f.Session.CurrentRevision;
        await f.EditAsync("A");
        Assert.AreEqual(SclEditStatus.StaleRevision, (await f.Session.UndoAsync(revision)).Status);
        Assert.AreEqual(SclEditStatus.Rejected, (await f.Session.ExecuteAsync(new CompoundEditCommand("Duplicate",
            new SetIedDescriptionCommand(f.FirstIed, "A", "B"),
            new SetIedDescriptionCommand(f.FirstIed, "A", "C")), f.Session.CurrentRevision)).Status);
        Assert.AreEqual("A", f.Description(f.FirstIed));
    }

    [TestMethod]
    public async Task ConcurrentEditsCannotBothCommitAgainstSameRevision()
    {
        await using var f = await EditingFixture.CreateAsync();
        var revision = f.Session.CurrentRevision;
        var first = f.Session.ExecuteAsync(new SetIedDescriptionCommand(f.FirstIed, "Original", "A"), revision);
        var second = f.Session.ExecuteAsync(new SetIedDescriptionCommand(f.FirstIed, "Original", "B"), revision);
        var results = await Task.WhenAll(first, second);
        Assert.AreEqual(1, results.Count(r => r.Status == SclEditStatus.Committed));
        Assert.AreEqual(1, results.Count(r => r.Status == SclEditStatus.StaleRevision));
    }

    [TestMethod]
    [Timeout(15_000)]
    public async Task CancellationAfterStagingPreservesState()
    {
        using var validator = new BlockingValidator();
        await using var f = await EditingFixture.CreateAsync(validator);
        using var cancellation = new CancellationTokenSource();
        var original = f.State;
        var edit = f.Session.ExecuteAsync(new SetIedDescriptionCommand(f.FirstIed, "Original", "A"),
            f.Session.CurrentRevision, cancellation.Token);
        await validator.Started.Task;
        cancellation.Cancel();
        Assert.AreEqual(SclEditStatus.Cancelled, (await edit).Status);
        Assert.AreSame(original, f.State);
    }

    [TestMethod]
    [Timeout(15_000)]
    public async Task OpenPreparedDuringEditCannotOverwriteCommittedEdit()
    {
        using var validator = new BlockingValidator();
        await using var f = await EditingFixture.CreateAsync(validator);
        var edit = f.EditAsync("A");
        await validator.Started.Task;
        var open = f.Session.OpenFileAsync(f.Path);
        // Parse work is submitted before OpenFileAsync yields; drain its single queue slot.
        await f.Session.RunLatestAsync(WorkKind.Index, _ => Task.FromResult(0));
        validator.Release.Set();
        Assert.IsTrue((await edit).Succeeded);
        Assert.AreEqual(SclOpenStatus.Superseded, (await open).Status);
        Assert.AreEqual("A", f.Description(f.FirstIed));
    }

    [TestMethod]
    [Timeout(15_000)]
    public async Task DisposeCancelsWriterAndDrainsQueuedOperations()
    {
        using var validator = new BlockingValidator();
        await using var f = await EditingFixture.CreateAsync(validator);
        var edit = f.EditAsync("A");
        await validator.Started.Task;
        var queued = f.Session.SaveAsync(f.Session.CurrentRevision);
        await f.Session.DisposeAsync();
        Assert.AreEqual(SclEditStatus.Cancelled, (await edit).Status);
        Assert.AreEqual(SclSaveStatus.Cancelled, (await queued).Status);
        Assert.IsNull(f.Session.CurrentState);
        Assert.AreEqual(0, f.Session.ChangeJournal.Count);
    }

    [TestMethod]
    public async Task HistoryAndRegistryRemainBoundedAcrossRepeatedAddRemove()
    {
        await using var f = await EditingFixture.CreateAsync();
        var count = f.State.Syntax.IndexedNodeCount;
        for (var i = 0; i < 300; i++)
        {
            Assert.IsTrue((await f.EditAsync(i % 2 == 0 ? null : "A")).Succeeded);
        }

        Assert.AreEqual(count, f.State.Syntax.IndexedNodeCount);
        Assert.AreEqual(256, f.Session.ChangeJournal.Count);
        var undoCount = 0;
        while (f.Session.CanUndo)
        {
            await f.Session.UndoAsync(f.Session.CurrentRevision);
            undoCount++;
        }

        Assert.AreEqual(128, undoCount);
    }

    private sealed class RejectValidator : ISclTransactionValidator
    {
        internal bool SawChangedValue { get; private set; }
        public void Validate(SclDocumentState candidate, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            candidate.Syntax.TryGetAttributeValue(candidate.TopLevelIndex.Ieds[0].Handle, "desc", out var value);
            SawChangedValue = value == "Rejected";
            throw new InvalidOperationException("Injected post-edit validation failure.");
        }
    }

    private sealed class BlockingValidator : ISclTransactionValidator, IDisposable
    {
        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal ManualResetEventSlim Release { get; } = new();
        public void Validate(SclDocumentState candidate, CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            Release.Wait(cancellationToken);
        }
        public void Dispose() => Release.Dispose();
    }
}
