using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class SclServicesWorkspaceProjectorTests
{
    [TestMethod]
    public async Task ProjectsKnownNestedAndUnknownServicesWithoutInventingRuntimeSupport()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="IED_A">
                <Services>
                  <DynAssociation />
                  <SettingGroups>
                    <SGEdit />
                  </SettingGroups>
                  <ConfDataSet max="50"
                               maxAttributes="200"
                               modify="true" />
                  <ReportSettings cbName="Conf"
                                  datSet="Dyn"
                                  rptID="Fix"
                                  optFields="Dyn" />
                  <GSESettings cbName="Fix"
                               datSet="Conf"
                               appID="Dyn" />
                  <ConfLNs fixPrefix="true"
                           fixLnInst="false" />
                  <GOOSE max="16" />
                  <VendorFutureCapability mode="vendor-defined" />
                </Services>
              </IED>
            </SCL>
            """);

        try
        {
            await using var session =
                new SclDocumentSession();

            var open =
                await session.OpenFileAsync(path);

            Assert.IsTrue(open.Succeeded);
            Assert.IsNotNull(open.State);

            var ied =
                SclIedWorkspaceProjector
                    .Build(open.State)
                    .Single();

            var rows =
                SclServicesWorkspaceProjector.Build(
                    open.State,
                    ied.Handle);

            Assert.AreEqual(9, rows.Length);

            var settingGroups = rows.Single(
                row => row.Name == "SettingGroups");

            Assert.AreEqual(
                "Setting Groups",
                settingGroups.Category);
            Assert.AreEqual(
                "Declared; 1 sub-capability",
                settingGroups.Interpretation);

            var sgEdit = rows.Single(
                row => row.Name == "SGEdit");

            Assert.AreEqual(
                "SettingGroups / SGEdit",
                sgEdit.Path);
            Assert.AreEqual(
                "Declared",
                sgEdit.Interpretation);

            var dataSet = rows.Single(
                row => row.Name == "ConfDataSet");

            Assert.AreEqual(
                "DataSets",
                dataSet.Category);
            Assert.AreEqual(
                "max=50 · maxAttributes=200 · modify=true",
                dataSet.Parameters);
            StringAssert.Contains(
                dataSet.Interpretation,
                "Maximum: 50");
            StringAssert.Contains(
                dataSet.Interpretation,
                "Maximum attributes: 200");
            StringAssert.Contains(
                dataSet.Interpretation,
                "Modification: allowed");

            var report = rows.Single(
                row => row.Name == "ReportSettings");

            StringAssert.Contains(
                report.Interpretation,
                "cbName: Configurable");
            StringAssert.Contains(
                report.Interpretation,
                "datSet: Dynamic");
            StringAssert.Contains(
                report.Interpretation,
                "rptID: Fixed");

            var confLns = rows.Single(
                row => row.Name == "ConfLNs");

            StringAssert.Contains(
                confLns.Interpretation,
                "Prefix: fixed");
            StringAssert.Contains(
                confLns.Interpretation,
                "LN instance: not fixed");

            var goose = rows.Single(
                row => row.Name == "GOOSE");

            Assert.AreEqual("GSE", goose.Category);
            Assert.AreEqual(
                "Maximum: 16",
                goose.Interpretation);

            var unknown = rows.Single(
                row =>
                    row.Name ==
                    "VendorFutureCapability");

            Assert.IsFalse(unknown.IsKnown);
            Assert.AreEqual(
                "Other",
                unknown.Category);
            Assert.AreEqual(
                "mode=vendor-defined",
                unknown.Parameters);
            StringAssert.Contains(
                unknown.Interpretation,
                "semantics not interpreted by ARSCL");

            var details =
                SclNodeDetailsProjector.Create(
                    open.State,
                    dataSet.Handle);

            Assert.AreEqual(
                "ConfDataSet",
                details.Title);
            Assert.AreEqual(
                "ConfDataSet",
                details.Kind);
            Assert.IsTrue(
                details.SourceLocation.Length > 0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task IedWithoutServicesProducesNoCapabilityRows()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="IED_A" />
            </SCL>
            """);

        try
        {
            await using var session =
                new SclDocumentSession();

            var open =
                await session.OpenFileAsync(path);

            Assert.IsTrue(open.Succeeded);
            Assert.IsNotNull(open.State);

            var ied =
                SclIedWorkspaceProjector
                    .Build(open.State)
                    .Single();

            Assert.AreEqual(
                0,
                SclServicesWorkspaceProjector
                    .Build(
                        open.State,
                        ied.Handle)
                    .Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task<string>
        CreateTempFileAsync(string content)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            string.Concat(
                Guid.NewGuid().ToString("N"),
                ".scd"));

        await File.WriteAllTextAsync(
            path,
            content);

        return path;
    }
}
