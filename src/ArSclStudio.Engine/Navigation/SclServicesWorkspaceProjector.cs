using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;
using ArSclStudio.Scl.Syntax;

namespace ArSclStudio.Engine.Navigation;

public sealed record SclServiceCapabilityProjection(
    SclNodeHandle Handle,
    string Path,
    string Name,
    string Category,
    string Declaration,
    string Parameters,
    string Interpretation,
    bool IsKnown);

public static class SclServicesWorkspaceProjector
{
    private const int MaxServiceDepth = 8;

    public static SclServiceCapabilityProjection[] Build(
        SclDocumentState state,
        SclNodeHandle iedHandle)
    {
        ArgumentNullException.ThrowIfNull(state);

        var services = FindServices(
            state,
            iedHandle);

        if (services.IsNone ||
            !state.Syntax.TryGetNodeInfo(
                services,
                out var servicesInfo) ||
            servicesInfo is null)
        {
            return [];
        }

        var rows =
            new List<SclServiceCapabilityProjection>();

        AppendDirectServiceElements(
            state,
            services,
            servicesInfo.NamespaceUri,
            parentPath: string.Empty,
            depth: 0,
            rows);

        return [.. rows];
    }

    private static SclNodeHandle FindServices(
        SclDocumentState state,
        SclNodeHandle iedHandle)
    {
        var children =
            state.SemanticIndex.GetChildren(
                iedHandle);

        for (var i = 0; i < children.Count; i++)
        {
            if (children[i].Kind ==
                SclSemanticKind.Services)
            {
                return children[i].Handle;
            }
        }

        return SclNodeHandle.None;
    }

    private static void AppendDirectServiceElements(
        SclDocumentState state,
        SclNodeHandle parent,
        string sclNamespace,
        string parentPath,
        int depth,
        List<SclServiceCapabilityProjection> rows)
    {
        if (depth >= MaxServiceDepth)
        {
            return;
        }

        var children =
            state.Syntax.GetSelectableChildren(
                parent);

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];

            if (!state.Syntax.TryGetNodeInfo(
                    child,
                    out var info) ||
                info is null ||
                info.Kind != SclSyntaxNodeKind.Element ||
                !string.Equals(
                    info.NamespaceUri,
                    sclNamespace,
                    StringComparison.Ordinal))
            {
                continue;
            }

            var name = info.LocalName;
            var path = string.IsNullOrWhiteSpace(
                    parentPath)
                ? name
                : string.Concat(
                    parentPath,
                    " / ",
                    name);

            var attributes =
                ReadDirectAttributes(
                    state,
                    child);

            var subElementCount =
                CountDirectElements(
                    state,
                    child,
                    sclNamespace);

            var known = TryGetCategory(
                name,
                out var category);

            rows.Add(
                new SclServiceCapabilityProjection(
                    child,
                    path,
                    name,
                    category,
                    "Declared in SCL",
                    FormatRawParameters(attributes),
                    BuildInterpretation(
                        attributes,
                        subElementCount,
                        known),
                    known));

            AppendDirectServiceElements(
                state,
                child,
                sclNamespace,
                path,
                depth + 1,
                rows);
        }
    }

    private static List<ServiceAttribute>
        ReadDirectAttributes(
            SclDocumentState state,
            SclNodeHandle element)
    {
        var result =
            new List<ServiceAttribute>();

        var children =
            state.Syntax.GetSelectableChildren(
                element);

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];

            if (!state.Syntax.TryGetNodeInfo(
                    child,
                    out var info) ||
                info is null ||
                info.Kind !=
                    SclSyntaxNodeKind.Attribute ||
                info.Parent != element)
            {
                continue;
            }

            var name = string.IsNullOrWhiteSpace(
                    info.Prefix)
                ? info.LocalName
                : string.Concat(
                    info.Prefix,
                    ":",
                    info.LocalName);

            result.Add(
                new ServiceAttribute(
                    name,
                    info.LocalName,
                    info.Value ?? string.Empty));
        }

        return result;
    }

    private static int CountDirectElements(
        SclDocumentState state,
        SclNodeHandle parent,
        string sclNamespace)
    {
        var children =
            state.Syntax.GetSelectableChildren(
                parent);

        var count = 0;

        for (var i = 0; i < children.Count; i++)
        {
            if (state.Syntax.TryGetNodeInfo(
                    children[i],
                    out var info) &&
                info is not null &&
                info.Kind ==
                    SclSyntaxNodeKind.Element &&
                string.Equals(
                    info.NamespaceUri,
                    sclNamespace,
                    StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    private static string FormatRawParameters(
        List<ServiceAttribute> attributes)
    {
        if (attributes.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            " · ",
            attributes.Select(
                static attribute =>
                    string.Concat(
                        attribute.DisplayName,
                        "=",
                        attribute.Value)));
    }

    private static string BuildInterpretation(
        List<ServiceAttribute> attributes,
        int subElementCount,
        bool known)
    {
        if (attributes.Count == 0)
        {
            if (subElementCount > 0)
            {
                return subElementCount == 1
                    ? "Declared; 1 sub-capability"
                    : $"Declared; {subElementCount} sub-capabilities";
            }

            return known
                ? "Declared"
                : "Declared; semantics not interpreted by ARSCL";
        }

        var parts =
            new List<string>(
                attributes.Count + 1);

        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];

            parts.Add(
                InterpretAttribute(
                    attribute));
        }

        if (subElementCount > 0)
        {
            parts.Add(
                subElementCount == 1
                    ? "1 sub-capability"
                    : $"{subElementCount} sub-capabilities");
        }

        if (!known)
        {
            parts.Add(
                "semantics not interpreted by ARSCL");
        }

        return string.Join(
            " · ",
            parts);
    }

    private static string InterpretAttribute(
        ServiceAttribute attribute)
    {
        return attribute.LocalName switch
        {
            "max" =>
                string.Concat(
                    "Maximum: ",
                    attribute.Value),

            "maxAttributes" =>
                string.Concat(
                    "Maximum attributes: ",
                    attribute.Value),

            "modify" =>
                string.Concat(
                    "Modification: ",
                    InterpretBoolean(
                        attribute.Value,
                        trueText: "allowed",
                        falseText: "not allowed")),

            "fixPrefix" =>
                string.Concat(
                    "Prefix: ",
                    InterpretBoolean(
                        attribute.Value,
                        trueText: "fixed",
                        falseText: "not fixed")),

            "fixLnInst" =>
                string.Concat(
                    "LN instance: ",
                    InterpretBoolean(
                        attribute.Value,
                        trueText: "fixed",
                        falseText: "not fixed")),

            "cbName" or
            "datSet" or
            "rptID" or
            "optFields" or
            "bufTime" or
            "trgOps" or
            "intgPd" or
            "appID" or
            "dataLabel" =>
                string.Concat(
                    attribute.LocalName,
                    ": ",
                    InterpretSettingMode(
                        attribute.Value)),

            _ =>
                string.Concat(
                    attribute.LocalName,
                    ": ",
                    attribute.Value)
        };
    }

    private static string InterpretBoolean(
        string value,
        string trueText,
        string falseText)
    {
        if (bool.TryParse(
                value,
                out var parsed))
        {
            return parsed
                ? trueText
                : falseText;
        }

        return value;
    }

    private static string InterpretSettingMode(
        string value) =>
        value switch
        {
            "Fix" => "Fixed",
            "Conf" => "Configurable",
            "Dyn" => "Dynamic",
            _ => value
        };

    private static bool TryGetCategory(
        string serviceName,
        out string category)
    {
        category = serviceName switch
        {
            "DynAssociation" =>
                "Association",

            "GetDirectory" or
            "GetDataObjectDefinition" or
            "DataObjectDirectory" or
            "ReadWrite" =>
                "Data Model",

            "GetDataSetValue" or
            "SetDataSetValue" or
            "DataSetDirectory" or
            "ConfDataSet" or
            "DynDataSet" =>
                "DataSets",

            "ConfReportControl" or
            "GetCBValues" or
            "ReportSettings" =>
                "Reporting",

            "GSESettings" or
            "GOOSE" or
            "GSSE" =>
                "GSE",

            "SMVSettings" or
            "SMV" =>
                "Sampled Values",

            "SettingGroups" or
            "SGEdit" =>
                "Setting Groups",

            "ConfLNs" =>
                "Logical Nodes",

            "FileHandling" =>
                "Files",

            _ => "Other"
        };

        return !string.Equals(
            category,
            "Other",
            StringComparison.Ordinal);
    }

    private readonly record struct ServiceAttribute(
        string DisplayName,
        string LocalName,
        string Value);
}
