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
                <Services>
                  <GOOSE max="2" />
                </Services>
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
                <Services>
                  <ConfDataSet max="4" modify="false" />
                </Services>
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
            Assert.AreEqual(2, vm.StationNetworkWorkspaceRows.Count);
            Assert.AreEqual(1, vm.NetworkWorkspaceRows.Count);
            Assert.AreEqual("IED_A", vm.NetworkWorkspaceRows.Single().IedName);

            Assert.IsNotNull(vm.SelectedIedWorkspace);
            Assert.AreEqual("IED_A", vm.SelectedIedWorkspace.Name);
            Assert.AreEqual(1, vm.ServiceCapabilityRows.Count);
            Assert.AreEqual("GOOSE", vm.ServiceCapabilityRows[0].Name);
            Assert.AreEqual("Maximum: 2", vm.ServiceCapabilityRows[0].Interpretation);
            Assert.AreEqual(1, vm.DataSetWorkspaceRows.Count);
            Assert.AreEqual("Events_A", vm.DataSetWorkspaceRows[0].Name);
            Assert.AreEqual(1, vm.ReportWorkspaceRows.Count);
            Assert.AreEqual("BRCB", vm.ReportWorkspaceRows[0].Kind);

            vm.SelectedIedWorkspace = vm.IedWorkspaceRows.Single(
                row => row.Name == "IED_B");

            Assert.AreEqual(1, vm.NetworkWorkspaceRows.Count);
            Assert.AreEqual("IED_B", vm.NetworkWorkspaceRows.Single().IedName);
            Assert.AreEqual(1, vm.ServiceCapabilityRows.Count);
            Assert.AreEqual("ConfDataSet", vm.ServiceCapabilityRows[0].Name);
            StringAssert.Contains(
                vm.ServiceCapabilityRows[0].Interpretation,
                "Modification: not allowed");
            Assert.AreEqual(1, vm.DataSetWorkspaceRows.Count);
            Assert.AreEqual("Events_B", vm.DataSetWorkspaceRows[0].Name);
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
                    <GSE ldInst="LD0" cbName="GOOSE_CB">
                      <Address>
                        <P type="MAC-Address">01-0C-CD-01-00-01</P>
                        <P type="APPID">1001</P>
                        <P type="VLAN-ID">001</P>
                        <P type="VLAN-PRIORITY">4</P>
                      </Address>
                    </GSE>
                  </ConnectedAP>
                </SubNetwork>
              </Communication>
              <IED name="IED_A">
                <Services>
                  <ReportSettings cbName="Conf"
                                  datSet="Dyn"
                                  rptID="Fix" />
                </Services>
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="" lnType="LT_LLN0">
                        <DataSet name="Events">
                          <FCDA ldInst="LD0" lnClass="XCBR" lnInst="1"
                                doName="Pos" fc="ST" />
                        </DataSet>
                        <ReportControl name="BRCB01"
                                       datSet="Events"
                                       buffered="true" />
                        <GSEControl name="GOOSE_CB"
                                    type="GOOSE"
                                    datSet="Events"
                                    appID="IED_A/LD0/LLN0/GOOSE_CB"
                                    confRev="1" />
                        <SettingControl numOfSGs="2" actSG="1" />
                      </LN0>
                      <LN lnClass="XCBR" inst="1" lnType="LT_XCBR">
                        <DOI name="Pos" desc="Breaker position">
                          <DAI name="stVal"><Val>on</Val></DAI>
                        </DOI>
                        <DOI name="Cfg" desc="Breaker setting">
                          <DAI name="setVal"><Val>5</Val></DAI>
                          <DAI name="minVal"><Val>1</Val></DAI>
                          <DAI name="maxVal"><Val>10</Val></DAI>
                          <DAI name="stepSize"><Val>1</Val></DAI>
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
                  <DO name="Cfg" type="DOT_CFG" />
                </LNodeType>
                <DOType id="DOT_POS" cdc="DPC">
                  <DA name="stVal" fc="ST" bType="Dbpos" />
                </DOType>
                <DOType id="DOT_CFG" cdc="ING">
                  <DA name="setVal" fc="SG" bType="INT32" />
                  <DA name="minVal" fc="CF" bType="INT32" />
                  <DA name="maxVal" fc="CF" bType="INT32" />
                  <DA name="stepSize" fc="CF" bType="INT32U" />
                </DOType>
              </DataTypeTemplates>
            </SCL>
            """);

        try
        {
            await using var vm = new MainWindowViewModel();
            await vm.OpenFileAsync(path);

            Assert.AreEqual(1, vm.ServiceCapabilityRows.Count);
            var service = vm.ServiceCapabilityRows.Single();
            vm.SelectedServiceCapability = service;

            Assert.AreEqual("ReportSettings", vm.DetailTitle);
            StringAssert.Contains(service.Interpretation, "cbName: Configurable");

            vm.SelectedEngineeringWorkspaceIndex = 1;
            Assert.AreEqual("IED_A / P1", vm.DetailTitle);
            Assert.AreEqual(1, vm.SelectedEngineeringWorkspaceIndex);
            Assert.IsTrue(vm.EngineeringWorkspaceNavigationRows.All(
                row => row.WorkspaceIndex == 1 && row.IsObject));

            vm.SelectedEngineeringWorkspaceIndex = 2;
            await vm.WaitForEngineeringWorkspaceIdleAsync();
            Assert.AreEqual("GOOSE_CB", vm.DetailTitle);
            Assert.AreEqual(1, vm.GooseWorkspaceRows.Count);
            Assert.AreEqual(1, vm.GooseSignalRows.Count);
            Assert.AreEqual("01-0C-CD-01-00-01", vm.SelectedGooseWorkspace?.MacAddress);
            Assert.IsTrue(vm.EngineeringWorkspaceNavigationRows.Any(
                row =>
                    row.WorkspaceIndex == 2 &&
                    row.IsObject &&
                    row.Handle == vm.SelectedGooseWorkspace!.Handle &&
                    row.Name.Contains("GOOSE_CB", StringComparison.Ordinal)));

            vm.SelectedEngineeringWorkspaceIndex = 3;
            await vm.WaitForEngineeringWorkspaceIdleAsync();
            Assert.AreEqual("Events", vm.DetailTitle);
            Assert.AreEqual(1, vm.DataSetMemberRows.Count);

            var dataSetNavigationRow =
                vm.EngineeringWorkspaceNavigationRows.Single(
                    row =>
                        row.WorkspaceIndex == 3 &&
                        row.IsObject &&
                        row.Handle == vm.SelectedDataSetWorkspace!.Handle);
            Assert.AreEqual(
                dataSetNavigationRow,
                vm.SelectedEngineeringNavigationRow);

            vm.SelectedDataSetMember = vm.DataSetMemberRows.Single();
            StringAssert.Contains(vm.DetailTitle, "LD0/XCBR1/Pos");

            vm.SelectedEngineeringWorkspaceIndex = 4;
            await vm.WaitForEngineeringWorkspaceIdleAsync();
            Assert.AreEqual("BRCB01", vm.DetailTitle);
            Assert.AreEqual(1, vm.ReportDataSetMemberRows.Count);
            Assert.AreEqual(
                "LD0/XCBR1/Pos",
                vm.ReportDataSetMemberRows.Single().Reference);

            vm.SelectedDataModelLogicalNode = vm.DataModelLogicalNodes.Single(
                row => row.LogicalNode == "XCBR1");
            vm.SelectedEngineeringWorkspaceIndex = 5;
            await vm.WaitForEngineeringWorkspaceIdleAsync();

            Assert.AreEqual("XCBR1", vm.DetailTitle);
            Assert.IsTrue(vm.DataModelRows.Any(
                row => row.Path == "Pos" && row.Cdc == "DPC"));
            Assert.IsTrue(vm.DataModelRows.Any(
                row => row.Path == "Pos/stVal" &&
                       row.Value == "on" &&
                       row.FunctionalConstraint == "ST"));

            var positionDisplay = vm.DataModelDisplayRows.Single(
                row => row.Path == "Pos");
            Assert.IsTrue(positionDisplay.HasChildren);

            vm.ToggleDataModelRow(positionDisplay);
            Assert.IsFalse(vm.DataModelDisplayRows.Any(
                row => row.Path == "Pos/stVal"));

            vm.ToggleDataModelRow(vm.DataModelDisplayRows.Single(
                row => row.Path == "Pos"));
            Assert.IsTrue(vm.DataModelDisplayRows.Any(
                row => row.Path == "Pos/stVal"));

            vm.SelectedEngineeringWorkspaceIndex = 6;
            await vm.WaitForEngineeringWorkspaceIdleAsync();

            Assert.AreEqual("SettingControl", vm.DetailTitle);
            Assert.AreEqual(1, vm.SettingGroupControls.Count);
            Assert.AreEqual(1, vm.SettingGroupSettings.Count);

            var setting = vm.SettingGroupSettings.Single();
            Assert.AreEqual("Cfg", setting.DataObject);
            Assert.AreEqual("setVal", setting.Setting);
            Assert.AreEqual("5", setting.Value);
            Assert.AreEqual("1", setting.Minimum);
            Assert.AreEqual("10", setting.Maximum);

            vm.SelectedSettingGroupSetting = setting;
            Assert.AreEqual("setVal", vm.DetailTitle);
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
