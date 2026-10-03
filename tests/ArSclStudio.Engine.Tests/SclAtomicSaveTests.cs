using System.Text;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class SclAtomicSaveTests
{
    [TestMethod]
    public async Task NoEditSavePreservesVendorXmlAndRefreshesStableSourceHandles()
    {
        await using var f = await EditingFixture.CreateAsync();
        var original = f.State.Syntax.CreateXmlSnapshot();
        var handle = f.FirstIed;
        var result = await f.Session.SaveAsync(f.Session.CurrentRevision);
        Assert.IsTrue(result.Succeeded, result.Message);
        var reopened = await new SclDocumentLoader().LoadFileAsync(f.Path);
        Assert.AreEqual(original, reopened.CreateXmlSnapshot());
        Assert.AreEqual(handle, f.FirstIed);
        Assert.IsTrue(f.State.Syntax.GetSourceSpan(handle).IsKnown);
        Assert.IsFalse(f.Session.IsDirty);
        AssertNoTemporaryFiles(f);
    }

    [TestMethod]
    public async Task EditedSaveUndoRedoTracksSavedContentRatherThanRevisionNumber()
    {
        await using var f = await EditingFixture.CreateAsync();
        await f.EditAsync("Feeder <A> & \"backup\"\tline\r\nnext");
        var intended = f.State.Syntax.CreateXmlSnapshot();
        var saved = await f.Session.SaveAsync(f.Session.CurrentRevision);
        Assert.IsTrue(saved.Succeeded, saved.Message);
        var reopened = await new SclDocumentLoader().LoadFileAsync(f.Path);
        Assert.AreEqual(intended, reopened.CreateXmlSnapshot());
        Assert.IsFalse(f.Session.IsDirty);
        await f.Session.UndoAsync(f.Session.CurrentRevision);
        Assert.IsTrue(f.Session.IsDirty);
        await f.Session.RedoAsync(f.Session.CurrentRevision);
        Assert.IsFalse(f.Session.IsDirty);
        AssertNoTemporaryFiles(f);
    }

    [TestMethod]
    public async Task SaveAsNewPathRebindsSourceAndPreservesOriginal()
    {
        await using var f = await EditingFixture.CreateAsync();
        var original = await File.ReadAllBytesAsync(f.Path);
        await f.EditAsync("New");
        var destination = System.IO.Path.Combine(f.DirectoryPath, "copy.scd");
        var saved = await f.Session.SaveAsync(f.Session.CurrentRevision, destination);
        Assert.IsTrue(saved.Succeeded, saved.Message);
        Assert.AreEqual(destination, f.State.SourcePath);
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(f.Path));
        Assert.IsTrue(File.Exists(destination));
        Assert.IsTrue((await f.Session.UndoAsync(f.Session.CurrentRevision)).Succeeded);
        Assert.AreEqual("Original", f.Description(f.FirstIed));
        AssertNoTemporaryFiles(f);
    }

    [TestMethod]
    public async Task ExternalModificationIsNotOverwrittenAndDirtyStateRemains()
    {
        await using var f = await EditingFixture.CreateAsync();
        await f.EditAsync("New");
        const string external = "<SCL><IED name=\"External\"/></SCL>";
        await File.WriteAllTextAsync(f.Path, external);
        var state = f.State;
        var result = await f.Session.SaveAsync(f.Session.CurrentRevision);
        Assert.AreEqual(SclSaveStatus.Failed, result.Status);
        Assert.AreSame(state, f.State);
        Assert.AreEqual(external, await File.ReadAllTextAsync(f.Path));
        Assert.IsTrue(f.Session.IsDirty);
        AssertNoTemporaryFiles(f);
    }

    [TestMethod]
    public async Task SaveAsRequiresExplicitOverwriteAndKeepsFileRole()
    {
        await using var f = await EditingFixture.CreateAsync();
        var destination = System.IO.Path.Combine(f.DirectoryPath, "other.scd");
        await File.WriteAllTextAsync(destination, "keep me");
        var result = await f.Session.SaveAsync(f.Session.CurrentRevision, destination);
        Assert.AreEqual(SclSaveStatus.Failed, result.Status);
        Assert.AreEqual("keep me", await File.ReadAllTextAsync(destination));
        Assert.AreEqual(SclSaveStatus.Failed, (await f.Session.SaveAsync(f.Session.CurrentRevision,
            System.IO.Path.Combine(f.DirectoryPath, "other.cid"))).Status);
        Assert.IsTrue((await f.Session.SaveAsync(f.Session.CurrentRevision, destination, overwrite: true)).Succeeded);
    }

    [TestMethod]
    public async Task StaleCancelledAndIoFailedSavePreserveDestination()
    {
        await using var f = await EditingFixture.CreateAsync();
        var original = await File.ReadAllBytesAsync(f.Path);
        var revision = f.Session.CurrentRevision;
        await f.EditAsync("Changed");
        Assert.AreEqual(SclSaveStatus.StaleRevision, (await f.Session.SaveAsync(revision)).Status);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.AreEqual(SclSaveStatus.Cancelled, (await f.Session.SaveAsync(f.Session.CurrentRevision,
            cancellationToken: cancellation.Token)).Status);
        var impossiblePath = System.IO.Path.Combine(f.DirectoryPath, "missing", "copy.scd");
        Assert.AreEqual(SclSaveStatus.Failed, (await f.Session.SaveAsync(f.Session.CurrentRevision, impossiblePath)).Status);
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(f.Path));
        Assert.IsTrue(f.Session.IsDirty);
        AssertNoTemporaryFiles(f);
    }

    [TestMethod]
    public async Task AtomicMoveFailureDoesNotOverwriteRacingDestinationAndCleansStaging()
    {
        await using var f = await EditingFixture.CreateAsync();
        var destination = System.IO.Path.Combine(f.DirectoryPath, "racing.scd");
        using (var prepared = await SclAtomicWriter.PrepareAsync(f.State, destination, null, CancellationToken.None))
        {
            await File.WriteAllTextAsync(destination, "another writer won");
            Assert.ThrowsExactly<IOException>(prepared.Commit);
            Assert.AreEqual("another writer won", await File.ReadAllTextAsync(destination));
        }

        AssertNoTemporaryFiles(f);
    }

    [TestMethod]
    public async Task VerificationFailureNeverReplacesOriginal()
    {
        await using var f = await EditingFixture.CreateAsync();
        var original = await File.ReadAllBytesAsync(f.Path);
        await using var altered = new MemoryStream(Encoding.UTF8.GetBytes(
            EditingFixture.Xml.Replace("Vendor description", "lost vendor data", StringComparison.Ordinal)));
        var corrupt = await new SclDocumentLoader().LoadAsync(altered, "station.scd");
        Assert.ThrowsExactly<System.Xml.XmlException>(() =>
            f.State.Syntax.VerifyAndRebind(corrupt, CancellationToken.None));
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(f.Path));
        AssertNoTemporaryFiles(f);
    }

    [TestMethod]
    public async Task Utf16BomAndDeclarationFreeFilesRoundTrip()
    {
        await using var f = await EditingFixture.CreateAsync();
        var xml = EditingFixture.Xml.Replace("encoding=\"utf-8\"", "encoding=\"utf-16\"", StringComparison.Ordinal);
        await File.WriteAllTextAsync(f.Path, xml, Encoding.Unicode);
        Assert.IsTrue((await f.Session.OpenFileAsync(f.Path)).Succeeded);
        var saved = await f.Session.SaveAsync(f.Session.CurrentRevision);
        Assert.IsTrue(saved.Succeeded, saved.Message);
        var text = await File.ReadAllTextAsync(f.Path);
        StringAssert.Contains(text, "encoding=\"utf-8\"");
        StringAssert.Contains(text, "<![CDATA[keep <raw>]]>");

        await File.WriteAllTextAsync(f.Path, "<SCL xmlns=\"http://www.iec.ch/61850/2003/SCL\"><IED name=\"A\"/></SCL>");
        Assert.IsTrue((await f.Session.OpenFileAsync(f.Path)).Succeeded);
        Assert.IsTrue((await f.Session.SaveAsync(f.Session.CurrentRevision)).Succeeded);
        Assert.IsFalse((await File.ReadAllTextAsync(f.Path)).StartsWith("<?xml", StringComparison.Ordinal));
    }

    private static void AssertNoTemporaryFiles(EditingFixture fixture) =>
        Assert.AreEqual(0, Directory.GetFiles(fixture.DirectoryPath, "*.tmp").Length);
}
