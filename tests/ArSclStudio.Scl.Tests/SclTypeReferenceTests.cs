using System.Text;
using ArSclStudio.Scl.Semantics;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Scl.Tests;

[TestClass]
public sealed class SclTypeReferenceTests
{
    [TestMethod]
    public async Task ReferenceGraphResolvesLnDoDaTemplateChain()
    {
        const string xml = """
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="Relay_A">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN lnClass="XCBR" inst="1" lnType="XCBR_TYPE" />
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
              <DataTypeTemplates>
                <LNodeType id="XCBR_TYPE" lnClass="XCBR">
                  <DO name="Pos" type="DPC_TYPE" />
                </LNodeType>
                <DOType id="DPC_TYPE" cdc="DPC">
                  <DA name="stVal" bType="Enum" type="DPC_ENUM" fc="ST" />
                </DOType>
                <EnumType id="DPC_ENUM" />
              </DataTypeTemplates>
            </SCL>
            """;

        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(xml));

        var document = await new SclDocumentLoader()
            .LoadAsync(stream, "types.scd");

        var index = SclSemanticIndexBuilder.Build(document);

        var ln = Find(index, SclSemanticKind.LogicalNode);
        var lNodeType = Find(index, SclSemanticKind.LogicalNodeType);
        var dataObject = Find(index, SclSemanticKind.DataObjectDefinition);
        var doType = Find(index, SclSemanticKind.DataObjectType);
        var dataAttribute = Find(index, SclSemanticKind.DataAttributeDefinition);
        var enumType = Find(index, SclSemanticKind.EnumerationType);

        AssertEdge(index, ln, lNodeType);
        AssertEdge(index, dataObject, doType);
        AssertEdge(index, dataAttribute, enumType);
    }

    [TestMethod]
    public async Task ExternalReferenceResolvesRemoteLogicalNode()
    {
        const string xml = """
            <SCL xmlns="http://www.iec.ch/61850/2003/SCL">
              <IED name="Publisher">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN lnClass="XCBR" inst="1" lnType="XCBR_TYPE" />
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
              <IED name="Subscriber">
                <AccessPoint name="P1">
                  <Server>
                    <LDevice inst="LD0">
                      <LN0 lnClass="LLN0" inst="">
                        <Inputs>
                          <ExtRef
                            iedName="Publisher"
                            ldInst="LD0"
                            lnClass="XCBR"
                            lnInst="1"
                            doName="Pos"
                            daName="stVal" />
                        </Inputs>
                      </LN0>
                    </LDevice>
                  </Server>
                </AccessPoint>
              </IED>
              <DataTypeTemplates>
                <LNodeType id="XCBR_TYPE" lnClass="XCBR" />
              </DataTypeTemplates>
            </SCL>
            """;

        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(xml));

        var document = await new SclDocumentLoader()
            .LoadAsync(stream, "extref.scd");

        var index = SclSemanticIndexBuilder.Build(document);
        var extRef = Find(index, SclSemanticKind.ExternalReference);
        SclSemanticNode? publisherLn = null;

        foreach (var node in index.Nodes)
        {
            if (node.Kind == SclSemanticKind.LogicalNode &&
                node.DisplayName == "XCBR1")
            {
                publisherLn = node;
                break;
            }
        }

        Assert.IsNotNull(publisherLn);

        var edges = index.References.GetOutgoing(extRef.Handle);

        Assert.IsTrue(
            edges.Any(edge =>
                edge.Kind == SclReferenceKind.ExternalSource &&
                edge.Target == publisherLn.Handle &&
                edge.ReferenceText == "Pos.stVal"));
    }

    private static void AssertEdge(
        SclSemanticIndex index,
        SclSemanticNode source,
        SclSemanticNode target)
    {
        Assert.IsTrue(
            index.References.GetOutgoing(source.Handle)
                .Any(edge =>
                    edge.Kind == SclReferenceKind.TypeDefinition &&
                    edge.Target == target.Handle));
    }

    private static SclSemanticNode Find(
        SclSemanticIndex index,
        SclSemanticKind kind)
    {
        foreach (var node in index.Nodes)
        {
            if (node.Kind == kind)
            {
                return node;
            }
        }

        Assert.Fail($"Node {kind} was not found.");
        throw new InvalidOperationException();
    }
}
