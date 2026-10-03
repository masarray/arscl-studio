using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Navigation;

public sealed record SclGooseWorkspaceProjection(
    SclNodeHandle Handle,
    string IedName,
    string AccessPoint,
    string LogicalDevice,
    string LogicalNode,
    string Name,
    string ServiceType,
    string DataSet,
    string ControlAppId,
    string ConfigurationRevision,
    string MacAddress,
    string NetworkAppId,
    string VlanId,
    string VlanPriority,
    string MinTime,
    string MaxTime,
    int MemberCount,
    int SubscriberCount,
    int SubscriberIedCount,
    string EndpointStatus);

public sealed record SclGooseSignalProjection(
    SclNodeHandle Handle,
    string Reference,
    string FunctionalConstraint,
    int SubscriberCount);

public sealed record SclGooseSubscriberProjection(
    SclNodeHandle Handle,
    string SubscriberIed,
    string SubscriberLogicalDevice,
    string SubscriberLogicalNode,
    string InternalAddress,
    string SourceReference);

public static class SclGooseWorkspaceProjector
{
    public static SclGooseWorkspaceProjection[] BuildCatalog(
        SclDocumentState state,
        SclNodeHandle iedHandle)
    {
        ArgumentNullException.ThrowIfNull(state);

        var subscriberIndex = BuildSubscriberIndex(state);
        var rows = new List<SclGooseWorkspaceProjection>();

        foreach (var node in state.SemanticIndex.Nodes)
        {
            if (node.Kind != SclSemanticKind.GseControl ||
                !BelongsToIed(state, node.Handle, iedHandle))
            {
                continue;
            }

            var iedName = AncestorName(
                state,
                node.Handle,
                SclSemanticKind.Ied);

            var logicalDevice = AncestorName(
                state,
                node.Handle,
                SclSemanticKind.LogicalDevice);

            var logicalNode = AncestorName(
                state,
                node.Handle,
                SclSemanticKind.LogicalNodeZero,
                SclSemanticKind.LogicalNode);

            var serviceType = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "type");

            var dataSet = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "datSet");

            var controlAppId = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "appID");

            var confRev = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "confRev");

            var dataSetHandle = FindBoundDataSet(
                state,
                node.Handle);

            var memberHandles = GetDataSetMembers(
                state,
                dataSetHandle);

            var subscribers = MatchSubscribers(
                state,
                iedName,
                memberHandles,
                subscriberIndex);

            var endpoint = FindCommunicationEndpoint(
                state,
                node.Handle);

            var accessPoint = string.Empty;
            var macAddress = string.Empty;
            var networkAppId = string.Empty;
            var vlanId = string.Empty;
            var vlanPriority = string.Empty;
            var minTime = string.Empty;
            var maxTime = string.Empty;
            var endpointStatus = string.Equals(
                    serviceType,
                    "GSSE",
                    StringComparison.OrdinalIgnoreCase)
                ? "Not applicable"
                : "No Communication/GSE";

            if (!endpoint.IsNone)
            {
                endpointStatus = "Bound";

                if (state.SemanticIndex.TryFindAncestor(
                        endpoint,
                        SclSemanticKind.ConnectedAccessPoint,
                        out var connectedAccessPoint) &&
                    connectedAccessPoint is not null)
                {
                    accessPoint = SclWorkspaceSyntaxReader.Attribute(
                        state.Syntax,
                        connectedAccessPoint.Handle,
                        "apName");
                }

                if (SclWorkspaceSyntaxReader.TryFindDirectElement(
                        state.Syntax,
                        endpoint,
                        "Address",
                        out var address))
                {
                    var pValues = SclWorkspaceSyntaxReader.ReadPValues(
                        state.Syntax,
                        address);

                    macAddress = Get(pValues, "MAC-Address");
                    networkAppId = Get(pValues, "APPID");
                    vlanId = Get(pValues, "VLAN-ID");
                    vlanPriority = Get(pValues, "VLAN-PRIORITY");
                }

                minTime = ReadTime(
                    state,
                    endpoint,
                    "MinTime");

                maxTime = ReadTime(
                    state,
                    endpoint,
                    "MaxTime");
            }

            rows.Add(new SclGooseWorkspaceProjection(
                node.Handle,
                iedName,
                accessPoint,
                logicalDevice,
                logicalNode,
                node.DisplayName,
                serviceType,
                dataSet,
                controlAppId,
                confRev,
                macAddress,
                networkAppId,
                vlanId,
                vlanPriority,
                minTime,
                maxTime,
                memberHandles.Count,
                subscribers.Count,
                subscribers
                    .Select(static subscriber => subscriber.SubscriberIed)
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                endpointStatus));
        }

        return
        [
            .. rows.OrderBy(static row => row.LogicalDevice, StringComparer.OrdinalIgnoreCase)
                   .ThenBy(static row => row.ServiceType, StringComparer.OrdinalIgnoreCase)
                   .ThenBy(static row => row.Name, StringComparer.OrdinalIgnoreCase)
        ];
    }

    public static SclGooseSignalProjection[] BuildSignals(
        SclDocumentState state,
        SclNodeHandle controlHandle)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.SemanticIndex.TryGetNode(
                controlHandle,
                out var control) ||
            control is null ||
            control.Kind != SclSemanticKind.GseControl)
        {
            return [];
        }

        var iedName = AncestorName(
            state,
            controlHandle,
            SclSemanticKind.Ied);

        var dataSetHandle = FindBoundDataSet(
            state,
            controlHandle);

        var members = GetDataSetMembers(
            state,
            dataSetHandle);

        if (members.Count == 0)
        {
            return [];
        }

        var subscriberIndex = BuildSubscriberIndex(state);
        var rows = new SclGooseSignalProjection[members.Count];

        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            var key = CreatePublisherSignalKey(
                state,
                iedName,
                member);

            var subscriberCount =
                subscriberIndex.TryGetValue(key, out var subscribers)
                    ? subscribers.Count
                    : 0;

            rows[i] = new SclGooseSignalProjection(
                member,
                CreateFcdaReference(state, member),
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    member,
                    "fc"),
                subscriberCount);
        }

        return rows;
    }

    public static SclGooseSubscriberProjection[] BuildSubscribers(
        SclDocumentState state,
        SclNodeHandle controlHandle)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.SemanticIndex.TryGetNode(
                controlHandle,
                out var control) ||
            control is null ||
            control.Kind != SclSemanticKind.GseControl)
        {
            return [];
        }

        var iedName = AncestorName(
            state,
            controlHandle,
            SclSemanticKind.Ied);

        var members = GetDataSetMembers(
            state,
            FindBoundDataSet(
                state,
                controlHandle));

        return
        [
            .. MatchSubscribers(
                    state,
                    iedName,
                    members,
                    BuildSubscriberIndex(state))
                .OrderBy(
                    static row => row.SubscriberIed,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    static row => row.SourceReference,
                    StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static SclNodeHandle FindBoundDataSet(
        SclDocumentState state,
        SclNodeHandle controlHandle)
    {
        var outgoing = state.SemanticIndex.References.GetOutgoing(
            controlHandle);

        for (var i = 0; i < outgoing.Count; i++)
        {
            if (outgoing[i].Kind == SclReferenceKind.DataSetBinding)
            {
                return outgoing[i].Target;
            }
        }

        return SclNodeHandle.None;
    }

    private static SclNodeHandle FindCommunicationEndpoint(
        SclDocumentState state,
        SclNodeHandle controlHandle)
    {
        var incoming = state.SemanticIndex.References.GetIncoming(
            controlHandle);

        for (var i = 0; i < incoming.Count; i++)
        {
            var edge = incoming[i];

            if (edge.Kind != SclReferenceKind.CommunicationControlBinding)
            {
                continue;
            }

            if (state.SemanticIndex.TryGetNode(
                    edge.Source,
                    out var source) &&
                source is not null &&
                source.Kind == SclSemanticKind.GseCommunication)
            {
                return edge.Source;
            }
        }

        return SclNodeHandle.None;
    }

    private static List<SclNodeHandle> GetDataSetMembers(
        SclDocumentState state,
        SclNodeHandle dataSetHandle)
    {
        if (dataSetHandle.IsNone)
        {
            return [];
        }

        var children = state.SemanticIndex.GetChildren(
            dataSetHandle);

        var result = new List<SclNodeHandle>(children.Count);

        for (var i = 0; i < children.Count; i++)
        {
            if (children[i].Kind == SclSemanticKind.Fcda)
            {
                result.Add(children[i].Handle);
            }
        }

        return result;
    }

    private static Dictionary<SignalKey, List<SclGooseSubscriberProjection>>
        BuildSubscriberIndex(SclDocumentState state)
    {
        var result =
            new Dictionary<SignalKey, List<SclGooseSubscriberProjection>>();

        foreach (var node in state.SemanticIndex.Nodes)
        {
            if (node.Kind != SclSemanticKind.ExternalReference)
            {
                continue;
            }

            var sourceIed = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "iedName");

            var sourceLd = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "ldInst");

            var lnClass = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "lnClass");

            var doName = SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                node.Handle,
                "doName");

            if (string.IsNullOrWhiteSpace(sourceIed) ||
                string.IsNullOrWhiteSpace(sourceLd) ||
                string.IsNullOrWhiteSpace(lnClass) ||
                string.IsNullOrWhiteSpace(doName))
            {
                continue;
            }

            var key = new SignalKey(
                sourceIed,
                sourceLd,
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "prefix"),
                lnClass,
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "lnInst"),
                doName,
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "daName"));

            var projection = new SclGooseSubscriberProjection(
                node.Handle,
                AncestorName(
                    state,
                    node.Handle,
                    SclSemanticKind.Ied),
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
                    "intAddr"),
                CreateSourceReference(key));

            if (!result.TryGetValue(key, out var subscribers))
            {
                subscribers = [];
                result.Add(key, subscribers);
            }

            subscribers.Add(projection);
        }

        return result;
    }

    private static List<SclGooseSubscriberProjection> MatchSubscribers(
        SclDocumentState state,
        string publisherIed,
        List<SclNodeHandle> members,
        Dictionary<SignalKey, List<SclGooseSubscriberProjection>> subscriberIndex)
    {
        var result = new List<SclGooseSubscriberProjection>();

        for (var i = 0; i < members.Count; i++)
        {
            var key = CreatePublisherSignalKey(
                state,
                publisherIed,
                members[i]);

            if (subscriberIndex.TryGetValue(
                    key,
                    out var subscribers))
            {
                result.AddRange(subscribers);
            }
        }

        if (result.Count <= 1)
        {
            return result;
        }

        var seen = new HashSet<SclNodeHandle>();
        var unique = new List<SclGooseSubscriberProjection>(
            result.Count);

        for (var i = 0; i < result.Count; i++)
        {
            if (seen.Add(result[i].Handle))
            {
                unique.Add(result[i]);
            }
        }

        return unique;
    }

    private static SignalKey CreatePublisherSignalKey(
        SclDocumentState state,
        string iedName,
        SclNodeHandle fcda) =>
        new(
            iedName,
            SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                fcda,
                "ldInst"),
            SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                fcda,
                "prefix"),
            SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                fcda,
                "lnClass"),
            SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                fcda,
                "lnInst"),
            SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                fcda,
                "doName"),
            SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                fcda,
                "daName"));

    private static string CreateFcdaReference(
        SclDocumentState state,
        SclNodeHandle fcda)
    {
        var key = CreatePublisherSignalKey(
            state,
            string.Empty,
            fcda);

        var logicalNode = string.Concat(
            key.Prefix,
            key.LnClass,
            key.LnInst);

        var data = string.IsNullOrWhiteSpace(key.DaName)
            ? key.DoName
            : string.Concat(
                key.DoName,
                ".",
                key.DaName);

        return string.Concat(
            key.LdInst,
            "/",
            logicalNode,
            "/",
            data);
    }

    private static string CreateSourceReference(
        SignalKey key)
    {
        var logicalNode = string.Concat(
            key.Prefix,
            key.LnClass,
            key.LnInst);

        var data = string.IsNullOrWhiteSpace(key.DaName)
            ? key.DoName
            : string.Concat(
                key.DoName,
                ".",
                key.DaName);

        return string.Concat(
            key.IedName,
            "/",
            key.LdInst,
            "/",
            logicalNode,
            "/",
            data);
    }

    private static string ReadTime(
        SclDocumentState state,
        SclNodeHandle endpoint,
        string elementName)
    {
        if (!SclWorkspaceSyntaxReader.TryFindDirectElement(
                state.Syntax,
                endpoint,
                elementName,
                out var timeElement))
        {
            return string.Empty;
        }

        var value = SclWorkspaceSyntaxReader.ReadElementText(
            state.Syntax,
            timeElement);

        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var multiplier = SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            timeElement,
            "multiplier");

        var unit = SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            timeElement,
            "unit");

        return string.IsNullOrWhiteSpace(unit)
            ? value
            : string.Concat(
                value,
                " ",
                multiplier,
                unit);
    }

    private static string Get(
        Dictionary<string, string> values,
        string key) =>
        values.TryGetValue(key, out var value)
            ? value
            : string.Empty;

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

    private readonly record struct SignalKey(
        string IedName,
        string LdInst,
        string Prefix,
        string LnClass,
        string LnInst,
        string DoName,
        string DaName);
}
