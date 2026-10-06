using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Navigation;

public sealed record SclSettingGroupControlProjection(
    SclNodeHandle Handle,
    string LogicalDevice,
    string LogicalNode,
    string NumberOfGroups,
    string ActiveGroup,
    string EditGroup,
    string Confirmation,
    string ReservationTime);

public sealed record SclSettingGroupSettingProjection(
    SclNodeHandle Handle,
    string LogicalDevice,
    string LogicalNode,
    string DataObject,
    string Setting,
    string Value,
    string Unit,
    string Minimum,
    string Maximum,
    string Step,
    string BasicType,
    string Description);

public static class SclSettingGroupWorkspaceProjector
{
    public static SclSettingGroupControlProjection[] BuildControls(
        SclDocumentState state,
        SclNodeHandle iedHandle)
    {
        ArgumentNullException.ThrowIfNull(state);

        var rows = new List<SclSettingGroupControlProjection>();

        foreach (var node in state.SemanticIndex.Nodes)
        {
            if (node.Kind != SclSemanticKind.SettingGroupControl ||
                !BelongsToIed(
                    state,
                    node.Handle,
                    iedHandle))
            {
                continue;
            }

            rows.Add(new SclSettingGroupControlProjection(
                node.Handle,
                AncestorName(
                    state,
                    node.Handle,
                    SclSemanticKind.LogicalDevice),
                AncestorName(
                    state,
                    node.Handle,
                    SclSemanticKind.LogicalNodeZero,
                    SclSemanticKind.LogicalNode),
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "numOfSGs"),
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "actSG"),
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "editSG"),
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "cnfEdit"),
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "resvTms")));
        }

        return
        [
            .. rows.OrderBy(
                    static row => row.LogicalDevice,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static row => row.LogicalNode,
                    StringComparer.OrdinalIgnoreCase)
        ];
    }

    public static SclSettingGroupSettingProjection[] BuildSettings(
        SclDocumentState state,
        SclNodeHandle settingControlHandle,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.SemanticIndex.TryGetNode(
                settingControlHandle,
                out var control) ||
            control is null ||
            control.Kind != SclSemanticKind.SettingGroupControl ||
            !state.SemanticIndex.TryFindAncestor(
                settingControlHandle,
                SclSemanticKind.LogicalDevice,
                out var logicalDevice) ||
            logicalDevice is null)
        {
            return [];
        }

        var rows = new List<SclSettingGroupSettingProjection>();
        var logicalNodes = state.SemanticIndex.GetChildren(
            logicalDevice.Handle);

        for (var i = 0; i < logicalNodes.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var logicalNode = logicalNodes[i];

            if (logicalNode.Kind is not (
                    SclSemanticKind.LogicalNodeZero or
                    SclSemanticKind.LogicalNode))
            {
                continue;
            }

            var modelRows = SclDataModelWorkspaceProjector.BuildRows(
                state,
                logicalNode.Handle);

            if (modelRows.Length == 0)
            {
                continue;
            }

            var byPath = new Dictionary<string, SclDataModelRowProjection>(
                modelRows.Length,
                StringComparer.Ordinal);

            for (var j = 0; j < modelRows.Length; j++)
            {
                if ((j & 31) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                byPath[modelRows[j].Path] = modelRows[j];
            }

            for (var j = 0; j < modelRows.Length; j++)
            {
                if ((j & 31) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var row = modelRows[j];

                if (!string.Equals(
                        row.FunctionalConstraint,
                        "SG",
                        StringComparison.Ordinal) ||
                    row.Kind is not ("DA" or "BDA") ||
                    string.Equals(
                        row.BasicType,
                        "Struct",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var segments = row.Path.Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length < 2)
                {
                    continue;
                }

                var dataObject = segments[0];
                var setting = string.Join(
                    '.',
                    segments.Skip(1));

                var leafSuffix = segments.Length > 2
                    ? string.Join(
                        '/',
                        segments.Skip(2))
                    : string.Empty;

                var unit = BuildUnit(
                    byPath,
                    dataObject);

                rows.Add(new SclSettingGroupSettingProjection(
                    row.Handle,
                    logicalDevice.DisplayName,
                    logicalNode.DisplayName,
                    dataObject,
                    setting,
                    row.Value,
                    unit,
                    FindBoundValue(
                        byPath,
                        dataObject,
                        "minVal",
                        leafSuffix),
                    FindBoundValue(
                        byPath,
                        dataObject,
                        "maxVal",
                        leafSuffix),
                    FindBoundValue(
                        byPath,
                        dataObject,
                        "stepSize",
                        leafSuffix),
                    row.BasicType,
                    FindDataObjectDescription(
                        byPath,
                        dataObject,
                        row.Description)));
            }
        }

        return
        [
            .. rows.OrderBy(
                    static row => row.LogicalNode,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static row => row.DataObject,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static row => row.Setting,
                    StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static string BuildUnit(
        Dictionary<string, SclDataModelRowProjection> byPath,
        string dataObject)
    {
        var unitPath = string.Concat(
            dataObject,
            "/units/SIUnit");

        if (!byPath.TryGetValue(
                unitPath,
                out var unitRow) ||
            string.IsNullOrWhiteSpace(unitRow.Value))
        {
            return string.Empty;
        }

        var multiplierPath = string.Concat(
            dataObject,
            "/units/multiplier");

        var multiplier = byPath.TryGetValue(
                multiplierPath,
                out var multiplierRow)
            ? multiplierRow.Value
            : string.Empty;

        return string.Concat(
            multiplier,
            unitRow.Value);
    }

    private static string FindBoundValue(
        Dictionary<string, SclDataModelRowProjection> byPath,
        string dataObject,
        string boundName,
        string leafSuffix)
    {
        var path = string.IsNullOrWhiteSpace(leafSuffix)
            ? string.Concat(
                dataObject,
                "/",
                boundName)
            : string.Concat(
                dataObject,
                "/",
                boundName,
                "/",
                leafSuffix);

        return byPath.TryGetValue(path, out var row)
            ? row.Value
            : string.Empty;
    }

    private static string FindDataObjectDescription(
        Dictionary<string, SclDataModelRowProjection> byPath,
        string dataObject,
        string fallback)
    {
        if (byPath.TryGetValue(
                dataObject,
                out var row) &&
            !string.IsNullOrWhiteSpace(row.Description))
        {
            return row.Description;
        }

        return fallback;
    }

    private static bool BelongsToIed(
        SclDocumentState state,
        SclNodeHandle handle,
        SclNodeHandle iedHandle) =>
        state.SemanticIndex.TryFindAncestor(
            handle,
            SclSemanticKind.Ied,
            out var ied) &&
        ied is not null &&
        ied.Handle == iedHandle;

    private static string AncestorName(
        SclDocumentState state,
        SclNodeHandle handle,
        params SclSemanticKind[] kinds)
    {
        for (var i = 0; i < kinds.Length; i++)
        {
            if (state.SemanticIndex.TryFindAncestor(
                    handle,
                    kinds[i],
                    out var node) &&
                node is not null)
            {
                return node.DisplayName;
            }
        }

        return string.Empty;
    }
}
