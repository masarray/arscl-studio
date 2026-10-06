using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Navigation;

public sealed record SclReportWorkspaceProjection(
    SclNodeHandle Handle,
    string LogicalDevice,
    string LogicalNode,
    string Name,
    string Kind,
    string DataSet,
    string ReportId,
    string ConfigurationRevision,
    string BufferTime,
    string IntegrityPeriod,
    string MaxClients,
    string TriggerSummary,
    string OptionalFieldsSummary);

public static class SclReportWorkspaceProjector
{
    public static SclReportWorkspaceProjection[] Build(
        SclDocumentState state,
        SclNodeHandle iedHandle)
    {
        ArgumentNullException.ThrowIfNull(state);

        var rows = new List<SclReportWorkspaceProjection>();

        foreach (var node in state.SemanticIndex.Nodes)
        {
            if (node.Kind is not (SclSemanticKind.ReportControl or SclSemanticKind.LogControl) ||
                !BelongsToIed(state, node, iedHandle))
            {
                continue;
            }

            if (node.Kind == SclSemanticKind.LogControl)
            {
                rows.Add(new SclReportWorkspaceProjection(
                    node.Handle,
                    AncestorName(state, node.Handle, SclSemanticKind.LogicalDevice),
                    AncestorName(
                        state,
                        node.Handle,
                        SclSemanticKind.LogicalNodeZero,
                        SclSemanticKind.LogicalNode),
                    node.DisplayName,
                    "Log",
                    SclWorkspaceSyntaxReader.Attribute(state.Syntax, node.Handle, "datSet"),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    SclWorkspaceSyntaxReader.Attribute(state.Syntax, node.Handle, "intgPd"),
                    string.Empty,
                    BuildTriggerSummary(state, node.Handle),
                    string.Empty));
                continue;
            }

            var buffered = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "buffered");

            rows.Add(new SclReportWorkspaceProjection(
                node.Handle,
                AncestorName(state, node.Handle, SclSemanticKind.LogicalDevice),
                AncestorName(
                    state,
                    node.Handle,
                    SclSemanticKind.LogicalNodeZero,
                    SclSemanticKind.LogicalNode),
                node.DisplayName,
                string.Equals(buffered, "true", StringComparison.OrdinalIgnoreCase)
                    ? "BRCB"
                    : "URCB",
                SclWorkspaceSyntaxReader.Attribute(state.Syntax, node.Handle, "datSet"),
                SclWorkspaceSyntaxReader.Attribute(state.Syntax, node.Handle, "rptID"),
                SclWorkspaceSyntaxReader.Attribute(state.Syntax, node.Handle, "confRev"),
                SclWorkspaceSyntaxReader.Attribute(state.Syntax, node.Handle, "bufTime"),
                SclWorkspaceSyntaxReader.Attribute(state.Syntax, node.Handle, "intgPd"),
                SclWorkspaceSyntaxReader.ChildAttribute(
                    state.Syntax,
                    node.Handle,
                    "RptEnabled",
                    "max"),
                BuildTriggerSummary(state, node.Handle),
                BuildOptionalFieldsSummary(state, node.Handle)));
        }

        return
        [
            .. rows.OrderBy(static row => row.LogicalDevice, StringComparer.OrdinalIgnoreCase)
                   .ThenBy(static row => row.LogicalNode, StringComparer.OrdinalIgnoreCase)
                   .ThenBy(static row => row.Kind, StringComparer.Ordinal)
                   .ThenBy(static row => row.Name, StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static string BuildTriggerSummary(
        SclDocumentState state,
        SclNodeHandle control)
    {
        if (!SclWorkspaceSyntaxReader.TryFindDirectElement(
                state.Syntax,
                control,
                "TrgOps",
                out var trgOps))
        {
            return string.Empty;
        }

        return JoinEnabled(
            ("dchg", "Data"),
            ("qchg", "Quality"),
            ("dupd", "Update"),
            ("period", "Period"),
            ("gi", "GI"));

        string JoinEnabled(params (string Attribute, string Label)[] options)
        {
            return string.Join(
                ", ",
                options.Where(option =>
                        string.Equals(
                            SclWorkspaceSyntaxReader.Attribute(
                                state.Syntax,
                                trgOps,
                                option.Attribute),
                            "true",
                            StringComparison.OrdinalIgnoreCase))
                    .Select(static option => option.Label));
        }
    }

    private static string BuildOptionalFieldsSummary(
        SclDocumentState state,
        SclNodeHandle control)
    {
        if (!SclWorkspaceSyntaxReader.TryFindDirectElement(
                state.Syntax,
                control,
                "OptFields",
                out var fields))
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            new[]
            {
                ("seqNum", "Seq"),
                ("timeStamp", "Time"),
                ("dataSet", "DataSet"),
                ("reasonCode", "Reason"),
                ("dataRef", "DataRef"),
                ("entryID", "EntryID"),
                ("configRef", "ConfRev")
            }.Where(option =>
                string.Equals(
                    SclWorkspaceSyntaxReader.Attribute(
                        state.Syntax,
                        fields,
                        option.Item1),
                    "true",
                    StringComparison.OrdinalIgnoreCase))
              .Select(static option => option.Item2));
    }

    private static bool BelongsToIed(
        SclDocumentState state,
        SclSemanticNode node,
        SclNodeHandle iedHandle) =>
        state.SemanticIndex.TryFindAncestor(
            node.Handle,
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
