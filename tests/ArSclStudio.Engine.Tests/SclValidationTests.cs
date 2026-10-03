using ArSclStudio.Engine.Diagnostics;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Validation;
using ArSclStudio.Engine.Workers;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class SclValidationTests
{
    [TestMethod]
    public async Task FastValidationReportsUnresolvedReferenceWithRevisionAndSource()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="Relay_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="">
                        <ReportControl name="BRCB01" datSet="MissingDataSet" />
                      </LN0>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
            </SCL>
            """);

        try
        {
            await using var session = new SclDocumentSession();
            var open = await session.OpenFileAsync(path);
            Assert.IsTrue(open.Succeeded);
            Assert.IsNotNull(open.State);
            Assert.AreEqual(1, open.State.SemanticIndex.References.IssueCount);

            var result = await session.ValidateFastAsync();

            Assert.AreEqual(WorkResultStatus.Published, result.Status);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(open.State.Revision, result.Value.Revision);

            var reference = result.Value.Diagnostics.Single(
                diagnostic => diagnostic.Domain == DiagnosticDomain.Reference);

            Assert.AreEqual("SCL-REF-0001", reference.Code);
            Assert.AreEqual(DiagnosticSeverity.Error, reference.Severity);
            Assert.IsFalse(reference.Node.IsNone);
            Assert.IsTrue(reference.SourceSpan.IsKnown);
            Assert.AreEqual(Path.GetFullPath(path), reference.SourcePath);
            Assert.AreEqual<DocumentRevision?>(open.State.Revision, reference.Revision);
            StringAssert.Contains(reference.Message, "MissingDataSet");

            var schema = result.Value.Diagnostics.Single(
                diagnostic => diagnostic.Domain == DiagnosticDomain.Schema);

            Assert.AreEqual("SCL-SCHEMA-0001", schema.Code);
            Assert.AreEqual(DiagnosticSeverity.Info, schema.Severity);
            Assert.AreSame(open.State, session.CurrentState);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task AmbiguousReferenceIsReportedWithoutGuessingTarget()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="Relay_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN lnClass="XCBR" inst="1" lnType="DUPLICATE_TYPE" />
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
              <DataTypeTemplates>
                <LNodeType id="DUPLICATE_TYPE" lnClass="XCBR" />
                <LNodeType id="DUPLICATE_TYPE" lnClass="CSWI" />
              </DataTypeTemplates>
            </SCL>
            """);

        try
        {
            await using var session = new SclDocumentSession();
            var open = await session.OpenFileAsync(path);
            Assert.IsTrue(open.Succeeded);
            Assert.IsNotNull(open.State);

            var result = await session.ValidateFastAsync();
            Assert.IsNotNull(result.Value);

            var reference = result.Value.Diagnostics.Single(
                diagnostic => diagnostic.Domain == DiagnosticDomain.Reference);

            Assert.AreEqual("SCL-REF-0002", reference.Code);
            StringAssert.Contains(reference.Message, "ambiguous");
            StringAssert.Contains(reference.Message, "did not guess");
            Assert.AreEqual(0, open.State.SemanticIndex.References.EdgeCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task FastValidationIsLatestWinsAndCancelsOlderRequest()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Header id="Station-A" />
            </SCL>
            """);

        try
        {
            var provider = new BlockingStatusProvider();
            await using var session = new SclDocumentSession(
                maxWorkerConcurrency: 2,
                schemaProvider: provider);

            var open = await session.OpenFileAsync(path);
            Assert.IsTrue(open.Succeeded);

            var first = session.ValidateFastAsync();
            await provider.FirstStatusEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var second = await session.ValidateFastAsync();
            var firstResult = await first;

            Assert.AreEqual(WorkResultStatus.Superseded, firstResult.Status);
            Assert.AreEqual(WorkResultStatus.Published, second.Status);
            Assert.IsNotNull(second.Value);
            Assert.AreEqual(2, provider.StatusCalls);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task FullValidationPublishesSchemaFindingsFromConfiguredProvider()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Header id="Station-A" />
            </SCL>
            """);

        try
        {
            var provider = new FindingSchemaProvider();
            await using var session = new SclDocumentSession(schemaProvider: provider);

            var open = await session.OpenFileAsync(path);
            Assert.IsTrue(open.Succeeded);
            Assert.IsNotNull(open.State);

            var fast = await session.ValidateFastAsync();
            Assert.IsNotNull(fast.Value);
            Assert.AreEqual(0, provider.ValidateCalls);
            Assert.IsTrue(fast.Value.Diagnostics.Any(
                diagnostic => diagnostic.Code == "SCL-SCHEMA-0002"));

            var full = await session.ValidateFullAsync();
            Assert.AreEqual(WorkResultStatus.Published, full.Status);
            Assert.IsNotNull(full.Value);
            Assert.AreEqual(1, provider.ValidateCalls);

            var finding = full.Value.Diagnostics.Single(
                diagnostic => diagnostic.Code == "TEST-XSD-0001");

            Assert.AreEqual(DiagnosticDomain.Schema, finding.Domain);
            Assert.AreEqual<DocumentRevision?>(open.State.Revision, finding.Revision);
            Assert.AreEqual(Path.GetFullPath(path), finding.SourcePath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task<string> CreateTempFileAsync(string content)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            string.Concat(Guid.NewGuid().ToString("N"), ".scd"));

        await File.WriteAllTextAsync(path, content);
        return path;
    }

    private sealed class BlockingStatusProvider : ISclSchemaProvider
    {
        private int _statusCalls;

        public TaskCompletionSource<bool> FirstStatusEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int StatusCalls => Volatile.Read(ref _statusCalls);

        public async ValueTask<SclSchemaProviderStatus> GetStatusAsync(
            SclDocumentState state,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);

            var call = Interlocked.Increment(ref _statusCalls);
            if (call == 1)
            {
                FirstStatusEntered.TrySetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return new SclSchemaProviderStatus(
                "test",
                SclSchemaProviderAvailability.Available,
                "Test schema provider is available.",
                "Synthetic test provider.");
        }

        public ValueTask<IReadOnlyList<SclSchemaFinding>> ValidateAsync(
            SclDocumentState state,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<SclSchemaFinding>>([]);
    }

    private sealed class FindingSchemaProvider : ISclSchemaProvider
    {
        private int _validateCalls;

        public int ValidateCalls => Volatile.Read(ref _validateCalls);

        public ValueTask<SclSchemaProviderStatus> GetStatusAsync(
            SclDocumentState state,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(new SclSchemaProviderStatus(
                "test-xsd",
                SclSchemaProviderAvailability.Available,
                "Synthetic schema set available.",
                "Test-only schema provider."));
        }

        public ValueTask<IReadOnlyList<SclSchemaFinding>> ValidateAsync(
            SclDocumentState state,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _validateCalls);

            return ValueTask.FromResult<IReadOnlyList<SclSchemaFinding>>(
            [
                new SclSchemaFinding(
                    "TEST-XSD-0001",
                    DiagnosticSeverity.Warning,
                    "Synthetic schema finding.",
                    state.Syntax.RootHandle,
                    state.Syntax.GetSourceSpan(state.Syntax.RootHandle))
            ]);
        }
    }
}
