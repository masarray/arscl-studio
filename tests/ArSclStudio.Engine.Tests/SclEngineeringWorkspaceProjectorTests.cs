using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class SclEngineeringWorkspaceProjectorTests
{
    [TestMethod]
    public async Task NetworkWorkspaceProjectsAddressParametersAsEngineeringColumns()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Communication>
                <SubNetwork name="Subnet" type="8-MMS">
                  <ConnectedAP iedName="SIEBCU" apName="E">
                    <Address>
                      <P type="IP">192.16.1.192</P>
                      <P type="IP-SUBNET">255.255.255.0</P>
                      <P type="OSI-AP-Title">1,3,9999,23</P>
                      <P type="OSI-AE-Qualifier">23</P>
                    </Address>
                  </ConnectedAP>
                  <ConnectedAP iedName="BCUGE" apName="S1">
                    <Address>
                      <P type="IP">192.16.1.33</P>
                      <P type="IP-SUBNET">255.255.255.0</P>
                      <P type="IP-GATEWAY">192.16.1.1</P>
                    </Address>
                  </ConnectedAP>
                </SubNetwork>
              </Communication>
              <IED name="SIEBCU" />
              <IED name="BCUGE" />
            </SCL>
            """);

        try
        {
            await using var session = new SclDocumentSession();
            var open = await session.OpenFileAsync(path);
            Assert.IsTrue(open.Succeeded);
            Assert.IsNotNull(open.State);

            var rows = SclNetworkWorkspaceProjector.Build(open.State);

            Assert.AreEqual(2, rows.Length);

            var siebcu = rows.Single(row => row.IedName == "SIEBCU");
            Assert.AreEqual("Subnet", siebcu.SubNetwork);
            Assert.AreEqual("8-MMS", siebcu.NetworkType);
            Assert.AreEqual("E", siebcu.AccessPoint);
            Assert.AreEqual("192.16.1.192", siebcu.IpAddress);
            Assert.AreEqual("255.255.255.0", siebcu.SubnetMask);
            Assert.AreEqual("1,3,9999,23", siebcu.OsiApTitle);
            Assert.AreEqual("23", siebcu.OsiAeQualifier);

            var bcuge = rows.Single(row => row.IedName == "BCUGE");
            Assert.AreEqual("192.16.1.1", bcuge.Gateway);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task DataSetWorkspaceProjectsCatalogMembersAndUsage()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="SIEBCU">
                <AccessPoint name="E">
                  <Server>
                    <LDevice inst="CTRL">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="DataSet">
                          <FCDA ldInst="CTRL" lnClass="XSWI" lnInst="5" doName="Pos" daName="stVal" fc="ST" />
                          <FCDA ldInst="CTRL" lnClass="XSWI" lnInst="5" doName="Pos" daName="q" fc="ST" />
                        </DataSet>
                        <ReportControl name="Buffer" datSet="DataSet" buffered="true" />
                        <GSEControl name="Control_DataSet" datSet="DataSet" />
                      </LN0>
                      <LN lnClass="XSWI" inst="5" />
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

            var ied = SclIedWorkspaceProjector.Build(open.State).Single();
            var dataSets = SclDataSetWorkspaceProjector.BuildCatalog(
                open.State,
                ied.Handle);

            Assert.AreEqual(1, dataSets.Length);
            Assert.AreEqual("CTRL", dataSets[0].LogicalDevice);
            Assert.AreEqual("LLN0", dataSets[0].LogicalNode);
            Assert.AreEqual("DataSet", dataSets[0].Name);
            Assert.AreEqual(2, dataSets[0].MemberCount);
            Assert.AreEqual(2, dataSets[0].UsedByCount);

            var members = SclDataSetWorkspaceProjector.BuildMembers(
                open.State,
                dataSets[0].Handle);

            Assert.AreEqual(2, members.Length);
            Assert.AreEqual("CTRL/XSWI5/Pos.stVal", members[0].Reference);
            Assert.AreEqual("ST", members[0].FunctionalConstraint);
            Assert.AreEqual("q", members[1].DataAttribute);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task ReportWorkspaceTranslatesRcbFlagsIntoReadableEngineeringProperties()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="SIEBCU">
                <AccessPoint name="E">
                  <Server>
                    <LDevice inst="CTRL">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="DataSet_1" />
                        <ReportControl name="Buffer"
                                       datSet="DataSet_1"
                                       rptID="SIEBCUCTRL/LLN0$BR$Buffer"
                                       confRev="80001"
                                       buffered="true"
                                       bufTime="100"
                                       intgPd="5000">
                          <TrgOps dchg="true" qchg="true" dupd="true" period="true" />
                          <OptFields seqNum="true" timeStamp="true" dataSet="true"
                                     reasonCode="true" dataRef="false"
                                     entryID="false" configRef="true" />
                          <RptEnabled max="6" />
                        </ReportControl>
                        <ReportControl name="Unbuffer"
                                       datSet="DataSet_1"
                                       rptID="SIEBCUCTRL/LLN0$RP$Unbuffer"
                                       confRev="80001"
                                       buffered="false" />
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

            var ied = SclIedWorkspaceProjector.Build(open.State).Single();
            var reports = SclReportWorkspaceProjector.Build(open.State, ied.Handle);

            Assert.AreEqual(2, reports.Length);

            var buffered = reports.Single(row => row.Name == "Buffer");
            Assert.AreEqual("BRCB", buffered.Kind);
            Assert.AreEqual("DataSet_1", buffered.DataSet);
            Assert.AreEqual("80001", buffered.ConfigurationRevision);
            Assert.AreEqual("100", buffered.BufferTime);
            Assert.AreEqual("5000", buffered.IntegrityPeriod);
            Assert.AreEqual("6", buffered.MaxClients);
            Assert.AreEqual("Data, Quality, Update, Period", buffered.TriggerSummary);
            Assert.AreEqual(
                "Seq, Time, DataSet, Reason, ConfRev",
                buffered.OptionalFieldsSummary);

            var unbuffered = reports.Single(row => row.Name == "Unbuffer");
            Assert.AreEqual("URCB", unbuffered.Kind);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task GooseWorkspaceJoinsEndpointDataSetAndSubscribersAndKeepsGsseDistinct()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Communication>
                <SubNetwork name="StationLAN" type="8-MMS">
                  <ConnectedAP iedName="PUB" apName="P1">
                    <Address>
                      <P type="IP">10.0.0.1</P>
                    </Address>
                    <GSE ldInst="CTRL" cbName="GOOSE_CB">
                      <Address>
                        <P type="MAC-Address">01-0C-CD-01-00-01</P>
                        <P type="APPID">1001</P>
                        <P type="VLAN-ID">001</P>
                        <P type="VLAN-PRIORITY">4</P>
                      </Address>
                      <MinTime unit="s" multiplier="m">10</MinTime>
                      <MaxTime unit="s" multiplier="m">2000</MaxTime>
                    </GSE>
                  </ConnectedAP>
                </SubNetwork>
              </Communication>

              <IED name="PUB">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="CTRL">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="GooseDs">
                          <FCDA ldInst="CTRL" lnClass="XSWI" lnInst="1"
                                doName="Pos" daName="stVal" fc="ST" />
                          <FCDA ldInst="CTRL" lnClass="XSWI" lnInst="1"
                                doName="Pos" daName="q" fc="ST" />
                        </DataSet>
                        <GSEControl name="GOOSE_CB"
                                    type="GOOSE"
                                    datSet="GooseDs"
                                    appID="PUB/CTRL/LLN0/GOOSE_CB"
                                    confRev="7" />
                        <GSEControl name="GSSE_CB"
                                    type="GSSE"
                                    appID="LEGACY_GSSE" />
                      </LN0>
                      <LN lnClass="XSWI" inst="1" />
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>

              <IED name="SUB">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="">
                        <Inputs>
                          <ExtRef iedName="PUB"
                                  ldInst="CTRL"
                                  lnClass="XSWI"
                                  lnInst="1"
                                  doName="Pos"
                                  daName="stVal"
                                  intAddr="RxPos/stVal" />
                        </Inputs>
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

            var pub = SclIedWorkspaceProjector
                .Build(open.State)
                .Single(row => row.Name == "PUB");

            var controls = SclGooseWorkspaceProjector.BuildCatalog(
                open.State,
                pub.Handle);

            Assert.AreEqual(2, controls.Length);

            var goose = controls.Single(row => row.Name == "GOOSE_CB");
            Assert.AreEqual("GOOSE", goose.ServiceType);
            Assert.AreEqual("P1", goose.AccessPoint);
            Assert.AreEqual("CTRL", goose.LogicalDevice);
            Assert.AreEqual("GooseDs", goose.DataSet);
            Assert.AreEqual("PUB/CTRL/LLN0/GOOSE_CB", goose.ControlAppId);
            Assert.AreEqual("7", goose.ConfigurationRevision);
            Assert.AreEqual("01-0C-CD-01-00-01", goose.MacAddress);
            Assert.AreEqual("1001", goose.NetworkAppId);
            Assert.AreEqual("001", goose.VlanId);
            Assert.AreEqual("4", goose.VlanPriority);
            Assert.AreEqual("10 ms", goose.MinTime);
            Assert.AreEqual("2000 ms", goose.MaxTime);
            Assert.AreEqual(2, goose.MemberCount);
            Assert.AreEqual(1, goose.SubscriberCount);
            Assert.AreEqual(1, goose.SubscriberIedCount);
            Assert.AreEqual("Bound", goose.EndpointStatus);

            var incoming = open.State.SemanticIndex.References
                .GetIncoming(goose.Handle);

            Assert.IsTrue(incoming.Any(edge =>
                edge.Kind ==
                    ArSclStudio.Scl.Semantics.SclReferenceKind
                        .CommunicationControlBinding));

            var signals = SclGooseWorkspaceProjector.BuildSignals(
                open.State,
                goose.Handle);

            Assert.AreEqual(2, signals.Length);
            Assert.AreEqual("CTRL/XSWI1/Pos.stVal", signals[0].Reference);
            Assert.AreEqual(1, signals[0].SubscriberCount);
            Assert.AreEqual("CTRL/XSWI1/Pos.q", signals[1].Reference);
            Assert.AreEqual(0, signals[1].SubscriberCount);

            var subscribers = SclGooseWorkspaceProjector.BuildSubscribers(
                open.State,
                goose.Handle);

            Assert.AreEqual(1, subscribers.Length);
            Assert.AreEqual("SUB", subscribers[0].SubscriberIed);
            Assert.AreEqual("LD0", subscribers[0].SubscriberLogicalDevice);
            Assert.AreEqual("LLN0", subscribers[0].SubscriberLogicalNode);
            Assert.AreEqual("RxPos/stVal", subscribers[0].InternalAddress);
            Assert.AreEqual(
                "PUB/CTRL/XSWI1/Pos.stVal",
                subscribers[0].SourceReference);

            var gsse = controls.Single(row => row.Name == "GSSE_CB");
            Assert.AreEqual("GSSE", gsse.ServiceType);
            Assert.AreEqual("Not applicable", gsse.EndpointStatus);
            Assert.AreEqual(string.Empty, gsse.MacAddress);
            Assert.AreEqual(0, gsse.MemberCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task DataModelWorkspaceExpandsTypeTemplatesAndOverlaysInstanceValues()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="IED_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="CTRL">
                      <LN0 lnClass="LLN0" inst="" lnType="LT_LLN0" />
                      <LN lnClass="CSWI" inst="1" lnType="LT_CSWI" desc="Control">
                        <DOI name="Pos" desc="Cmd. with feedback">
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
                <LNodeType id="LT_LLN0" lnClass="LLN0" />
                <LNodeType id="LT_CSWI" lnClass="CSWI">
                  <DO name="Pos" type="DOT_POS" />
                </LNodeType>

                <DOType id="DOT_POS" cdc="DPC">
                  <DA name="stVal" fc="ST" bType="Dbpos" />
                  <DA name="ctlModel" fc="CF" bType="Enum" type="ENUM_CTL" />
                  <DA name="origin" fc="ST" bType="Struct" type="DAT_ORIGIN" />
                </DOType>

                <DAType id="DAT_ORIGIN">
                  <BDA name="orCat" bType="Enum" type="ENUM_ORCAT" />
                  <BDA name="orIdent" bType="Octet64" />
                </DAType>

                <EnumType id="ENUM_CTL">
                  <EnumVal ord="0">status-only</EnumVal>
                  <EnumVal ord="4">sbo-with-enhanced-security</EnumVal>
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
            Assert.IsNotNull(open.State);

            var ied = SclIedWorkspaceProjector.Build(open.State).Single();
            var logicalNodes = SclDataModelWorkspaceProjector.BuildLogicalNodes(
                open.State,
                ied.Handle);

            Assert.AreEqual(2, logicalNodes.Length);

            var cswi = logicalNodes.Single(row => row.LogicalNode == "CSWI1");
            Assert.AreEqual("CTRL", cswi.LogicalDevice);
            Assert.AreEqual("CSWI", cswi.LnClass);
            Assert.AreEqual("Control", cswi.Description);
            Assert.AreEqual("LT_CSWI", cswi.LnType);
            Assert.AreEqual(1, cswi.DataObjectCount);

            var rows = SclDataModelWorkspaceProjector.BuildRows(
                open.State,
                cswi.Handle);

            var pos = rows.Single(row =>
                row.Kind == "DO" &&
                row.Path == "Pos");

            Assert.AreEqual("DPC", pos.Cdc);
            Assert.AreEqual("Cmd. with feedback", pos.Description);

            var stVal = rows.Single(row => row.Path == "Pos/stVal");
            Assert.AreEqual("DA", stVal.Kind);
            Assert.AreEqual("ST", stVal.FunctionalConstraint);
            Assert.AreEqual("Dbpos", stVal.BasicType);

            var ctlModel = rows.Single(row => row.Path == "Pos/ctlModel");
            Assert.AreEqual("CF", ctlModel.FunctionalConstraint);
            Assert.AreEqual("Enum", ctlModel.BasicType);
            Assert.AreEqual(
                "sbo-with-enhanced-security",
                ctlModel.Value);

            var origin = rows.Single(row => row.Path == "Pos/origin");
            Assert.AreEqual("Struct", origin.BasicType);

            var orCat = rows.Single(row => row.Path == "Pos/origin/orCat");
            Assert.AreEqual("BDA", orCat.Kind);
            Assert.AreEqual("ST", orCat.FunctionalConstraint);
            Assert.AreEqual("Enum", orCat.BasicType);
            Assert.AreEqual(2, orCat.Depth);
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
}
