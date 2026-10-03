using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;
using ArSclStudio.Scl.Source;

namespace ArSclStudio.Engine.Diagnostics;

public enum DiagnosticSeverity
{
    Info = 0,
    Warning,
    Error,
    Blocker
}

public enum DiagnosticDomain
{
    Xml = 0,
    Schema,
    Semantic,
    Reference,
    Engineering,
    Compatibility,
    Runtime
}

public sealed record Diagnostic(
    string Code,
    DiagnosticSeverity Severity,
    DiagnosticDomain Domain,
    string Message,
    SclNodeHandle Node,
    SclSourceSpan SourceSpan = default,
    string? SourcePath = null,
    string? Explanation = null,
    DocumentRevision? Revision = null);
