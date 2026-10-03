using System.Text;
using ArSclStudio.Scl.Semantics;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Scl.Tests;

[TestClass]
public sealed class SclSemanticIndexTests
{
    [TestMethod]
    public async Task BuildIndexesDeepIedHierarchyAndControlBlocks()
    {
        var document = await LoadAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="Relay_A" manufacturer="Example">
                <Services />
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="" lnType="LLN0_TYPE">
                        <DataSet name="Events">
                          <FCDA ldInst="LD0" lnClass="XCBR" lnInst="1" doName="Pos" daName="stVal" fc="ST" />
                        </DataSet>
                        <ReportControl name="BRCB01" datSet="Events" buffered="true" />
                        <GSEControl name="GO1" datSet="Events" />
                        <Inputs>
                          <ExtRef iedName="Relay_B" ldInst="LD0" lnClass="XCBR" lnInst="1" doName="Pos" daName="stVal" />
                        </Inputs>
                        <SettingControl numOfSGs="4" actSG="1" />
                      </LN0>
                      <LN lnClass="XCBR" inst="1" lnType="XCBR_TYPE">
                        <DOI name="Pos"><DAI name="stVal" /></DOI>
                      </LN>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
              <DataTypeTemplates>
                <LNodeType id="LLN0_TYPE" lnClass="LLN0" />
                <LNodeType id="XCBR_TYPE" lnClass="XCBR">
                  <DO name="Pos" type="DPC_TYPE" />
                </LNodeType>
                <DOType id="DPC_TYPE" cdc="DPC">
                  <DA name="stVal" bType="Enum" type="DPC_ENUM" fc="ST" />
                </DOType>
                <EnumType id="DPC_ENUM" />
              </DataTypeTemplates>
            </SCL>
            """);

        var index = SclSemanticIndexBuilder.Build(document);

        Assert.IsTrue(index.NodeCount >= 20);
        Assert.AreEqual(1, Count(index, SclSemanticKind.Ied));
        Assert.AreEqual(1, Count(index, SclSemanticKind.AccessPoint));
        Assert.AreEqual(1, Count(index, SclSemanticKind.LogicalDevice));
        Assert.AreEqual(1, Count(index, SclSemanticKind.LogicalNodeZero));
        Assert.AreEqual(1, Count(index, SclSemanticKind.LogicalNode));
        Assert.AreEqual(1, Count(index, SclSemanticKind.DataSet));
        Assert.AreEqual(1, Count(index, SclSemanticKind.Fcda));
        Assert.AreEqual(1, Count(index, SclSemanticKind.ReportControl));
        Assert.AreEqual(1, Count(index, SclSemanticKind.GseControl));
        Assert.AreEqual(1, Count(index, SclSemanticKind.ExternalReference));
        Assert.AreEqual(1, Count(index, SclSemanticKind.SettingGroupControl));
    }

    [TestMethod]
    public async Task ReferenceGraphResolvesDataSetTypeAndCommunicationBindings()
    {
        var document = await LoadAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Communication>
                <SubNetwork name="StationBus">
                  <ConnectedAP iedName="Relay_A" apName="P1" />
                </SubNetwork>
              </Communication>
              <IED name="Relay_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="" lnType="LLN0_TYPE">
                        <DataSet name="Events" />
                        <ReportControl name="BRCB01" datSet="Events" />
                      </LN0>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
              <DataTypeTemplates>
                <LNodeType id="LLN0_TYPE" lnClass="LLN0">
                  <DO name="Beh" type="ENS_TYPE" />
                </LNodeType>
                <DOType id="ENS_TYPE" cdc="ENS">
                  <DA name="stVal" bType="Enum" type="BEH_ENUM" fc="ST" />
                </DOType>
                <EnumType id="BEH_ENUM" />
              </DataTypeTemplates>
            </SCL>
            """);

        var index = SclSemanticIndexBuilder.Build(document);
        var report = FindSingle(index, SclSemanticKind.ReportControl);
        var dataSet = FindSingle(index, SclSemanticKind.DataSet);
        var ln0 = FindSingle(index, SclSemanticKind.LogicalNodeZero);
        var lNodeType = FindSingle(index, SclSemanticKind.LogicalNodeType);
        var connectedAp = FindSingle(index, SclSemanticKind.ConnectedAccessPoint);
        var ied = FindSingle(index, SclSemanticKind.Ied);
        var ap = FindSingle(index, SclSemanticKind.AccessPoint);

        Assert.IsTrue(
            index.References.GetOutgoing(report.Handle)
                .Any(edge =>
                    edge.Kind == SclReferenceKind.DataSetBinding &&
                    edge.Target == dataSet.Handle));

        Assert.IsTrue(
            index.References.GetOutgoing(ln0.Handle)
                .Any(edge =>
                    edge.Kind == SclReferenceKind.TypeDefinition &&
                    edge.Target == lNodeType.Handle));

        var communicationEdges =
            index.References.GetOutgoing(connectedAp.Handle);

        Assert.IsTrue(
            communicationEdges.Any(edge =>
                edge.Kind == SclReferenceKind.CommunicationBinding &&
                edge.Target == ied.Handle));

        Assert.IsTrue(
            communicationEdges.Any(edge =>
                edge.Kind == SclReferenceKind.CommunicationBinding &&
                edge.Target == ap.Handle));

        Assert.IsTrue(
            index.References.GetIncoming(dataSet.Handle)
                .Any(edge => edge.Source == report.Handle));
    }

    [TestMethod]
    public async Task ReferenceGraphResolvesSmvCommunicationEndpointToSampledValueControl()
    {
        var document = await LoadAsync("""
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <Communication>
                <SubNetwork name="ProcessBus">
                  <ConnectedAP iedName="MU_A" apName="P1">
                    <SMV ldInst="MU" cbName="MSVCB01">
                      <Address>
                        <P type="MAC-Address">01-0C-CD-04-00-01</P>
                        <P type="APPID">4001</P>
                      </Address>
                    </SMV>
                  </ConnectedAP>
                </SubNetwork>
              </Communication>

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

        var index = SclSemanticIndexBuilder.Build(document);

        var endpoint = FindSingle(index, SclSemanticKind.SmvCommunication);
        var control = FindSingle(index, SclSemanticKind.SampledValueControl);

        Assert.IsTrue(
            index.References.GetOutgoing(endpoint.Handle)
                .Any(edge =>
                    edge.Kind ==
                        SclReferenceKind.CommunicationControlBinding &&
                    edge.Target == control.Handle));

        Assert.IsTrue(
            index.References.GetIncoming(control.Handle)
                .Any(edge =>
                    edge.Kind ==
                        SclReferenceKind.CommunicationControlBinding &&
                    edge.Source == endpoint.Handle));

        Assert.IsFalse(index.References.Issues.Any(issue =>
            issue.Source == endpoint.Handle));
    }

    private static int Count(
        SclSemanticIndex index,
        SclSemanticKind kind)
    {
        var count = 0;

        foreach (var node in index.Nodes)
        {
            if (node.Kind == kind)
            {
                count++;
            }
        }

        return count;
    }

    private static SclSemanticNode FindSingle(
        SclSemanticIndex index,
        SclSemanticKind kind)
    {
        SclSemanticNode? result = null;

        foreach (var node in index.Nodes)
        {
            if (node.Kind != kind)
            {
                continue;
            }

            Assert.IsNull(result);
            result = node;
        }

        Assert.IsNotNull(result);
        return result;
    }

    private static async Task<SclSyntaxDocument> LoadAsync(string xml)
    {
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(xml));

        return await new SclDocumentLoader()
            .LoadAsync(stream, "test.scd");
    }
}
