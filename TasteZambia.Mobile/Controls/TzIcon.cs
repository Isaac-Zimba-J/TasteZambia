using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace TasteZambia.Mobile.Controls;

/// <summary>
/// Stroke-based vector icons on a 24x24 grid, drawn as Path geometry.
///
/// Deliberately not an icon font and not emoji: emoji render differently on every
/// platform and cannot be recoloured, and a font would add a dependency and a
/// licence. Path data scales crisply to any size and takes its colour at runtime,
/// which the archive needs - the same heart is clay when saved and grey when not.
/// </summary>
public sealed class TzIcon : ContentView
{
    private const double Grid = 24.0;

    /// <summary>Outline icons: stroked, never filled.</summary>
    private static readonly Dictionary<string, string> Outline = new()
    {
        ["search"]        = "M 18 11 A 7 7 0 1 1 4 11 A 7 7 0 1 1 18 11 M 16.2 16.2 L 21 21",
        ["heart"]         = "M 12 20.7 C 12 20.7 3.2 14.9 3.2 8.9 A 4.9 4.9 0 0 1 12 6.2 A 4.9 4.9 0 0 1 20.8 8.9 C 20.8 14.9 12 20.7 12 20.7 Z",
        ["close"]         = "M 6.5 6.5 L 17.5 17.5 M 17.5 6.5 L 6.5 17.5",
        ["arrow-left"]    = "M 19.5 12 L 4.5 12 M 11 5.5 L 4.5 12 L 11 18.5",
        ["arrow-right"]   = "M 4.5 12 L 19.5 12 M 13 5.5 L 19.5 12 L 13 18.5",
        ["chevron-right"] = "M 9 4.8 L 16.2 12 L 9 19.2",
        ["check"]         = "M 5 12.8 L 9.6 17.4 L 19 8",
        ["sort"]          = "M 4 7 L 20 7 M 7 12 L 17 12 M 10 17 L 14 17",
    };

    /// <summary>Solid variants, used where the design fills a shape rather than outlining it.</summary>
    private static readonly Dictionary<string, string> Solid = new()
    {
        ["heart"] = "M 12 20.7 C 12 20.7 3.2 14.9 3.2 8.9 A 4.9 4.9 0 0 1 12 6.2 A 4.9 4.9 0 0 1 20.8 8.9 C 20.8 14.9 12 20.7 12 20.7 Z",
    };

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(TzIcon), "search",
            propertyChanged: (b, _, _) => ((TzIcon)b).Rebuild());

    public static readonly BindableProperty IconSizeProperty =
        BindableProperty.Create(nameof(IconSize), typeof(double), typeof(TzIcon), 20.0,
            propertyChanged: (b, _, _) => ((TzIcon)b).Rebuild());

    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(TzIcon), Colors.Black,
            propertyChanged: (b, _, _) => ((TzIcon)b).Rebuild());

    public static readonly BindableProperty IsFilledProperty =
        BindableProperty.Create(nameof(IsFilled), typeof(bool), typeof(TzIcon), false,
            propertyChanged: (b, _, _) => ((TzIcon)b).Rebuild());

    public static readonly BindableProperty StrokeWeightProperty =
        BindableProperty.Create(nameof(StrokeWeight), typeof(double), typeof(TzIcon), 1.9,
            propertyChanged: (b, _, _) => ((TzIcon)b).Rebuild());

    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public double IconSize { get => (double)GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public Color IconColor { get => (Color)GetValue(IconColorProperty); set => SetValue(IconColorProperty, value); }
    public bool IsFilled { get => (bool)GetValue(IsFilledProperty); set => SetValue(IsFilledProperty, value); }
    public double StrokeWeight { get => (double)GetValue(StrokeWeightProperty); set => SetValue(StrokeWeightProperty, value); }

    public TzIcon()
    {
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        Rebuild();
    }

    private void Rebuild()
    {
        var name = Icon ?? "search";
        var filled = IsFilled && Solid.ContainsKey(name);
        var source = filled ? Solid : Outline;

        if (!source.TryGetValue(name, out var data))
        {
            Content = null;
            return;
        }

        var path = new Path
        {
            Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString(data)!,
            Aspect = Stretch.None,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };

        if (filled)
        {
            path.Fill = new SolidColorBrush(IconColor);
        }
        else
        {
            path.Stroke = new SolidColorBrush(IconColor);
            path.StrokeThickness = StrokeWeight;
            path.StrokeLineCap = PenLineCap.Round;
            path.StrokeLineJoin = PenLineJoin.Round;
        }

        // Geometry is authored on a 24x24 grid; scale it to the requested size.
        var scale = IconSize / Grid;
        path.Scale = scale;

        WidthRequest = IconSize;
        HeightRequest = IconSize;
        Content = path;
    }
}
