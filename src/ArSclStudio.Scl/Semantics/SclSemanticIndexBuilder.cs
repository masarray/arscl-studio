using System.Xml;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Scl.Semantics;

public static class SclSemanticIndexBuilder
{
    public static SclSemanticIndex Build(SclSyntaxDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.TryGetNode(document.RootHandle, out var rootNode) ||
            rootNode is not XmlElement root)
        {
            throw new InvalidOperationException(
                "The SCL syntax document does not expose a valid root element.");
        }

        var nodes = new Dictionary<SclNodeHandle, SclSemanticNode>();
        var childLists = new Dictionary<SclNodeHandle, List<SclSemanticNode>>();
        var rootNamespace = root.NamespaceURI ?? string.Empty;

        var stack = new Stack<TraversalItem>();
        stack.Push(new TraversalItem(root, SclNodeHandle.None));

        while (stack.Count != 0)
        {
            var item = stack.Pop();
            var element = item.Element;
            var semanticParent = item.SemanticParent;

            if (IsSclElement(element, rootNamespace) &&
                TryClassify(element, rootNamespace, out var kind) &&
                document.TryGetHandle(element, out var handle))
            {
                var node = new SclSemanticNode(
                    handle,
                    semanticParent,
                    kind,
                    CreateDisplayName(element, kind),
                    CreateBadge(element, kind));

                nodes.Add(handle, node);

                if (!semanticParent.IsNone)
                {
                    if (!childLists.TryGetValue(semanticParent, out var list))
                    {
                        list = [];
                        childLists.Add(semanticParent, list);
                    }

                    list.Add(node);
                }

                semanticParent = handle;
            }

            for (var i = element.ChildNodes.Count - 1; i >= 0; i--)
            {
                if (element.ChildNodes[i] is XmlElement child)
                {
                    stack.Push(new TraversalItem(child, semanticParent));
                }
            }
        }

        var children = new Dictionary<SclNodeHandle, SclSemanticNode[]>(
            childLists.Count);

        foreach (var pair in childLists)
        {
            children.Add(pair.Key, [.. pair.Value]);
        }

        var preliminary = new SclSemanticIndex(
            nodes,
            children,
            new SclReferenceGraph([]));

        var references = SclReferenceGraphBuilder.Build(
            document,
            preliminary);

        return new SclSemanticIndex(nodes, children, references);
    }

    private static bool IsSclElement(
        XmlElement element,
        string rootNamespace) =>
        string.Equals(
            element.NamespaceURI ?? string.Empty,
            rootNamespace,
            StringComparison.Ordinal);

    private static bool TryClassify(
        XmlElement element,
        string rootNamespace,
        out SclSemanticKind kind)
    {
        if (IsDirectServiceCapability(
                element,
                rootNamespace))
        {
            kind = SclSemanticKind.ServiceCapability;
            return true;
        }

        return TryClassify(
            element.LocalName,
            out kind);
    }

    private static bool IsDirectServiceCapability(
        XmlElement element,
        string rootNamespace) =>
        !string.Equals(
            element.LocalName,
            "Private",
            StringComparison.Ordinal) &&
        element.ParentNode is XmlElement parent &&
        string.Equals(
            parent.NamespaceURI ?? string.Empty,
            rootNamespace,
            StringComparison.Ordinal) &&
        string.Equals(
            parent.LocalName,
            "Services",
            StringComparison.Ordinal);

    private static bool TryClassify(
        string localName,
        out SclSemanticKind kind)
    {
        kind = localName switch
        {
            "SCL" => SclSemanticKind.Document,
            "Header" => SclSemanticKind.Header,
            "Substation" => SclSemanticKind.Substation,
            "Communication" => SclSemanticKind.Communication,
            "SubNetwork" => SclSemanticKind.SubNetwork,
            "ConnectedAP" => SclSemanticKind.ConnectedAccessPoint,
            "GSE" => SclSemanticKind.GseCommunication,
            "SMV" => SclSemanticKind.SmvCommunication,
            "Address" => SclSemanticKind.Address,
            "IED" => SclSemanticKind.Ied,
            "Services" => SclSemanticKind.Services,
            "AccessPoint" => SclSemanticKind.AccessPoint,
            "Server" => SclSemanticKind.Server,
            "LDevice" => SclSemanticKind.LogicalDevice,
            "LN0" => SclSemanticKind.LogicalNodeZero,
            "LN" => SclSemanticKind.LogicalNode,
            "DataSet" => SclSemanticKind.DataSet,
            "FCDA" => SclSemanticKind.Fcda,
            "ReportControl" => SclSemanticKind.ReportControl,
            "LogControl" => SclSemanticKind.LogControl,
            "GSEControl" => SclSemanticKind.GseControl,
            "SampledValueControl" => SclSemanticKind.SampledValueControl,
            "Inputs" => SclSemanticKind.Inputs,
            "ExtRef" => SclSemanticKind.ExternalReference,
            "SettingControl" => SclSemanticKind.SettingGroupControl,
            "DOI" => SclSemanticKind.Doi,
            "SDI" => SclSemanticKind.Sdi,
            "DAI" => SclSemanticKind.Dai,
            "DataTypeTemplates" => SclSemanticKind.DataTypeTemplates,
            "LNodeType" => SclSemanticKind.LogicalNodeType,
            "DOType" => SclSemanticKind.DataObjectType,
            "DAType" => SclSemanticKind.DataAttributeType,
            "EnumType" => SclSemanticKind.EnumerationType,
            "DO" => SclSemanticKind.DataObjectDefinition,
            "SDO" => SclSemanticKind.SubDataObjectDefinition,
            "DA" => SclSemanticKind.DataAttributeDefinition,
            "BDA" => SclSemanticKind.BasicDataAttributeDefinition,
            "Private" => SclSemanticKind.Private,
            _ => default
        };

        return localName is
            "SCL" or
            "Header" or
            "Substation" or
            "Communication" or
            "SubNetwork" or
            "ConnectedAP" or
            "GSE" or
            "SMV" or
            "Address" or
            "IED" or
            "Services" or
            "AccessPoint" or
            "Server" or
            "LDevice" or
            "LN0" or
            "LN" or
            "DataSet" or
            "FCDA" or
            "ReportControl" or
            "LogControl" or
            "GSEControl" or
            "SampledValueControl" or
            "Inputs" or
            "ExtRef" or
            "SettingControl" or
            "DOI" or
            "SDI" or
            "DAI" or
            "DataTypeTemplates" or
            "LNodeType" or
            "DOType" or
            "DAType" or
            "EnumType" or
            "DO" or
            "SDO" or
            "DA" or
            "BDA" or
            "Private";
    }

    private static string CreateDisplayName(
        XmlElement element,
        SclSemanticKind kind) =>
        kind switch
        {
            SclSemanticKind.Document => "SCL",
            SclSemanticKind.ServiceCapability => element.LocalName,
            SclSemanticKind.LogicalNodeZero or
            SclSemanticKind.LogicalNode => CreateLogicalNodeName(element),
            SclSemanticKind.LogicalDevice =>
                NonEmpty(element.GetAttribute("inst"), "LDevice"),
            SclSemanticKind.Fcda => CreateFcdaName(element),
            SclSemanticKind.ConnectedAccessPoint =>
                CreateConnectedAccessPointName(element),
            SclSemanticKind.GseCommunication or
            SclSemanticKind.SmvCommunication =>
                CreateCommunicationEndpointName(element),
            SclSemanticKind.ExternalReference => CreateExtRefName(element),
            _ => FirstNonEmpty(
                element.GetAttribute("name"),
                element.GetAttribute("id"),
                element.GetAttribute("type"),
                element.LocalName)
        };

    private static string? CreateBadge(
        XmlElement element,
        SclSemanticKind kind) =>
        kind switch
        {
            SclSemanticKind.Ied => NullIfEmpty(element.GetAttribute("manufacturer")),
            SclSemanticKind.ReportControl or
            SclSemanticKind.LogControl or
            SclSemanticKind.GseControl or
            SclSemanticKind.SampledValueControl =>
                NullIfEmpty(element.GetAttribute("datSet")),
            SclSemanticKind.DataAttributeDefinition or
            SclSemanticKind.BasicDataAttributeDefinition =>
                NullIfEmpty(element.GetAttribute("bType")),
            SclSemanticKind.LogicalNodeType =>
                NullIfEmpty(element.GetAttribute("lnClass")),
            _ => null
        };

    private static string CreateLogicalNodeName(XmlElement element)
    {
        var prefix = element.GetAttribute("prefix");
        var lnClass = element.GetAttribute("lnClass");
        var inst = element.GetAttribute("inst");

        if (string.IsNullOrWhiteSpace(lnClass))
        {
            return element.LocalName;
        }

        return string.Concat(prefix, lnClass, inst);
    }

    private static string CreateFcdaName(XmlElement element)
    {
        var ldInst = element.GetAttribute("ldInst");
        var prefix = element.GetAttribute("prefix");
        var lnClass = element.GetAttribute("lnClass");
        var lnInst = element.GetAttribute("lnInst");
        var doName = element.GetAttribute("doName");
        var daName = element.GetAttribute("daName");
        var fc = element.GetAttribute("fc");

        var ln = string.Concat(prefix, lnClass, lnInst);
        var data = string.IsNullOrWhiteSpace(daName)
            ? doName
            : string.Concat(doName, ".", daName);

        var reference = string.IsNullOrWhiteSpace(ldInst)
            ? string.Concat(ln, "/", data)
            : string.Concat(ldInst, "/", ln, "/", data);

        return string.IsNullOrWhiteSpace(fc)
            ? reference
            : string.Concat(reference, " [", fc, "]");
    }

    private static string CreateCommunicationEndpointName(XmlElement element)
    {
        var ldInst = element.GetAttribute("ldInst");
        var cbName = element.GetAttribute("cbName");

        if (string.IsNullOrWhiteSpace(ldInst))
        {
            return NonEmpty(cbName, element.LocalName);
        }

        return string.IsNullOrWhiteSpace(cbName)
            ? ldInst
            : string.Concat(ldInst, " / ", cbName);
    }

    private static string CreateConnectedAccessPointName(XmlElement element)
    {
        var iedName = element.GetAttribute("iedName");
        var apName = element.GetAttribute("apName");

        if (string.IsNullOrWhiteSpace(iedName))
        {
            return NonEmpty(apName, "ConnectedAP");
        }

        return string.IsNullOrWhiteSpace(apName)
            ? iedName
            : string.Concat(iedName, " / ", apName);
    }

    private static string CreateExtRefName(XmlElement element)
    {
        var intAddr = element.GetAttribute("intAddr");

        if (!string.IsNullOrWhiteSpace(intAddr))
        {
            return intAddr;
        }

        var iedName = element.GetAttribute("iedName");
        var ldInst = element.GetAttribute("ldInst");
        var prefix = element.GetAttribute("prefix");
        var lnClass = element.GetAttribute("lnClass");
        var lnInst = element.GetAttribute("lnInst");
        var doName = element.GetAttribute("doName");
        var daName = element.GetAttribute("daName");

        var source = string.Concat(
            iedName,
            "/",
            ldInst,
            "/",
            prefix,
            lnClass,
            lnInst,
            "/",
            doName);

        return string.IsNullOrWhiteSpace(daName)
            ? source
            : string.Concat(source, ".", daName);
    }

    private static string FirstNonEmpty(
        string first,
        string second,
        string third,
        string fallback)
    {
        if (!string.IsNullOrWhiteSpace(first))
        {
            return first;
        }

        if (!string.IsNullOrWhiteSpace(second))
        {
            return second;
        }

        return !string.IsNullOrWhiteSpace(third)
            ? third
            : fallback;
    }

    private static string NonEmpty(
        string value,
        string fallback) =>
        string.IsNullOrWhiteSpace(value)
            ? fallback
            : value;

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value;

    private readonly record struct TraversalItem(
        XmlElement Element,
        SclNodeHandle SemanticParent);
}
