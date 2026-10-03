using CommunityToolkit.Mvvm.ComponentModel;

namespace ArSclStudio.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _targetProfile = "SICAM SCC";

    [ObservableProperty]
    private string _selectedObject = "Digital";

    public string WindowTitle => "ARSCL Studio — IEC 61850 SCL Editor & Configurator";

    public IReadOnlyList<ExplorerRow> ExplorerRows { get; } =
    [
        new("▾", "Station_A.scd", 0, "SCD"),
        new("▸", "Header", 18),
        new("▸", "Substation", 18),
        new("▾", "IEDs (3)", 18),
        new("▾", "AA1E1F06R4", 36),
        new("▸", "Communication", 54),
        new("▾", "DataSets (2)", 54),
        new("•", "Analog (22)", 72),
        new("•", "Digital (36)", 72),
        new("▾", "Reports (3)", 54),
        new("•", "BR.Buffer01", 72),
        new("⚠", "BR.Buffer02", 72, HasWarning: true),
        new("•", "RP.Unbuffer01", 72),
        new("▸", "GOOSE (1)", 54),
        new("▸", "Setting Groups (2)", 54),
        new("▾", "Data Model", 54),
        new("•", "LLN0", 72),
        new("•", "LPHD1", 72),
        new("•", "XCBR1", 72),
        new("•", "MMXU1", 72),
        new("▸", "Communication", 18),
        new("▸", "DataTypeTemplates", 18),
        new("▸", "Private & Extensions", 18)
    ];

    public IReadOnlyList<MemberRow> Members { get; } =
    [
        new("XCBR1.Pos.stVal", "LLN0", "XCBR1", "Pos", "stVal", "ST", "DPC", "Switch position — status value"),
        new("XCBR1.Pos.q", "LLN0", "XCBR1", "Pos", "q", "ST", "DPC", "Quality"),
        new("XCBR1.Pos.t", "LLN0", "XCBR1", "Pos", "t", "ST", "DPC", "Timestamp"),
        new("GGIO1.Ind1.stVal", "LLN0", "GGIO1", "Ind1", "stVal", "ST", "SPS", "Indication 1 — status value"),
        new("GGIO1.Ind1.q", "LLN0", "GGIO1", "Ind1", "q", "ST", "SPS", "Quality"),
        new("MMXU1.A.phsA.cVal.mag.f", "LLN0", "MMXU1", "A", "phsA", "MX", "WYE", "Phase A current — magnitude")
    ];

    public IReadOnlyList<ProblemRow> Problems { get; } =
    [
        new("Error", "RPT-0014", "Reference", "BR.Buffer02", "DataSet 'DigitalX' not found."),
        new("Warning", "SCC-0021", "Target", "AA1E1F06R4", "Target compatibility profile reports a report-count risk."),
        new("Info", "DT-0037", "Model", "DPC_TYPE_14", "DataType is currently unused.")
    ];
}
