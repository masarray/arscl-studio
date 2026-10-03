using System.Runtime.CompilerServices;
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

            for (var i = 0; i < 5; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                if (!HasTarget(weakDocument))
                {
                    break;
                }
            }

            Assert.IsFalse(HasTarget(weakDocument));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool HasTarget(
        WeakReference<SclSyntaxDocument> weakDocument) =>
        weakDocument.TryGetTarget(out _);

    [MethodImpl(MethodImplOptions.NoInlining)]
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
