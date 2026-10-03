using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ArSclStudio.Desktop.Converters;

public sealed class DepthToIndentConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        var depth = value is int parsed
            ? Math.Clamp(parsed, 0, 32)
            : 0;

        return new Thickness(
            5 + (depth * 12),
            3,
            5,
            3);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        throw new NotSupportedException();
}
