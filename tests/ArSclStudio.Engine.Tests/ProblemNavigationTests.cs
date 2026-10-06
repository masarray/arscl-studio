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
            Assert.AreEqual(
                4,
                vm.SelectedEngineeringWorkspaceIndex,
                "Reference navigation should activate the Reports workspace.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task ProblemsCanBeFilteredByDomainSeverityAndTextWithoutLosingNavigation()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="Relay_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="Events" />
                        <ReportControl name="BRCB01" datSet="MissingDataSet" />
                        <GSEControl name="GOOSE_CB"
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

            Assert.IsTrue(vm.Problems.Count >= 3);

            vm.SelectedProblemDomainFilter = "Reference";
            Assert.AreEqual(
                1,
                vm.Problems.Count(row => row.Domain == "Reference"));
            Assert.IsTrue(vm.Problems.All(row => row.Domain == "Reference"));

            vm.SelectedProblemDomainFilter = "All domains";
            vm.SelectedProblemSeverityFilter = "Warning";
            Assert.IsTrue(vm.Problems.Count > 0);
            Assert.IsTrue(vm.Problems.All(row => row.Severity == "Warning"));

            vm.ProblemFilterText = "GOOSE_CB";
            Assert.AreEqual(1, vm.Problems.Count);

            var gooseProblem = vm.Problems.Single();
            Assert.IsFalse(gooseProblem.Node.IsNone);

            vm.SelectedProblemRow = gooseProblem;

            Assert.AreEqual(gooseProblem.Node, vm.SelectedEngineeringRow?.Handle);
            Assert.AreEqual(
                2,
                vm.SelectedEngineeringWorkspaceIndex,
                "Problem navigation should activate the GOOSE workspace.");

            vm.ProblemFilterText = string.Empty;
            vm.SelectedProblemSeverityFilter = "All severities";
            Assert.IsTrue(vm.Problems.Count >= 3);
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
