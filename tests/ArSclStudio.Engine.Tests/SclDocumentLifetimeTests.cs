using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class SclDocumentLifetimeTests
{
    [TestMethod]
    public async Task DisposedSessionReleasesCommittedDocumentState()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            string.Concat(Guid.NewGuid().ToString("N"), ".scd"));

        await File.WriteAllTextAsync(
            path,
            "<SCL xmlns=\"http://www.iec.ch/61850/2003/SCL\"><Header id=\"Lifetime\"/></SCL>");

        try
        {
            var weakDocument = await OpenDisposeAndReturnWeakDocumentAsync(path);

            for (var i = 0; i < 3 && weakDocument.TryGetTarget(out _); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.IsFalse(weakDocument.TryGetTarget(out _));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task<WeakReference<SclSyntaxDocument>>
        OpenDisposeAndReturnWeakDocumentAsync(string path)
    {
        var session = new SclDocumentSession(maxWorkerConcurrency: 1);
        var result = await session.OpenFileAsync(path);

        Assert.IsNotNull(result.State);

        var weakDocument = new WeakReference<SclSyntaxDocument>(
            result.State.Syntax);

        await session.DisposeAsync();

        return weakDocument;
    }
}
