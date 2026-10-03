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

    private static async Task<string> CreateTempFileAsync(string content)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            string.Concat(Guid.NewGuid().ToString("N"), ".scd"));

        await File.WriteAllTextAsync(path, content);
        return path;
    }
}
