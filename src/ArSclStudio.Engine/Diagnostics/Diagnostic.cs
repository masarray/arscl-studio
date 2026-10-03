using ArSclStudio.Scl.Identity;

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
    string? Explanation = null);
