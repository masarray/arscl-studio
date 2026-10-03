using ArSclStudio.Desktop.ViewModels;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class ProblemNavigationTests
{
    [TestMethod]
    public async Task SelectingReferenceProblemNavigatesToItsSourceNode()
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
            await using var vm = new MainWindowViewModel();
            await vm.OpenFileAsync(path);

            var problem = vm.Problems.Single(row => row.Domain == "Reference");
            Assert.IsFalse(problem.Node.IsNone);

            vm.SelectedProblemRow = problem;

            Assert.IsNotNull(vm.SelectedEngineeringRow);
            Assert.AreEqual(problem.Node, vm.SelectedEngineeringRow.Handle);
            Assert.AreEqual("BRCB01", vm.SelectedEngineeringRow.Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task GooseEndpointProblemKeepsSourceSelectionAndHighlightsBoundPublisher()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Communication>
                <SubNetwork name="StationBus">
                  <ConnectedAP iedName="IED_A" apName="P1">
                    <GSE ldInst="LD0" cbName="GOOSE_CB">
                      <Address>
                        <P type="MAC-Address">01-0C-CD-01-00-01</P>
                      </Address>
                    </GSE>
                  </ConnectedAP>
                </SubNetwork>
              </Communication>
              <IED name="IED_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="Events" />
                        <GSEControl
                          name="GOOSE_CB"
                          type="GOOSE"
                          datSet="Events" />
                      </LN0>
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

            var problem = vm.Problems.Single(row =>
                row.Code == "SCL-ENG-GOOSE-0002");

            StringAssert.Contains(
                problem.Explanation,
                "schema validation");

            vm.SelectedProblemRow = problem;

            Assert.IsNotNull(vm.SelectedGooseWorkspace);
            Assert.AreEqual(
                "GOOSE_CB",
                vm.SelectedGooseWorkspace.Name);
            StringAssert.Contains(
                vm.DetailTitle,
                "GOOSE_CB");
            Assert.AreNotEqual(
                vm.SelectedGooseWorkspace.Handle,
                problem.Node);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task UnmatchedDataInstanceProblemHighlightsNearestResolvedModelAncestor()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="IED_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN lnClass="XCBR" inst="1" lnType="LT_XCBR">
                        <DOI name="Pos">
                          <DAI name="unexpected">
                            <Val>1</Val>
                          </DAI>
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
            await using var vm = new MainWindowViewModel();
            await vm.OpenFileAsync(path);

            var problem = vm.Problems.Single(row =>
                row.Code == "SCL-ENG-MODEL-0001");

            vm.SelectedProblemRow = problem;

            Assert.IsNotNull(vm.SelectedDataModelLogicalNode);
            Assert.AreEqual(
                "XCBR1",
                vm.SelectedDataModelLogicalNode.LogicalNode);

            Assert.IsNotNull(vm.SelectedDataModelRow);
            Assert.AreEqual(
                "Pos",
                vm.SelectedDataModelRow.Path);

            Assert.AreEqual(
                "unexpected",
                vm.DetailTitle);
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
