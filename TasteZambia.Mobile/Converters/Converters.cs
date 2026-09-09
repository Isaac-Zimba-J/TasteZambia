using System.Globalization;

namespace TasteZambia.Mobile.Converters;

public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

public sealed class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && value is not string { Length: 0 };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>ViewModels carry hex strings so TasteZambia.Core stays free of MAUI types.</summary>
public sealed class HexToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string hex && hex.Length > 0 ? Color.FromArgb(hex) : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Maps a saved/unsaved flag to a Fluent icon variant, so the heart fills when saved.
/// Lives here rather than in TasteZambia.Core: the Core layer stays free of both MAUI
/// and the icon library.
/// </summary>
public sealed class BoolToIconVariantConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? FluentIcons.Common.IconVariant.Filled : FluentIcons.Common.IconVariant.Regular;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
