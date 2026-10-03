namespace ArSclStudio.Scl.Identity;

public readonly record struct SclNodeHandle(long Value)
{
    public static SclNodeHandle None => default;

    public bool IsNone => Value == 0;

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
