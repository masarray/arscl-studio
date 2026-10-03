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
    public async Task FastValidationReportsGooseEndpointAndDataModelEngineeringFindings()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="PUB">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="" lnType="LT_LLN0">
                        <GSEControl name="GOOSE_CB"
                                    type="GOOSE"
                                    appID="PUB/LD0/LLN0/GOOSE_CB" />
                      </LN0>
                      <LN lnClass="XCBR" inst="1" lnType="LT_XCBR">
                        <DOI name="VendorOnly">
                          <DAI name="stVal"><Val>true</Val></DAI>
                        </DOI>
                      </LN>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>

              <DataTypeTemplates>
                <LNodeType id="LT_LLN0" lnClass="LLN0" />
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

            var result = await session.ValidateFastAsync();
            Assert.IsNotNull(result.Value);

            var goose = result.Value.Diagnostics.Single(
                diagnostic => diagnostic.Code == "SCL-ENG-GOOSE-0001");

            Assert.AreEqual(DiagnosticDomain.Engineering, goose.Domain);
            Assert.AreEqual(DiagnosticSeverity.Warning, goose.Severity);
            Assert.IsTrue(goose.SourceSpan.IsKnown);
            Assert.AreEqual<DocumentRevision?>(open.State.Revision, goose.Revision);
            StringAssert.Contains(goose.Message, "GOOSE_CB");
            StringAssert.Contains(goose.Message, "no Communication/GSE endpoint");

            var model = result.Value.Diagnostics.Single(
                diagnostic => diagnostic.Code == "SCL-SEM-MODEL-0001");

            Assert.AreEqual(DiagnosticDomain.Semantic, model.Domain);
            Assert.AreEqual(DiagnosticSeverity.Warning, model.Severity);
            Assert.IsTrue(model.SourceSpan.IsKnown);
            StringAssert.Contains(model.Message, "VendorOnly");
            StringAssert.Contains(model.Message, "XCBR1");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task FastValidationAcceptsCompleteGooseEndpointWithoutEngineeringWarnings()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Communication>
                <SubNetwork name="StationLAN" type="8-MMS">
                  <ConnectedAP iedName="PUB" apName="P1">
                    <GSE ldInst="LD0" cbName="GOOSE_CB">
                      <Address>
                        <P type="MAC-Address">01-0C-CD-01-00-01</P>
                        <P type="APPID">1001</P>
                      </Address>
                    </GSE>
                  </ConnectedAP>
                </SubNetwork>
              </Communication>

              <IED name="PUB">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="" lnType="LT_LLN0">
                        <GSEControl name="GOOSE_CB"
                                    type="GOOSE"
                                    appID="PUB/LD0/LLN0/GOOSE_CB" />
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

            var result = await session.ValidateFastAsync();
            Assert.IsNotNull(result.Value);

            Assert.IsFalse(result.Value.Diagnostics.Any(
                diagnostic =>
                    diagnostic.Code.StartsWith(
                        "SCL-ENG-GOOSE-",
                        StringComparison.Ordinal)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task FastValidationReportsMissingSmvEndpointWithoutConfusingSmvIdWithNetworkAppId()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="MU_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="MU">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="Samples" />
                        <SampledValueControl name="MSVCB01"
                                             datSet="Samples"
                                             smvID="MU_A/MU/LLN0/MSVCB01"
                                             smpRate="80"
                                             nofASDU="2" />
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

            var smv = result.Value.Diagnostics.Single(
                diagnostic => diagnostic.Code == "SCL-ENG-SMV-0001");

            Assert.AreEqual(DiagnosticDomain.Engineering, smv.Domain);
            Assert.AreEqual(DiagnosticSeverity.Warning, smv.Severity);
            Assert.IsTrue(smv.SourceSpan.IsKnown);
            StringAssert.Contains(smv.Message, "MSVCB01");
            StringAssert.Contains(smv.Message, "no Communication/SMV endpoint");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task FastValidationAcceptsDeepResolvedDoiSdiDaiInstanceChain()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="IED_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="CTRL">
                      <LN lnClass="CSWI" inst="1" lnType="LT_CSWI">
                        <DOI name="Pos">
                          <SDI name="origin">
                            <DAI name="orCat"><Val>bay-control</Val></DAI>
                            <SDI name="orIdent">
                              <DAI name="station"><Val>HMI01</Val></DAI>
                            </SDI>
                          </SDI>
                          <SDI name="Oper">
                            <DAI name="ctlNum"><Val>7</Val></DAI>
                          </SDI>
                          <DAI name="ctlModel">
                            <Val>sbo-with-enhanced-security</Val>
                          </DAI>
                        </DOI>
                      </LN>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>

              <DataTypeTemplates>
                <LNodeType id="LT_CSWI" lnClass="CSWI">
                  <DO name="Pos" type="DOT_POS" />
                </LNodeType>

                <DOType id="DOT_POS" cdc="DPC">
                  <DA name="origin" fc="ST" bType="Struct" type="DAT_ORIGIN" />
                  <SDO name="Oper" type="DOT_OPER" />
                  <DA name="ctlModel" fc="CF" bType="Enum" type="ENUM_CTL" />
                </DOType>

                <DOType id="DOT_OPER" cdc="ACT">
                  <DA name="ctlNum" fc="CO" bType="INT8U" />
                </DOType>

                <DAType id="DAT_ORIGIN">
                  <BDA name="orCat" bType="Enum" type="ENUM_ORCAT" />
                  <BDA name="orIdent" bType="Struct" type="DAT_IDENT" />
                </DAType>

                <DAType id="DAT_IDENT">
                  <BDA name="station" bType="VisString64" />
                </DAType>

                <EnumType id="ENUM_CTL">
                  <EnumVal ord="4">sbo-with-enhanced-security</EnumVal>
                </EnumType>
                <EnumType id="ENUM_ORCAT">
                  <EnumVal ord="2">bay-control</EnumVal>
                </EnumType>
              </DataTypeTemplates>
            </SCL>
            """);

        try
        {
            await using var session = new SclDocumentSession();
            var open = await session.OpenFileAsync(path);
            Assert.IsTrue(open.Succeeded);

            var result = await session.ValidateFastAsync();
            Assert.IsNotNull(result.Value);

            Assert.IsFalse(result.Value.Diagnostics.Any(
                diagnostic =>
                    diagnostic.Code.StartsWith(
                        "SCL-SEM-MODEL-",
                        StringComparison.Ordinal)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task FastValidationReportsUnknownAndStructurallyInvalidNestedInstances()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="IED_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="CTRL">
                      <LN lnClass="CSWI" inst="1" lnType="LT_CSWI">
                        <DOI name="Pos">
                          <SDI name="UnknownStruct">
                            <DAI name="x"><Val>1</Val></DAI>
                          </SDI>
                          <DAI name="UnknownLeaf"><Val>1</Val></DAI>
                          <SDI name="ctlModel">
                            <DAI name="x"><Val>1</Val></DAI>
                          </SDI>
                          <DAI name="origin"><Val>bad</Val></DAI>
                        </DOI>
                      </LN>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>

              <DataTypeTemplates>
                <LNodeType id="LT_CSWI" lnClass="CSWI">
                  <DO name="Pos" type="DOT_POS" />
                </LNodeType>
                <DOType id="DOT_POS" cdc="DPC">
                  <DA name="origin" fc="ST" bType="Struct" type="DAT_ORIGIN" />
                  <DA name="ctlModel" fc="CF" bType="Enum" type="ENUM_CTL" />
                </DOType>
                <DAType id="DAT_ORIGIN">
                  <BDA name="orCat" bType="Enum" type="ENUM_ORCAT" />
                </DAType>
                <EnumType id="ENUM_CTL">
                  <EnumVal ord="0">status-only</EnumVal>
                </EnumType>
                <EnumType id="ENUM_ORCAT">
                  <EnumVal ord="0">not-supported</EnumVal>
                </EnumType>
              </DataTypeTemplates>
            </SCL>
            """);

        try
        {
            await using var session = new SclDocumentSession();
            var open = await session.OpenFileAsync(path);
            Assert.IsTrue(open.Succeeded);

            var result = await session.ValidateFastAsync();
            Assert.IsNotNull(result.Value);

            var codes = result.Value.Diagnostics
                .Where(diagnostic =>
                    diagnostic.Code.StartsWith(
                        "SCL-SEM-MODEL-",
                        StringComparison.Ordinal))
                .Select(diagnostic => diagnostic.Code)
                .ToArray();

            CollectionAssert.Contains(codes, "SCL-SEM-MODEL-0002");
            CollectionAssert.Contains(codes, "SCL-SEM-MODEL-0003");
            CollectionAssert.Contains(codes, "SCL-SEM-MODEL-0005");
            CollectionAssert.Contains(codes, "SCL-SEM-MODEL-0006");

            var structuredAsLeaf = result.Value.Diagnostics.Single(
                diagnostic => diagnostic.Code == "SCL-SEM-MODEL-0006");

            StringAssert.Contains(structuredAsLeaf.Message, "origin");
            Assert.IsTrue(structuredAsLeaf.SourceSpan.IsKnown);
            Assert.AreEqual<DocumentRevision?>(
                open.State?.Revision,
                structuredAsLeaf.Revision);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task FastValidationDoesNotGuessDuplicateTemplateMember()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="IED_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="CTRL">
                      <LN lnClass="CSWI" inst="1" lnType="LT_CSWI">
                        <DOI name="Pos">
                          <DAI name="stVal"><Val>on</Val></DAI>
                        </DOI>
                      </LN>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>

              <DataTypeTemplates>
                <LNodeType id="LT_CSWI" lnClass="CSWI">
                  <DO name="Pos" type="DOT_POS" />
                </LNodeType>
                <DOType id="DOT_POS" cdc="DPC">
                  <DA name="stVal" fc="ST" bType="Dbpos" />
                  <DA name="stVal" fc="ST" bType="BOOLEAN" />
                </DOType>
              </DataTypeTemplates>
            </SCL>
            """);

        try
        {
            await using var session = new SclDocumentSession();
            var open = await session.OpenFileAsync(path);
            Assert.IsTrue(open.Succeeded);

            var result = await session.ValidateFastAsync();
            Assert.IsNotNull(result.Value);

            var ambiguous = result.Value.Diagnostics.Single(
                diagnostic => diagnostic.Code == "SCL-SEM-MODEL-0004");

            Assert.AreEqual(DiagnosticDomain.Semantic, ambiguous.Domain);
            StringAssert.Contains(ambiguous.Message, "stVal");
            StringAssert.Contains(
                ambiguous.Explanation ?? string.Empty,
                "did not guess");
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
