using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Scl.Semantics;

public sealed record SclIedSummary(
    SclNodeHandle Handle,
    string Name,
    string? Manufacturer);
