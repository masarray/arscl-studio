using ArSclStudio.Engine.Diagnostics;
using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Semantics;
using ArSclStudio.Scl.Source;

namespace ArSclStudio.Engine.Validation;

public enum SclSchemaProviderAvailability
{
    Available = 0,
    Unavailable,
    UnsupportedRevision
}

public sealed record SclSchemaProviderStatus(
    string ProviderId,
    SclSchemaProviderAvailability Availability,
    string Message,
    string? Provenance = null);

public sealed record SclSchemaFinding(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    SclNodeHandle Node = default,
    SclSourceSpan SourceSpan = default,
    string? Explanation = null);

public interface ISclSchemaProvider
{
    ValueTask<SclSchemaProviderStatus> GetStatusAsync(
        SclDocumentState state,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<SclSchemaFinding>> ValidateAsync(
        SclDocumentState state,
        CancellationToken cancellationToken = default);
}

public sealed class UnavailableSclSchemaProvider : ISclSchemaProvider
{
    public static UnavailableSclSchemaProvider Instance { get; } = new();

    private UnavailableSclSchemaProvider()
    {
    }

    public ValueTask<SclSchemaProviderStatus> GetStatusAsync(
        SclDocumentState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new SclSchemaProviderStatus(
            "none",
            SclSchemaProviderAvailability.Unavailable,
            "IEC 61850 schema validation is unavailable because no legally sourced schema pack is configured.",
            "No IEC schema text is embedded in ARSCL Studio."));
    }

    public ValueTask<IReadOnlyList<SclSchemaFinding>> ValidateAsync(
        SclDocumentState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<SclSchemaFinding>>([]);
    }
}

public sealed record SclValidationSnapshot(
    DocumentRevision Revision,
    IReadOnlyList<Diagnostic> Diagnostics,
    SclSchemaProviderStatus SchemaStatus);

internal static class SclValidationEngine
{
    public static async ValueTask<SclValidationSnapshot> ValidateFastAsync(
        SclDocumentState state,
        ISclSchemaProvider schemaProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(schemaProvider);

        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new List<Diagnostic>(
            state.SemanticIndex.References.IssueCount + 1);

        AppendReferenceDiagnostics(state, diagnostics, cancellationToken);

        var schemaStatus = await schemaProvider
            .GetStatusAsync(state, cancellationToken)
            .ConfigureAwait(false);

        AppendSchemaStatusDiagnostic(
            state,
            schemaStatus,
            diagnostics,
            fullValidationRequested: false);

        return new SclValidationSnapshot(
            state.Revision,
            diagnostics.Count == 0 ? [] : [.. diagnostics],
            schemaStatus);
    }

    public static async ValueTask<SclValidationSnapshot> ValidateFullAsync(
        SclDocumentState state,
        ISclSchemaProvider schemaProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(schemaProvider);

        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new List<Diagnostic>(
            state.SemanticIndex.References.IssueCount + 4);

        AppendReferenceDiagnostics(state, diagnostics, cancellationToken);

        var schemaStatus = await schemaProvider
            .GetStatusAsync(state, cancellationToken)
            .ConfigureAwait(false);

        if (schemaStatus.Availability == SclSchemaProviderAvailability.Available)
        {
            var findings = await schemaProvider
                .ValidateAsync(state, cancellationToken)
                .ConfigureAwait(false);

            for (var i = 0; i < findings.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var finding = findings[i];
                diagnostics.Add(new Diagnostic(
                    finding.Code,
                    finding.Severity,
                    DiagnosticDomain.Schema,
                    finding.Message,
                    finding.Node,
                    finding.SourceSpan,
                    state.SourcePath,
                    finding.Explanation,
                    state.Revision));
            }
        }
        else
        {
            AppendSchemaStatusDiagnostic(
                state,
                schemaStatus,
                diagnostics,
                fullValidationRequested: true);
        }

        return new SclValidationSnapshot(
            state.Revision,
            diagnostics.Count == 0 ? [] : [.. diagnostics],
            schemaStatus);
    }

    private static void AppendReferenceDiagnostics(
        SclDocumentState state,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var issues = state.SemanticIndex.References.Issues;

        for (var i = 0; i < issues.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var issue = issues[i];
            var ambiguous = issue.Status == SclReferenceResolutionStatus.Ambiguous;
            var expected = issue.ExpectedTargetKind?.ToString() ?? "target";

            diagnostics.Add(new Diagnostic(
                ambiguous ? "SCL-REF-0002" : "SCL-REF-0001",
                DiagnosticSeverity.Error,
                DiagnosticDomain.Reference,
                ambiguous
                    ? $"{issue.Kind} reference '{issue.ReferenceText}' is ambiguous across multiple {expected} objects. ARSCL did not guess a target."
                    : $"{issue.Kind} reference '{issue.ReferenceText}' does not resolve to a {expected} object.",
                issue.Source,
                state.Syntax.GetSourceSpan(issue.Source),
                state.SourcePath,
                "The document remains viewable. Resolve the referenced identity explicitly before using destructive edits or export gates that require reference integrity.",
                state.Revision));
        }
    }

    private static void AppendSchemaStatusDiagnostic(
        SclDocumentState state,
        SclSchemaProviderStatus status,
        List<Diagnostic> diagnostics,
        bool fullValidationRequested)
    {
        var code = status.Availability switch
        {
            SclSchemaProviderAvailability.Available => "SCL-SCHEMA-0002",
            SclSchemaProviderAvailability.UnsupportedRevision => "SCL-SCHEMA-0003",
            _ => "SCL-SCHEMA-0001"
        };

        var severity = status.Availability == SclSchemaProviderAvailability.UnsupportedRevision
            ? DiagnosticSeverity.Warning
            : DiagnosticSeverity.Info;

        var message = status.Availability switch
        {
            SclSchemaProviderAvailability.Available when !fullValidationRequested =>
                $"Schema provider '{status.ProviderId}' is available; full XSD validation is intentionally deferred during fast validation.",
            SclSchemaProviderAvailability.Available =>
                status.Message,
            SclSchemaProviderAvailability.UnsupportedRevision =>
                status.Message,
            _ => status.Message
        };

        diagnostics.Add(new Diagnostic(
            code,
            severity,
            DiagnosticDomain.Schema,
            message,
            state.Syntax.RootHandle,
            state.Syntax.GetSourceSpan(state.Syntax.RootHandle),
            state.SourcePath,
            status.Provenance,
            state.Revision));
    }
}
