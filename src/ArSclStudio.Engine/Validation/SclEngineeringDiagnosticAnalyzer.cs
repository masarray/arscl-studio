using ArSclStudio.Engine.Diagnostics;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Validation;

internal static class SclEngineeringDiagnosticAnalyzer
{
    public static void AppendDiagnostics(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        bool includeDeepModelChecks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(diagnostics);

        AppendNetworkDiagnostics(
            state,
            diagnostics,
            cancellationToken);

        AppendGooseDiagnostics(
            state,
            diagnostics,
            cancellationToken);

        AppendSampledValueDiagnostics(
            state,
            diagnostics,
            cancellationToken);

        if (includeDeepModelChecks)
        {
            AppendDataModelDiagnostics(
                state,
                diagnostics,
                cancellationToken);
        }

        AppendSettingGroupDiagnostics(
            state,
            diagnostics,
            cancellationToken);
    }

    private static void AppendNetworkDiagnostics(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var rows = SclNetworkWorkspaceProjector.Build(state);

        var groups = rows
            .Where(static row =>
                !string.IsNullOrWhiteSpace(row.IpAddress))
            .GroupBy(
                static row => new NetworkAddressKey(
                    row.SubNetwork,
                    row.IpAddress),
                NetworkAddressKeyComparer.Instance)
            .Where(static group => group.Count() > 1);

        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var duplicates = group.ToArray();
            var participants = string.Join(
                ", ",
                duplicates.Select(static row =>
                    string.Concat(
                        row.IedName,
                        "/",
                        row.AccessPoint)));

            for (var i = 0; i < duplicates.Length; i++)
            {
                var row = duplicates[i];

                diagnostics.Add(CreateDiagnostic(
                    state,
                    "SCL-ENG-NET-0001",
                    DiagnosticSeverity.Warning,
                    row.Handle,
                    $"IP address '{row.IpAddress}' is used by {duplicates.Length} ConnectedAP objects on SubNetwork '{row.SubNetwork}': {participants}.",
                    "This is an engineering consistency warning, not a schema-invalidity claim. Verify whether the duplicate address is intentional for the station network design."));
            }
        }
    }

    private static void AppendGooseDiagnostics(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (var node in state.SemanticIndex.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node.Kind != SclSemanticKind.GseControl)
            {
                continue;
            }

            var serviceType = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "type");

            if (string.Equals(
                    serviceType,
                    "GSSE",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AppendMissingDataSetNameDiagnostic(
                state,
                diagnostics,
                node,
                "SCL-ENG-GOOSE-0003",
                "GOOSE");

            var endpoint = FindCommunicationEndpoint(
                state,
                node.Handle,
                SclSemanticKind.GseCommunication);

            if (endpoint.IsNone)
            {
                diagnostics.Add(CreateDiagnostic(
                    state,
                    "SCL-ENG-GOOSE-0001",
                    DiagnosticSeverity.Warning,
                    node.Handle,
                    $"GOOSE control '{node.DisplayName}' has no resolved Communication/GSE endpoint, so destination MAC, APPID and VLAN parameters cannot be projected.",
                    "ARSCL leaves the publisher inspectable but does not invent communication parameters. Verify the Communication section or the intended publisher binding."));
                continue;
            }

            var missing = GetMissingEndpointIdentity(
                state,
                endpoint);

            if (missing.Length != 0)
            {
                diagnostics.Add(CreateDiagnostic(
                    state,
                    "SCL-ENG-GOOSE-0002",
                    DiagnosticSeverity.Warning,
                    endpoint,
                    $"Communication/GSE endpoint for '{node.DisplayName}' is missing {string.Join(" and ", missing)}.",
                    "This check only reports missing engineering identity fields required by the ARSCL GOOSE workspace. It does not substitute for edition-specific schema validation."));
            }
        }
    }

    private static void AppendSampledValueDiagnostics(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (var node in state.SemanticIndex.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node.Kind !=
                SclSemanticKind.SampledValueControl)
            {
                continue;
            }

            AppendMissingDataSetNameDiagnostic(
                state,
                diagnostics,
                node,
                "SCL-ENG-SMV-0003",
                "SampledValueControl");

            var endpoint = FindCommunicationEndpoint(
                state,
                node.Handle,
                SclSemanticKind.SmvCommunication);

            if (endpoint.IsNone)
            {
                diagnostics.Add(CreateDiagnostic(
                    state,
                    "SCL-ENG-SMV-0001",
                    DiagnosticSeverity.Warning,
                    node.Handle,
                    $"SampledValueControl '{node.DisplayName}' has no resolved Communication/SMV endpoint, so multicast MAC, APPID and VLAN parameters cannot be projected.",
                    "ARSCL leaves the sampled-value control inspectable but does not invent process-bus communication parameters. Verify the Communication/SMV binding."));
                continue;
            }

            var missing = GetMissingEndpointIdentity(
                state,
                endpoint);

            if (missing.Length != 0)
            {
                diagnostics.Add(CreateDiagnostic(
                    state,
                    "SCL-ENG-SMV-0002",
                    DiagnosticSeverity.Warning,
                    endpoint,
                    $"Communication/SMV endpoint for '{node.DisplayName}' is missing {string.Join(" and ", missing)}.",
                    "This engineering check reports communication identity fields required for a usable SMV endpoint projection. It does not substitute for edition-specific schema validation."));
            }
        }
    }

    private static void AppendDataModelDiagnostics(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (var logicalNode in state.SemanticIndex.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (logicalNode.Kind is not (
                    SclSemanticKind.LogicalNodeZero or
                    SclSemanticKind.LogicalNode) ||
                !HasResolvedLogicalNodeType(
                    state,
                    logicalNode.Handle))
            {
                continue;
            }

            var rows = SclDataModelWorkspaceProjector.BuildRows(
                state,
                logicalNode.Handle);

            var resolvedInstanceHandles =
                new HashSet<SclNodeHandle>();

            for (var i = 0; i < rows.Length; i++)
            {
                resolvedInstanceHandles.Add(rows[i].Handle);
            }

            var children = state.SemanticIndex.GetChildren(
                logicalNode.Handle);

            for (var i = 0; i < children.Count; i++)
            {
                if (children[i].Kind != SclSemanticKind.Doi)
                {
                    continue;
                }

                AppendUnmatchedInstanceBranch(
                    state,
                    logicalNode,
                    children[i],
                    parentPath: string.Empty,
                    resolvedInstanceHandles,
                    diagnostics,
                    cancellationToken);
            }
        }
    }

    private static void AppendUnmatchedInstanceBranch(
        SclDocumentState state,
        SclSemanticNode logicalNode,
        SclSemanticNode instance,
        string parentPath,
        HashSet<SclNodeHandle> resolvedInstanceHandles,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var name = SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            instance.Handle,
            "name");

        var path = string.IsNullOrWhiteSpace(parentPath)
            ? name
            : string.Concat(
                parentPath,
                "/",
                name);

        if (!resolvedInstanceHandles.Contains(instance.Handle))
        {
            diagnostics.Add(CreateDiagnostic(
                state,
                "SCL-ENG-MODEL-0001",
                DiagnosticSeverity.Warning,
                instance.Handle,
                $"Instance path '{path}' under logical node '{logicalNode.DisplayName}' does not match the resolved DataTypeTemplates model.",
                "ARSCL did not attach this DOI/SDI/DAI branch to a different template by guess. Verify the instance names and the resolved LN/DO/DA type chain. Descendants of this unmatched branch are suppressed to avoid cascading findings."));
            return;
        }

        var children = state.SemanticIndex.GetChildren(
            instance.Handle);

        for (var i = 0; i < children.Count; i++)
        {
            if (children[i].Kind is not (
                    SclSemanticKind.Sdi or
                    SclSemanticKind.Dai))
            {
                continue;
            }

            AppendUnmatchedInstanceBranch(
                state,
                logicalNode,
                children[i],
                path,
                resolvedInstanceHandles,
                diagnostics,
                cancellationToken);
        }
    }

    private static bool HasResolvedLogicalNodeType(
        SclDocumentState state,
        SclNodeHandle logicalNode)
    {
        var outgoing = state.SemanticIndex.References.GetOutgoing(
            logicalNode);

        for (var i = 0; i < outgoing.Count; i++)
        {
            var edge = outgoing[i];

            if (edge.Kind != SclReferenceKind.TypeDefinition)
            {
                continue;
            }

            if (state.SemanticIndex.TryGetNode(
                    edge.Target,
                    out var target) &&
                target is not null &&
                target.Kind == SclSemanticKind.LogicalNodeType)
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendSettingGroupDiagnostics(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (var node in state.SemanticIndex.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node.Kind != SclSemanticKind.SettingGroupControl)
            {
                continue;
            }

            var numberText = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "numOfSGs");

            var activeText = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "actSG");

            var hasNumber = int.TryParse(
                numberText,
                out var numberOfGroups) &&
                numberOfGroups > 0;

            if (!hasNumber)
            {
                diagnostics.Add(CreateDiagnostic(
                    state,
                    "SCL-ENG-SG-0001",
                    DiagnosticSeverity.Warning,
                    node.Handle,
                    $"SettingControl '{node.DisplayName}' does not provide a positive numOfSGs value that ARSCL can interpret.",
                    "ARSCL cannot establish the setting-group range from this control. This engineering finding is separate from schema validation."));
            }

            if (hasNumber &&
                int.TryParse(
                    activeText,
                    out var activeGroup) &&
                (activeGroup < 1 ||
                 activeGroup > numberOfGroups))
            {
                diagnostics.Add(CreateDiagnostic(
                    state,
                    "SCL-ENG-SG-0002",
                    DiagnosticSeverity.Warning,
                    node.Handle,
                    $"SettingControl '{node.DisplayName}' declares active group {activeGroup}, outside the interpreted range 1..{numberOfGroups}.",
                    "Verify the configured active group and group count. ARSCL reports the inconsistency without modifying the source."));
            }

            var settings =
                SclSettingGroupWorkspaceProjector.BuildSettings(
                    state,
                    node.Handle);

            if (settings.Length == 0)
            {
                diagnostics.Add(CreateDiagnostic(
                    state,
                    "SCL-ENG-SG-0003",
                    DiagnosticSeverity.Info,
                    node.Handle,
                    $"SettingControl '{node.DisplayName}' has no configured FC=SG leaf values resolvable through the current Data Model.",
                    "This can be legitimate when setting values are not instantiated in the SCL. ARSCL reports it as information rather than assuming the configuration is invalid."));
            }
        }
    }

    private static SclNodeHandle FindCommunicationEndpoint(
        SclDocumentState state,
        SclNodeHandle controlHandle,
        SclSemanticKind endpointKind)
    {
        var incoming = state.SemanticIndex.References.GetIncoming(
            controlHandle);

        for (var i = 0; i < incoming.Count; i++)
        {
            var edge = incoming[i];

            if (edge.Kind !=
                SclReferenceKind.CommunicationControlBinding)
            {
                continue;
            }

            if (state.SemanticIndex.TryGetNode(
                    edge.Source,
                    out var source) &&
                source is not null &&
                source.Kind == endpointKind)
            {
                return edge.Source;
            }
        }

        return SclNodeHandle.None;
    }

    private static void AppendMissingDataSetNameDiagnostic(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        SclSemanticNode control,
        string code,
        string label)
    {
        var dataSetName = SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            control.Handle,
            "datSet");

        if (!string.IsNullOrWhiteSpace(dataSetName))
        {
            return;
        }

        diagnostics.Add(CreateDiagnostic(
            state,
            code,
            DiagnosticSeverity.Warning,
            control.Handle,
            $"{label} '{control.DisplayName}' does not name a DataSet, so its published signal set cannot be projected.",
            "This is an engineering completeness finding. If a DataSet name is present but cannot be resolved, the typed Reference diagnostics report that separately."));
    }

    private static string[] GetMissingEndpointIdentity(
        SclDocumentState state,
        SclNodeHandle endpoint)
    {
        if (!SclWorkspaceSyntaxReader.TryFindDirectElement(
                state.Syntax,
                endpoint,
                "Address",
                out var address))
        {
            return ["destination MAC", "APPID"];
        }

        var values = SclWorkspaceSyntaxReader.ReadPValues(
            state.Syntax,
            address);

        var missing = new List<string>(2);

        if (!values.TryGetValue(
                "MAC-Address",
                out var mac) ||
            string.IsNullOrWhiteSpace(mac))
        {
            missing.Add("destination MAC");
        }

        if (!values.TryGetValue(
                "APPID",
                out var appId) ||
            string.IsNullOrWhiteSpace(appId))
        {
            missing.Add("APPID");
        }

        return missing.Count == 0
            ? []
            : [.. missing];
    }

    private static Diagnostic CreateDiagnostic(
        SclDocumentState state,
        string code,
        DiagnosticSeverity severity,
        SclNodeHandle node,
        string message,
        string explanation) =>
        new(
            code,
            severity,
            DiagnosticDomain.Engineering,
            message,
            node,
            state.Syntax.GetSourceSpan(node),
            state.SourcePath,
            explanation,
            state.Revision);

    private readonly record struct NetworkAddressKey(
        string SubNetwork,
        string IpAddress);

    private sealed class NetworkAddressKeyComparer :
        IEqualityComparer<NetworkAddressKey>
    {
        public static NetworkAddressKeyComparer Instance { get; } =
            new();

        public bool Equals(
            NetworkAddressKey x,
            NetworkAddressKey y) =>
            string.Equals(
                x.SubNetwork,
                y.SubNetwork,
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                x.IpAddress,
                y.IpAddress,
                StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(NetworkAddressKey obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(
                    obj.SubNetwork),
                StringComparer.OrdinalIgnoreCase.GetHashCode(
                    obj.IpAddress));
    }
}
