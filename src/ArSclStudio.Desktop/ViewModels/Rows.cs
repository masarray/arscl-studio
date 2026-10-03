namespace ArSclStudio.Desktop.ViewModels;

public sealed record ExplorerRow(
    string Glyph,
    string Name,
    double Indent,
    string? Badge = null,
    bool HasWarning = false);

public sealed record MemberRow(
    string Reference,
    string Ld,
    string Ln,
    string DataObject,
    string DataAttribute,
    string Fc,
    string Cdc,
    string Description);

public sealed record ProblemRow(
    string Severity,
    string Code,
    string Domain,
    string ObjectName,
    string Message);
