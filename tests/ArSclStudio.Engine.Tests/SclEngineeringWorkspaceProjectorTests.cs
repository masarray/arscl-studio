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

    private static async Task<string> CreateTempFileAsync(string content)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            string.Concat(Guid.NewGuid().ToString("N"), ".scd"));

        await File.WriteAllTextAsync(path, content);
        return path;
    }
}
