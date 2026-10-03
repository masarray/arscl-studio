using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Navigation;

public static class SclNodeDetailsProjector
{
    public static SclNodeDetailsProjection Create(
        SclDocumentState state,
        SclNodeHandle handle)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.Syntax.TryGetNodeInfo(handle, out var info) ||
            info is null)
        {
            return new SclNodeDetailsProjection(
                handle,
                "Unresolved node",
                "Unknown",
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                "The selected node is no longer available in this document revision.");
        }

        var semanticNode = state.SemanticIndex.TryGetNode(
            handle,
            out var semantic) &&
            semantic is not null
                ? semantic
                : null;

        var title = semanticNode?.DisplayName ??
            GetPrimaryName(state.Syntax, info);

        var kind = semanticNode?.Kind.ToString() ??
            GetKindName(info);
        var path = BuildPath(state, info);
        var sourceLocation = info.SourceSpan.IsKnown
            ? $"{state.DisplayName}:{info.SourceSpan}"
            : state.DisplayName;

        return new SclNodeDetailsProjection(
            handle,
            title,
            kind,
            path,
            sourceLocation,
            info.NamespaceUri,
            info.Value,
            GetDescription(info));
    }

    private static string GetPrimaryName(
        SclSyntaxDocument syntax,
        SclSyntaxNodeInfo info)
    {
        if (info.Kind == SclSyntaxNodeKind.Attribute)
        {
            return string.IsNullOrWhiteSpace(info.Prefix)
                ? $"@{info.LocalName}"
                : $"@{info.Prefix}:{info.LocalName}";
        }

        if (info.Kind != SclSyntaxNodeKind.Element)
        {
            return info.LocalName;
        }

        if (syntax.TryGetAttributeValue(info.Handle, "name", out var name) &&
            !string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (syntax.TryGetAttributeValue(info.Handle, "id", out var id) &&
            !string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        if (syntax.TryGetAttributeValue(info.Handle, "type", out var type) &&
            !string.IsNullOrWhiteSpace(type))
        {
            return type;
        }

        return info.LocalName;
    }

    private static string GetKindName(SclSyntaxNodeInfo info) =>
        info.Kind == SclSyntaxNodeKind.Element
            ? info.LocalName
            : info.Kind.ToString();

    private static string BuildPath(
        SclDocumentState state,
        SclSyntaxNodeInfo selected)
    {
        var segments = new List<string>(12);
        var current = selected;

        for (var depth = 0; depth < 128; depth++)
        {
            segments.Add(GetPathSegment(state, current));

            if (current.Parent.IsNone ||
                !state.Syntax.TryGetNodeInfo(
                    current.Parent,
                    out var parent) ||
                parent is null)
            {
                break;
            }

            current = parent;
        }

        segments.Reverse();
        return string.Join(" / ", segments);
    }

    private static string GetPathSegment(
        SclDocumentState state,
        SclSyntaxNodeInfo info)
    {
        var syntax = state.Syntax;
        if (info.Kind == SclSyntaxNodeKind.Attribute)
        {
            return $"@{info.LocalName}";
        }

        if (info.Kind != SclSyntaxNodeKind.Element)
        {
            return info.Kind.ToString();
        }

        if (state.SemanticIndex.TryGetNode(
                info.Handle,
                out var semanticNode) &&
            semanticNode is not null &&
            !string.IsNullOrWhiteSpace(semanticNode.DisplayName) &&
            !string.Equals(
                semanticNode.DisplayName,
                info.LocalName,
                StringComparison.Ordinal))
        {
            return $"{info.LocalName}[{semanticNode.DisplayName}]";
        }

        if (syntax.TryGetAttributeValue(info.Handle, "name", out var name) &&
            !string.IsNullOrWhiteSpace(name))
        {
            return $"{info.LocalName}[{name}]";
        }

        if (syntax.TryGetAttributeValue(info.Handle, "id", out var id) &&
            !string.IsNullOrWhiteSpace(id))
        {
            return $"{info.LocalName}[{id}]";
        }

        return info.LocalName;
    }

    private static string GetDescription(SclSyntaxNodeInfo info) =>
        info.Kind == SclSyntaxNodeKind.Element
            ? info.LocalName switch
            {
                "SCL" => "Root of the IEC 61850 System Configuration Language document.",
                "Header" => "Document identification and engineering history metadata.",
                "Substation" => "Primary-system structure and its logical-node mappings.",
                "Communication" => "Communication-network configuration and IED access-point bindings.",
                "IED" => "An IED definition containing capabilities, access points, servers, logical devices, and configured services.",
                "Services" => "IEC 61850 service and configuration capabilities declared by this IED in SCL. Runtime support is a separate verification concern.",
                "DynAssociation" => "SCL declares dynamic association capability for this IED.",
                "SettingGroups" => "SCL declares setting-group service capability. Nested declarations describe setting-group operations such as SGEdit.",
                "SGEdit" => "SCL declares setting-group edit capability.",
                "GetDirectory" => "SCL declares directory retrieval capability.",
                "GetDataObjectDefinition" => "SCL declares retrieval of data-object definitions.",
                "DataObjectDirectory" => "SCL declares data-object directory capability.",
                "GetDataSetValue" => "SCL declares DataSet value retrieval capability.",
                "SetDataSetValue" => "SCL declares DataSet value writing capability.",
                "DataSetDirectory" => "SCL declares DataSet directory capability.",
                "ConfDataSet" => "SCL declaration for configurable DataSets and any stated limits or modification flag.",
                "DynDataSet" => "SCL declaration for dynamically created DataSets and any stated limits.",
                "ReadWrite" => "SCL declares read/write data service capability.",
                "ConfReportControl" => "SCL declaration for configurable report-control capability and any stated maximum.",
                "GetCBValues" => "SCL declares retrieval of control-block values.",
                "ReportSettings" => "SCL declares whether report-control settings are fixed, configurable, or dynamic.",
                "GSESettings" => "SCL declares whether GSE control settings are fixed, configurable, or dynamic.",
                "GOOSE" => "SCL declaration for GOOSE service capability and any stated maximum. This is not runtime verification.",
                "GSSE" => "SCL declaration for GSSE service capability and any stated maximum. This is not runtime verification.",
                "SMV" => "SCL declaration for Sampled Values service capability and any stated maximum. This is not runtime verification.",
                "SMVSettings" => "SCL declares whether sampled-value control settings are fixed, configurable, or dynamic.",
                "ConfLNs" => "SCL declaration describing whether logical-node prefix and instance identities are fixed.",
                "FileHandling" => "SCL declares file-handling service capability.",
                "AccessPoint" => "Named communication access point of an IED.",
                "Server" => "IEC 61850 server model hosted by an access point.",
                "LDevice" => "Logical Device containing LN0 and logical nodes.",
                "LN0" => "LLN0: common logical-node services, datasets, control blocks, inputs, and setting-group configuration for a logical device.",
                "LN" => "Logical Node instance linked to an LNodeType definition.",
                "DataSet" => "Ordered collection of FCDA members referenced by reporting, GOOSE, sampled values, logging, or client engineering.",
                "FCDA" => "Functional-constraint data reference belonging to a DataSet.",
                "ReportControl" => "Report Control Block configuration. Its datSet reference is tracked by the ARSCL reference graph.",
                "LogControl" => "Log Control configuration and DataSet binding.",
                "GSEControl" => "GOOSE publisher control block and DataSet binding.",
                "SampledValueControl" => "Sampled Value publisher control block and DataSet binding.",
                "Inputs" => "External-reference subscriptions configured for a logical node.",
                "ExtRef" => "External signal reference. Resolved source relationships appear in Where Used/References when sufficient SCL identity is present.",
                "SettingControl" => "Setting-group control configuration for LN0.",
                "DOI" => "Configured Data Object instance values or overrides under a logical node.",
                "SDI" => "Configured sub-data instance.",
                "DAI" => "Configured Data Attribute instance value or override.",
                "DataTypeTemplates" => "Shared IEC 61850 logical-node, data-object, data-attribute, and enumeration type definitions.",
                "LNodeType" => "Logical Node type template referenced by LN/LN0 through lnType.",
                "DOType" => "Data Object type template referenced by DO/SDO definitions.",
                "DAType" => "Structured Data Attribute type template.",
                "EnumType" => "Enumeration type template referenced by Enum data attributes.",
                "Private" => "Vendor or tool-specific extension content. ARSCL preserves this content even when its semantics are unknown.",
                _ => "IEC 61850 SCL structure. Deeper semantic explanation will be added as the model index expands."
            }
            : "XML syntax node belonging to the same authoritative SCL document.";
}
