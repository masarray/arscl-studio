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

    [TestMethod]
    public async Task EngineeringDiagnosticsAreSourceLinkedAndKeepGsseDistinct()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Communication>
                <SubNetwork name="StationLAN" type="8-MMS">
                  <ConnectedAP iedName="IED_A" apName="P1">
                    <Address>
                      <P type="IP">10.0.0.10</P>
                    </Address>
                    <GSE ldInst="LD0" cbName="GOOSE_PARTIAL">
                      <Address>
                        <P type="MAC-Address">01-0C-CD-01-00-01</P>
                      </Address>
                    </GSE>
                  </ConnectedAP>
                  <ConnectedAP iedName="IED_B" apName="P1">
                    <Address>
                      <P type="IP">10.0.0.10</P>
                    </Address>
                  </ConnectedAP>
                </SubNetwork>
              </Communication>

              <IED name="IED_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="">
                        <GSEControl name="GOOSE_PARTIAL" type="GOOSE" />
                        <GSEControl name="GOOSE_NO_ENDPOINT" type="GOOSE" />
                        <GSEControl name="LEGACY_GSSE" type="GSSE" />
                      </LN0>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>

              <IED name="IED_B">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="" />
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

            var result = await session.ValidateFastAsync();

            Assert.AreEqual(WorkResultStatus.Published, result.Status);
            Assert.IsNotNull(result.Value);

            var engineering = result.Value.Diagnostics
                .Where(diagnostic =>
                    diagnostic.Domain == DiagnosticDomain.Engineering)
                .ToArray();

            Assert.AreEqual(
                2,
                engineering.Count(diagnostic =>
                    diagnostic.Code == "SCL-ENG-NET-0001"));

            var missingEndpoint = engineering.Single(diagnostic =>
                diagnostic.Code == "SCL-ENG-GOOSE-0001");

            Assert.AreEqual(
                DiagnosticSeverity.Warning,
                missingEndpoint.Severity);
            Assert.IsTrue(missingEndpoint.SourceSpan.IsKnown);
            Assert.AreEqual(
                Path.GetFullPath(path),
                missingEndpoint.SourcePath);
            Assert.AreEqual<DocumentRevision?>(
                open.State.Revision,
                missingEndpoint.Revision);
            StringAssert.Contains(
                missingEndpoint.Message,
                "GOOSE_NO_ENDPOINT");

            var partialEndpoint = engineering.Single(diagnostic =>
                diagnostic.Code == "SCL-ENG-GOOSE-0002");

            Assert.IsTrue(partialEndpoint.SourceSpan.IsKnown);
            StringAssert.Contains(
                partialEndpoint.Message,
                "APPID");
            StringAssert.Contains(
                partialEndpoint.Message,
                "GOOSE_PARTIAL");

            Assert.IsFalse(engineering.Any(diagnostic =>
                diagnostic.Message.Contains(
                    "LEGACY_GSSE",
                    StringComparison.Ordinal)));

            Assert.IsFalse(engineering.Any(diagnostic =>
                diagnostic.Domain == DiagnosticDomain.Schema));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task SettingGroupDiagnosticsReportRangeAndMissingConfiguredValues()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="IED_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="PROT">
                      <LN0 lnClass="LLN0" inst="" lnType="LT_LLN0">
                        <SettingControl numOfSGs="2" actSG="3" />
                      </LN0>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
              <DataTypeTemplates>
                <LNodeType id="LT_LLN0" lnClass="LLN0" />
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

            var engineering = result.Value.Diagnostics
                .Where(diagnostic =>
                    diagnostic.Domain == DiagnosticDomain.Engineering)
                .ToArray();

            Assert.IsFalse(engineering.Any(diagnostic =>
                diagnostic.Code == "SCL-ENG-SG-0001"));

            var activeRange = engineering.Single(diagnostic =>
                diagnostic.Code == "SCL-ENG-SG-0002");

            Assert.AreEqual(
                DiagnosticSeverity.Warning,
                activeRange.Severity);
            StringAssert.Contains(
                activeRange.Message,
                "outside");
            StringAssert.Contains(
                activeRange.Message,
                "1..2");

            var noValues = engineering.Single(diagnostic =>
                diagnostic.Code == "SCL-ENG-SG-0003");

            Assert.AreEqual(
                DiagnosticSeverity.Info,
                noValues.Severity);
            StringAssert.Contains(
                noValues.Message,
                "FC=SG");
            Assert.AreEqual<DocumentRevision?>(
                open.State.Revision,
                noValues.Revision);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task DataModelDiagnosticsReportOnlyTopmostUnmatchedInstanceBranch()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="IED_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN lnClass="XCBR" inst="1" lnType="LT_XCBR">
                        <DOI name="Pos">
                          <DAI name="stVal">
                            <Val>on</Val>
                          </DAI>
                          <DAI name="unexpected">
                            <Val>vendor-value</Val>
                          </DAI>
                        </DOI>
                        <DOI name="Unknown">
                          <SDI name="nested">
                            <DAI name="value">
                              <Val>1</Val>
                            </DAI>
                          </SDI>
                        </DOI>
                      </LN>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>

              <DataTypeTemplates>
                <LNodeType id="LT_XCBR" lnClass="XCBR">
                  <DO name="Pos" type="DOT_POS" />
                </LNodeType>
                <DOType id="DOT_POS" cdc="DPC">
                  <DA name="stVal" fc="ST" bType="Dbpos" />
                </DOType>
              </DataTypeTemplates>
            </SCL>
            """);

        try
        {
            await using var session = new SclDocumentSession();
            var open = await session.OpenFileAsync(path);
            Assert.IsTrue(open.Succeeded);
            Assert.IsNotNull(open.State);

            var fast = await session.ValidateFastAsync();
            Assert.IsNotNull(fast.Value);
            Assert.IsFalse(fast.Value.Diagnostics.Any(diagnostic =>
                diagnostic.Code == "SCL-ENG-MODEL-0001"));

            var result = await session.ValidateFullAsync();
            Assert.IsNotNull(result.Value);

            var findings = result.Value.Diagnostics
                .Where(diagnostic =>
                    diagnostic.Code == "SCL-ENG-MODEL-0001")
                .ToArray();

            Assert.AreEqual(2, findings.Length);

            Assert.IsTrue(findings.Any(diagnostic =>
                diagnostic.Message.Contains(
                    "Pos/unexpected",
                    StringComparison.Ordinal)));

            Assert.IsTrue(findings.Any(diagnostic =>
                diagnostic.Message.Contains(
                    "'Unknown'",
                    StringComparison.Ordinal)));

            Assert.IsFalse(findings.Any(diagnostic =>
                diagnostic.Message.Contains(
                    "Unknown/nested",
                    StringComparison.Ordinal)));

            Assert.IsFalse(findings.Any(diagnostic =>
                diagnostic.Message.Contains(
                    "Pos/stVal",
                    StringComparison.Ordinal)));

            Assert.IsTrue(findings.All(diagnostic =>
                diagnostic.Domain == DiagnosticDomain.Engineering &&
                diagnostic.Severity == DiagnosticSeverity.Warning &&
                diagnostic.SourceSpan.IsKnown &&
                diagnostic.Revision == open.State.Revision));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task SampledValueDiagnosticsSeparateEndpointIdentityAndDataSetFindings()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Communication>
                <SubNetwork name="ProcessBus">
                  <ConnectedAP iedName="MU_A" apName="P1">
                    <SMV ldInst="LD0" cbName="SMV_PARTIAL">
                      <Address>
                        <P type="MAC-Address">01-0C-CD-04-00-01</P>
                      </Address>
                    </SMV>
                    <SMV ldInst="LD0" cbName="SMV_NO_DATASET">
                      <Address>
                        <P type="MAC-Address">01-0C-CD-04-00-02</P>
                        <P type="APPID">4002</P>
                      </Address>
                    </SMV>
                  </ConnectedAP>
                </SubNetwork>
              </Communication>

              <IED name="MU_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="ProcessData" />
                        <SampledValueControl
                          name="SMV_PARTIAL"
                          datSet="ProcessData"
                          smvID="MU_A/LD0/LLN0/SMV_PARTIAL" />
                        <SampledValueControl
                          name="SMV_NO_ENDPOINT"
                          datSet="ProcessData"
                          smvID="MU_A/LD0/LLN0/SMV_NO_ENDPOINT" />
                        <SampledValueControl
                          name="SMV_NO_DATASET"
                          smvID="MU_A/LD0/LLN0/SMV_NO_DATASET" />
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

            var result = await session.ValidateFastAsync();
            Assert.IsNotNull(result.Value);

            var engineering = result.Value.Diagnostics
                .Where(diagnostic =>
                    diagnostic.Domain ==
                        DiagnosticDomain.Engineering)
                .ToArray();

            var noEndpoint = engineering.Single(diagnostic =>
                diagnostic.Code == "SCL-ENG-SMV-0001");

            StringAssert.Contains(
                noEndpoint.Message,
                "SMV_NO_ENDPOINT");

            var partial = engineering.Single(diagnostic =>
                diagnostic.Code == "SCL-ENG-SMV-0002");

            StringAssert.Contains(
                partial.Message,
                "SMV_PARTIAL");
            StringAssert.Contains(
                partial.Message,
                "APPID");

            var noDataSet = engineering.Single(diagnostic =>
                diagnostic.Code == "SCL-ENG-SMV-0003");

            StringAssert.Contains(
                noDataSet.Message,
                "SMV_NO_DATASET");

            Assert.IsTrue(new[]
            {
                noEndpoint,
                partial,
                noDataSet
            }.All(diagnostic =>
                diagnostic.SourceSpan.IsKnown &&
                diagnostic.Revision == open.State.Revision));
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
