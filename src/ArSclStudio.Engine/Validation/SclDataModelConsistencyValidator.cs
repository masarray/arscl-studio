using ArSclStudio.Engine.Diagnostics;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Validation;

internal static class SclDataModelConsistencyValidator
{
    private const int MaxInstanceDepth = 64;

    public static void AppendDiagnostics(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var contexts =
            new Dictionary<SclNodeHandle, DefinitionContext>();

        foreach (var node in state.SemanticIndex.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node.Kind is not (
                    SclSemanticKind.LogicalNodeZero or
                    SclSemanticKind.LogicalNode))
            {
                continue;
            }

            ValidateLogicalNode(
                state,
                node,
                contexts,
                diagnostics,
                cancellationToken);
        }
    }

    private static void ValidateLogicalNode(
        SclDocumentState state,
        SclSemanticNode logicalNode,
        Dictionary<SclNodeHandle, DefinitionContext> contexts,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var logicalNodeType = FindOutgoingTypeTarget(
            state,
            logicalNode.Handle,
            SclSemanticKind.LogicalNodeType);

        if (logicalNodeType.IsNone)
        {
            // Missing/ambiguous lnType already belongs to the reference
            // diagnostic authority. Avoid reporting downstream guesses.
            return;
        }

        var logicalNodeContext = GetContext(
            state,
            logicalNodeType,
            contexts);

        var children = state.SemanticIndex.GetChildren(
            logicalNode.Handle);

        for (var i = 0; i < children.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var doi = children[i];

            if (doi.Kind != SclSemanticKind.Doi)
            {
                continue;
            }

            var name = ReadName(state, doi.Handle);

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var resolution = logicalNodeContext.Resolve(
                name,
                out var definition);

            if (resolution == MemberResolutionStatus.Missing)
            {
                diagnostics.Add(CreateDiagnostic(
                    state,
                    "SCL-SEM-MODEL-0001",
                    doi.Handle,
                    $"DOI '{name}' is not declared by resolved LNodeType '{logicalNode.DisplayName}'.",
                    "The instance is preserved, but it cannot be projected as a typed IEC 61850 data object from the resolved logical-node template."));
                continue;
            }

            if (resolution == MemberResolutionStatus.Ambiguous)
            {
                AppendAmbiguousMemberDiagnostic(
                    state,
                    diagnostics,
                    doi.Handle,
                    name,
                    logicalNodeContext.DisplayName);
                continue;
            }

            var doType = FindOutgoingTypeTarget(
                state,
                definition.Handle,
                SclSemanticKind.DataObjectType);

            if (doType.IsNone)
            {
                // A missing or ambiguous DO type edge is reported by the
                // reference graph when the type identity is present.
                continue;
            }

            ValidateInstanceChildren(
                state,
                doi.Handle,
                doType,
                depth: 0,
                contexts,
                diagnostics,
                cancellationToken);
        }
    }

    private static void ValidateInstanceChildren(
        SclDocumentState state,
        SclNodeHandle instanceParent,
        SclNodeHandle typeContext,
        int depth,
        Dictionary<SclNodeHandle, DefinitionContext> contexts,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        if (depth >= MaxInstanceDepth)
        {
            return;
        }

        var context = GetContext(
            state,
            typeContext,
            contexts);

        var children = state.SemanticIndex.GetChildren(
            instanceParent);

        for (var i = 0; i < children.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var instance = children[i];

            if (instance.Kind is not (
                    SclSemanticKind.Sdi or
                    SclSemanticKind.Dai))
            {
                continue;
            }

            var name = ReadName(
                state,
                instance.Handle);

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var resolution = context.Resolve(
                name,
                out var definition);

            if (resolution == MemberResolutionStatus.Missing)
            {
                diagnostics.Add(CreateDiagnostic(
                    state,
                    instance.Kind == SclSemanticKind.Sdi
                        ? "SCL-SEM-MODEL-0002"
                        : "SCL-SEM-MODEL-0003",
                    instance.Handle,
                    instance.Kind == SclSemanticKind.Sdi
                        ? $"SDI '{name}' is not declared by resolved type '{context.DisplayName}'."
                        : $"DAI '{name}' is not declared by resolved type '{context.DisplayName}'.",
                    "The instance branch is preserved. ARSCL does not invent a matching template member."));
                continue;
            }

            if (resolution == MemberResolutionStatus.Ambiguous)
            {
                AppendAmbiguousMemberDiagnostic(
                    state,
                    diagnostics,
                    instance.Handle,
                    name,
                    context.DisplayName);
                continue;
            }

            if (instance.Kind == SclSemanticKind.Sdi)
            {
                ValidateStructuredInstance(
                    state,
                    instance,
                    definition,
                    depth,
                    contexts,
                    diagnostics,
                    cancellationToken);
                continue;
            }

            ValidateLeafInstance(
                state,
                instance,
                definition,
                diagnostics);
        }
    }

    private static void ValidateStructuredInstance(
        SclDocumentState state,
        SclSemanticNode instance,
        DefinitionEntry definition,
        int depth,
        Dictionary<SclNodeHandle, DefinitionContext> contexts,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        SclSemanticKind expectedType;

        if (definition.Kind ==
            SclSemanticKind.SubDataObjectDefinition)
        {
            expectedType = SclSemanticKind.DataObjectType;
        }
        else if (definition.Kind is
                 SclSemanticKind.DataAttributeDefinition or
                 SclSemanticKind.BasicDataAttributeDefinition)
        {
            if (!string.Equals(
                    definition.BasicType,
                    "Struct",
                    StringComparison.Ordinal))
            {
                diagnostics.Add(CreateDiagnostic(
                    state,
                    "SCL-SEM-MODEL-0005",
                    instance.Handle,
                    $"SDI '{ReadName(state, instance.Handle)}' targets non-structured template member '{definition.DisplayName}'.",
                    "SDI represents a structured instance branch. A leaf DA/BDA must be represented as DAI."));
                return;
            }

            expectedType = SclSemanticKind.DataAttributeType;
        }
        else
        {
            return;
        }

        var target = FindOutgoingTypeTarget(
            state,
            definition.Handle,
            expectedType);

        if (target.IsNone)
        {
            return;
        }

        ValidateInstanceChildren(
            state,
            instance.Handle,
            target,
            depth + 1,
            contexts,
            diagnostics,
            cancellationToken);
    }

    private static void ValidateLeafInstance(
        SclDocumentState state,
        SclSemanticNode instance,
        DefinitionEntry definition,
        List<Diagnostic> diagnostics)
    {
        if (definition.Kind ==
                SclSemanticKind.SubDataObjectDefinition ||
            string.Equals(
                definition.BasicType,
                "Struct",
                StringComparison.Ordinal))
        {
            diagnostics.Add(CreateDiagnostic(
                state,
                "SCL-SEM-MODEL-0006",
                instance.Handle,
                $"DAI '{ReadName(state, instance.Handle)}' targets structured template member '{definition.DisplayName}'.",
                "A structured SDO or Struct DA/BDA must be represented as SDI so its nested members can be resolved."));
        }
    }

    private static DefinitionContext GetContext(
        SclDocumentState state,
        SclNodeHandle typeHandle,
        Dictionary<SclNodeHandle, DefinitionContext> contexts)
    {
        if (contexts.TryGetValue(
                typeHandle,
                out var existing))
        {
            return existing;
        }

        var displayName =
            state.SemanticIndex.TryGetNode(
                typeHandle,
                out var typeNode) &&
            typeNode is not null
                ? typeNode.DisplayName
                : typeHandle.ToString();

        var members =
            new Dictionary<string, DefinitionEntry>(
                StringComparer.Ordinal);

        var ambiguous =
            new HashSet<string>(
                StringComparer.Ordinal);

        var children =
            state.SemanticIndex.GetChildren(
                typeHandle);

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];

            if (!IsDefinitionMember(child.Kind))
            {
                continue;
            }

            var name = ReadName(
                state,
                child.Handle);

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var entry = new DefinitionEntry(
                child.Handle,
                child.Kind,
                child.DisplayName,
                ReadBasicType(
                    state,
                    child.Handle));

            if (!members.TryAdd(
                    name,
                    entry))
            {
                ambiguous.Add(name);
            }
        }

        var context = new DefinitionContext(
            displayName,
            members,
            ambiguous);

        contexts.Add(
            typeHandle,
            context);

        return context;
    }

    private static bool IsDefinitionMember(
        SclSemanticKind kind) =>
        kind is
            SclSemanticKind.DataObjectDefinition or
            SclSemanticKind.SubDataObjectDefinition or
            SclSemanticKind.DataAttributeDefinition or
            SclSemanticKind.BasicDataAttributeDefinition;

    private static string ReadName(
        SclDocumentState state,
        SclNodeHandle handle) =>
        SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            handle,
            "name");

    private static string ReadBasicType(
        SclDocumentState state,
        SclNodeHandle handle) =>
        SclWorkspaceSyntaxReader.Attribute(
            state.Syntax,
            handle,
            "bType");

    private static SclNodeHandle FindOutgoingTypeTarget(
        SclDocumentState state,
        SclNodeHandle source,
        SclSemanticKind expectedKind)
    {
        var outgoing =
            state.SemanticIndex.References.GetOutgoing(
                source);

        for (var i = 0; i < outgoing.Count; i++)
        {
            var edge = outgoing[i];

            if (edge.Kind !=
                SclReferenceKind.TypeDefinition)
            {
                continue;
            }

            if (state.SemanticIndex.TryGetNode(
                    edge.Target,
                    out var target) &&
                target is not null &&
                target.Kind == expectedKind)
            {
                return edge.Target;
            }
        }

        return SclNodeHandle.None;
    }

    private static void AppendAmbiguousMemberDiagnostic(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        SclNodeHandle instance,
        string memberName,
        string contextName)
    {
        diagnostics.Add(CreateDiagnostic(
            state,
            "SCL-SEM-MODEL-0004",
            instance,
            $"Instance member '{memberName}' is ambiguous in resolved type '{contextName}'.",
            "Multiple template members use the same name in this type context. ARSCL did not guess which definition applies."));
    }

    private static Diagnostic CreateDiagnostic(
        SclDocumentState state,
        string code,
        SclNodeHandle node,
        string message,
        string explanation) =>
        new(
            code,
            DiagnosticSeverity.Warning,
            DiagnosticDomain.Semantic,
            message,
            node,
            state.Syntax.GetSourceSpan(node),
            state.SourcePath,
            explanation,
            state.Revision);

    private enum MemberResolutionStatus
    {
        Unique = 0,
        Missing,
        Ambiguous
    }

    private sealed class DefinitionContext(
        string displayName,
        Dictionary<string, DefinitionEntry> members,
        HashSet<string> ambiguous)
    {
        public string DisplayName { get; } = displayName;

        public MemberResolutionStatus Resolve(
            string name,
            out DefinitionEntry member)
        {
            if (ambiguous.Contains(name))
            {
                member = default;
                return MemberResolutionStatus.Ambiguous;
            }

            if (members.TryGetValue(
                    name,
                    out member))
            {
                return MemberResolutionStatus.Unique;
            }

            member = default;
            return MemberResolutionStatus.Missing;
        }
    }

    private readonly record struct DefinitionEntry(
        SclNodeHandle Handle,
        SclSemanticKind Kind,
        string DisplayName,
        string BasicType);
}
