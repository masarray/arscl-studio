using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class SclIedWorkspaceProjectorTests
{
    [TestMethod]
    public async Task IedWorkspaceProjectsEngineeringContextInsteadOfXmlShape()
    {
        var path = await CreateTempFileAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Private type="vendor-noise">must-not-be-an-ied-row</Private>
              <IED name="SIEBCU" manufacturer="SIEMENS" desc="Station controller">
                <AccessPoint name="E">
                  <Server>
                    <LDevice inst="CTRL">
                      <LN0 lnClass="LLN0" inst="">
                        <DataSet name="Control_DataSet" />
                        <DataSet name="DataSet_1" />
                        <ReportControl name="Buffer" datSet="DataSet_1" buffered="true" />
                        <ReportControl name="Unbuffer" datSet="DataSet_1" buffered="false" />
                        <GSEControl name="Control_DataSet" datSet="Control_DataSet" />
                        <SettingControl numOfSGs="1" actSG="1" />
                      </LN0>
                      <LN lnClass="CSWI" inst="1" />
                    </LDevice>
                    <LDevice inst="MEAS">
                      <LN0 lnClass="LLN0" inst="" />
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
              <IED name="C264" manufacturer="ALSTOM">
                <AccessPoint name="AP1">
                  <Server>
                    <LDevice inst="CONTROL">
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

            var rows = SclIedWorkspaceProjector.Build(open.State);

            Assert.AreEqual(2, rows.Length);

            var siebcu = rows.Single(row => row.Name == "SIEBCU");
            Assert.AreEqual("Station controller", siebcu.Description);
            Assert.AreEqual("SIEMENS", siebcu.Manufacturer);
            Assert.AreEqual(1, siebcu.AccessPointCount);
            Assert.AreEqual(2, siebcu.LogicalDeviceCount);
            Assert.AreEqual(3, siebcu.LogicalNodeCount);
            Assert.AreEqual(2, siebcu.DataSetCount);
            Assert.AreEqual(2, siebcu.ReportCount);
            Assert.AreEqual(1, siebcu.GooseCount);
            Assert.AreEqual(1, siebcu.SettingGroupCount);

            var c264 = rows.Single(row => row.Name == "C264");
            Assert.AreEqual("ALSTOM", c264.Manufacturer);
            Assert.AreEqual(1, c264.LogicalDeviceCount);
            Assert.AreEqual(1, c264.LogicalNodeCount);
            Assert.IsFalse(rows.Any(row => row.Name.Contains("vendor", StringComparison.OrdinalIgnoreCase)));
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
