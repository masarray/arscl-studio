using ArSclStudio.Engine.Diagnostics;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;

namespace ArSclStudio.Engine.Validation;

internal static class SclServicesConsistencyValidator
{
    public static void AppendDiagnostics(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var counts =
            BuildPublisherCounts(
                state,
                cancellationToken);

        foreach (var node in state.SemanticIndex.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node.Kind != SclSemanticKind.Ied)
            {
                continue;
            }

            var rows =
                SclServicesWorkspaceProjector.Build(
                    state,
                    node.Handle);

            if (rows.Length == 0)
            {
                continue;
            }

            counts.TryGetValue(
                node.Handle,
                out var publisherCounts);

            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];

                switch (row.Name)
                {
                    case "GOOSE":
                        AppendLimitDiagnostic(
                            state,
                            diagnostics,
                            row,
                            publisherCounts.Goose,
                            "SCL-ENG-SERVICE-0001",
                            "explicit GOOSE");
                        break;

                    case "GSSE":
                        AppendLimitDiagnostic(
                            state,
                            diagnostics,
                            row,
                            publisherCounts.Gsse,
                            "SCL-ENG-SERVICE-0002",
                            "explicit GSSE");
                        break;

                    case "SMV":
                        AppendLimitDiagnostic(
                            state,
                            diagnostics,
                            row,
                            publisherCounts.Smv,
                            "SCL-ENG-SERVICE-0003",
                            "SampledValueControl");
                        break;
                }
            }
        }
    }

    private static Dictionary<SclNodeHandle, PublisherCounts>
        BuildPublisherCounts(
            SclDocumentState state,
            CancellationToken cancellationToken)
    {
        var result =
            new Dictionary<SclNodeHandle, PublisherCounts>();

        foreach (var node in state.SemanticIndex.Nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node.Kind is not (
                    SclSemanticKind.GseControl or
                    SclSemanticKind.SampledValueControl))
            {
                continue;
            }

            if (!state.SemanticIndex.TryFindAncestor(
                    node.Handle,
                    SclSemanticKind.Ied,
                    out var ied) ||
                ied is null)
            {
                continue;
            }

            result.TryGetValue(
                ied.Handle,
                out var counts);

            if (node.Kind ==
                SclSemanticKind.SampledValueControl)
            {
                result[ied.Handle] =
                    counts with
                    {
                        Smv = counts.Smv + 1
                    };

                continue;
            }

            var type =
                SclWorkspaceSyntaxReader.Attribute(
                    state.Syntax,
                    node.Handle,
                    "type");

            if (string.Equals(
                    type,
                    "GOOSE",
                    StringComparison.OrdinalIgnoreCase))
            {
                result[ied.Handle] =
                    counts with
                    {
                        Goose = counts.Goose + 1
                    };
            }
            else if (string.Equals(
                         type,
                         "GSSE",
                         StringComparison.OrdinalIgnoreCase))
            {
                result[ied.Handle] =
                    counts with
                    {
                        Gsse = counts.Gsse + 1
                    };
            }
        }

        return result;
    }

    private static void AppendLimitDiagnostic(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        SclServiceCapabilityProjection capability,
        int configuredCount,
        string code,
        string configuredLabel)
    {
        var rawMax =
            SclWorkspaceSyntaxReader.Attribute(
                state.Syntax,
                capability.Handle,
                "max");

        if (!int.TryParse(
                rawMax,
                out var maximum) ||
            maximum < 0 ||
            configuredCount <= maximum)
        {
            return;
        }

        diagnostics.Add(
            new Diagnostic(
                code,
                DiagnosticSeverity.Warning,
                DiagnosticDomain.Engineering,
                $"{configuredLabel} publisher count {configuredCount} exceeds the SCL-declared {capability.Name} maximum {maximum}.",
                capability.Handle,
                state.Syntax.GetSourceSpan(
                    capability.Handle),
                state.SourcePath,
                "The comparison uses only explicit publisher identities and the literal Services/@max declaration. ARSCL does not infer runtime device capability beyond the SCL declaration.",
                state.Revision));
    }

    private readonly record struct PublisherCounts(
        int Goose,
        int Gsse,
        int Smv);
}
