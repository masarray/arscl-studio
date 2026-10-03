using ArSclStudio.Desktop.ViewModels;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class EngineeringWorkspaceViewModelTests
{
    [TestMethod]
    public async Task DesktopFiltersDataSetsAndReportsBySelectedIed()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Communication>
                <SubNetwork name="StationLAN" type="8-MMS">
                  <ConnectedAP iedName="IED_A" apName="P1">
                    <Address>
                      <P type="IP">10.0.0.1</P>
                      <P type="IP-SUBNET">255.255.255.0</P>
                    </Address>
                  </ConnectedAP>
                  <ConnectedAP iedName="IED_B" apName="P1">
                    <Address>
                      <P type="IP">10.0.0.2</P>
                      <P type="IP-SUBNET">255.255.255.0</P>
                    </Address>
                  </ConnectedAP>
                </SubNetwork>
              </Communication>
              <IED name="IED_A" manufacturer="Vendor A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD_A">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="Events_A">
                          <FCDA ldInst="LD_A" lnClass="XCBR" lnInst="1"
                                doName="Pos" daName="stVal" fc="ST" />
                        </DataSet>
                        <ReportControl name="BRCB_A"
                                       datSet="Events_A"
                                       buffered="true" />
                      </LN0>
                      <LN lnClass="XCBR" inst="1" />
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
              <IED name="IED_B" manufacturer="Vendor B">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD_B">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="Events_B">
                          <FCDA ldInst="LD_B" lnClass="CSWI" lnInst="1"
                                doName="Pos" fc="ST" />
                          <FCDA ldInst="LD_B" lnClass="CSWI" lnInst="2"
                                doName="Pos" fc="ST" />
                        </DataSet>
                        <ReportControl name="URCB_B"
                                       datSet="Events_B"
                                       buffered="false" />
                      </LN0>
                      <LN lnClass="CSWI" inst="1" />
                      <LN lnClass="CSWI" inst="2" />
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
            </SCL>
            """);

        try
        {
            await using var vm = new MainWindowViewModel();
            await vm.OpenFileAsync(path);

            Assert.AreEqual(2, vm.IedWorkspaceRows.Count);
            Assert.AreEqual(2, vm.NetworkWorkspaceRows.Count);

            Assert.IsNotNull(vm.SelectedIedWorkspace);
            Assert.AreEqual("IED_A", vm.SelectedIedWorkspace.Name);
            Assert.AreEqual(1, vm.DataSetWorkspaceRows.Count);
            Assert.AreEqual("Events_A", vm.DataSetWorkspaceRows[0].Name);
            Assert.AreEqual(1, vm.DataSetMemberRows.Count);
            Assert.AreEqual(1, vm.ReportWorkspaceRows.Count);
            Assert.AreEqual("BRCB", vm.ReportWorkspaceRows[0].Kind);

            vm.SelectedIedWorkspace = vm.IedWorkspaceRows.Single(
                row => row.Name == "IED_B");

            Assert.AreEqual(1, vm.DataSetWorkspaceRows.Count);
            Assert.AreEqual("Events_B", vm.DataSetWorkspaceRows[0].Name);
            Assert.AreEqual(2, vm.DataSetMemberRows.Count);
            Assert.AreEqual(1, vm.ReportWorkspaceRows.Count);
            Assert.AreEqual("URCB", vm.ReportWorkspaceRows[0].Kind);
            Assert.AreEqual("IED_B", vm.DetailTitle);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task WorkspaceRowsNavigateThroughSharedSelection()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Communication>
                <SubNetwork name="StationLAN" type="8-MMS">
                  <ConnectedAP iedName="IED_A" apName="P1">
                    <Address><P type="IP">10.0.0.1</P></Address>
                  </ConnectedAP>
                </SubNetwork>
              </Communication>
              <IED name="IED_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="Events">
                          <FCDA ldInst="LD0" lnClass="XCBR" lnInst="1"
                                doName="Pos" fc="ST" />
                        </DataSet>
                        <ReportControl name="BRCB01"
                                       datSet="Events"
                                       buffered="true" />
                      </LN0>
                      <LN lnClass="XCBR" inst="1" />
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
            </SCL>
            """);

        try
        {
            await using var vm = new MainWindowViewModel();
            await vm.OpenFileAsync(path);

            vm.SelectedEngineeringWorkspaceIndex = 1;
            Assert.AreEqual("IED_A / P1", vm.DetailTitle);

            vm.SelectedEngineeringWorkspaceIndex = 2;
            Assert.AreEqual("Events", vm.DetailTitle);
            Assert.AreEqual(1, vm.DataSetMemberRows.Count);

            vm.SelectedDataSetMember = vm.DataSetMemberRows.Single();
            StringAssert.Contains(vm.DetailTitle, "LD0/XCBR1/Pos");

            vm.SelectedEngineeringWorkspaceIndex = 3;
            Assert.AreEqual("BRCB01", vm.DetailTitle);
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
