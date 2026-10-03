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
            depth * 12,
            0,
            0,
            0);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        throw new NotSupportedException();
}
