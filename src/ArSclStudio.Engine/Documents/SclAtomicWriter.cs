using System.Security.Cryptography;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Documents;

internal static class SclFileFingerprint
{
    public static async Task<string?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}

internal sealed class SclPreparedSave : IDisposable
{
    internal SclPreparedSave(string temporaryPath, string destinationPath, string? destinationFingerprint,
        string outputFingerprint, SclSyntaxDocument syntax)
    {
        TemporaryPath = temporaryPath;
        DestinationPath = destinationPath;
        DestinationFingerprint = destinationFingerprint;
        OutputFingerprint = outputFingerprint;
        Syntax = syntax;
    }

    internal string TemporaryPath { get; }
    internal string DestinationPath { get; }
    internal string? DestinationFingerprint { get; }
    internal string OutputFingerprint { get; }
    internal SclSyntaxDocument Syntax { get; }

    internal void Commit()
    {
        if (DestinationFingerprint is null)
        {
            File.Move(TemporaryPath, DestinationPath, overwrite: false);
        }
        else
        {
            // Same-directory staging; never delete destination or use a non-atomic copy fallback.
            File.Replace(TemporaryPath, DestinationPath, destinationBackupFileName: null);
        }
    }

    public void Dispose()
    {
        try
        {
            File.Delete(TemporaryPath);
        }
        catch (IOException) { /* Best-effort cleanup must not disguise commit status. */ }
        catch (UnauthorizedAccessException) { /* A failed cleanup never deletes the destination. */ }
    }
}

internal static class SclAtomicWriter
{
    internal static async Task<SclPreparedSave> PrepareAsync(SclDocumentState state, string destination,
        string? expectedFingerprint, CancellationToken cancellationToken)
    {
        var observed = await SclFileFingerprint.ReadAsync(destination, cancellationToken).ConfigureAwait(false);
        if (observed != expectedFingerprint)
        {
            throw new IOException("The destination changed outside ARSCL. Reopen it or choose a new Save As path.");
        }

        var temporary = Path.Combine(Path.GetDirectoryName(destination)!,
            $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                128 * 1024, FileOptions.SequentialScan))
            {
                state.Syntax.WriteTo(stream, cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            await using var input = new FileStream(temporary, FileMode.Open, FileAccess.Read, FileShare.Read,
                128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var reopened = await new SclDocumentLoader().LoadAsync(input, Path.GetFileName(destination),
                cancellationToken).ConfigureAwait(false);
            var verified = state.Syntax.VerifyAndRebind(reopened, cancellationToken);
            var outputFingerprint = await SclFileFingerprint.ReadAsync(temporary, cancellationToken).ConfigureAwait(false);
            return new SclPreparedSave(temporary, destination, observed, outputFingerprint!, verified);
        }
        catch
        {
            try { File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            throw;
        }
    }
}
