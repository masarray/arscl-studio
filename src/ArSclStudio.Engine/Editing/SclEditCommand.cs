using ArSclStudio.Engine.Documents;
using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Engine.Editing;

public sealed record DescriptionChange(SclNodeHandle Target, string? Before, string? After);

/// <summary>Commands prepare declarative patches. Only the transaction kernel can apply them.</summary>
public interface ISclEditCommand
{
    string Description { get; }
    IReadOnlyList<DescriptionChange> Prepare(SclDocumentState state);
}

public sealed record SetIedDescriptionCommand(
    SclNodeHandle Target, string? ExpectedValue, string? Value) : ISclEditCommand
{
    public string Description => "Edit IED description";
    public IReadOnlyList<DescriptionChange> Prepare(SclDocumentState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return [new DescriptionChange(Target, ExpectedValue, Value)];
    }
}

public sealed class CompoundEditCommand : ISclEditCommand
{
    private readonly ISclEditCommand[] _commands;

    public CompoundEditCommand(string description, params ISclEditCommand[] commands)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(commands);
        if (commands.Length is 0 or > 64)
        {
            throw new ArgumentException("A compound edit requires 1–64 commands.", nameof(commands));
        }

        Description = description;
        _commands = (ISclEditCommand[])commands.Clone();
    }

    public string Description { get; }

    public IReadOnlyList<DescriptionChange> Prepare(SclDocumentState state)
    {
        var changes = new List<DescriptionChange>();
        foreach (var command in _commands)
        {
            var prepared = command.Prepare(state);
            if (changes.Count + prepared.Count > 64)
            {
                throw new InvalidOperationException("A transaction is limited to 64 property changes.");
            }

            changes.AddRange(prepared);
        }

        return changes;
    }
}
