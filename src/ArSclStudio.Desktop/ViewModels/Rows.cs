using Avalonia;
using ArSclStudio.Engine.Diagnostics;
using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Desktop.ViewModels;

public sealed record ExplorerRow(
    SclNodeHandle Handle,
    ExplorerNodeKind Kind,
    int Depth,
    string Name,
    string? Badge,
    bool IsSelectable,
    bool HasChildren,
    bool IsExpanded)
{
    public Thickness IndentMargin => new(Depth * 16, 0, 0, 0);

    public string Glyph =>
        HasChildren
            ? IsExpanded ? "▾" : "▸"
            : "•";
}

public sealed record ProblemRow(
    string Severity,
    string Code,
    string Domain,
    string ObjectName,
    string Message,
    string Source)
{
    public static ProblemRow FromDiagnostic(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        var sourceName = string.IsNullOrWhiteSpace(diagnostic.SourcePath)
            ? string.Empty
            : Path.GetFileName(diagnostic.SourcePath);

        var source = diagnostic.SourceSpan.IsKnown
            ? string.IsNullOrWhiteSpace(sourceName)
                ? diagnostic.SourceSpan.ToString()
                : $"{sourceName}:{diagnostic.SourceSpan}"
            : sourceName;

        return new ProblemRow(
            diagnostic.Severity.ToString(),
            diagnostic.Code,
            diagnostic.Domain.ToString(),
            diagnostic.Node.IsNone
                ? "Document"
                : diagnostic.Node.ToString(),
            diagnostic.Message,
            source);
    }
}
