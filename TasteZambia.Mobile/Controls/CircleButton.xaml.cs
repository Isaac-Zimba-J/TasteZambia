using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace TasteZambia.Mobile.Controls;

public partial class CircleButton : ContentView
{
    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(CircleButton), "←");

    public static readonly BindableProperty GlyphColorProperty =
        BindableProperty.Create(nameof(GlyphColor), typeof(Color), typeof(CircleButton), Colors.Black);

    public static readonly BindableProperty DiameterProperty =
        BindableProperty.Create(nameof(Diameter), typeof(double), typeof(CircleButton), 36.0,
            propertyChanged: (b, _, _) => ((CircleButton)b).OnPropertyChanged(nameof(Shape)));

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(CircleButton), null);

    public static readonly BindableProperty SemanticLabelProperty =
        BindableProperty.Create(nameof(SemanticLabel), typeof(string), typeof(CircleButton), null,
            propertyChanged: (b, _, n) => SemanticProperties.SetDescription((CircleButton)b, (string?)n ?? ""));

    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public Color GlyphColor { get => (Color)GetValue(GlyphColorProperty); set => SetValue(GlyphColorProperty, value); }
    public double Diameter { get => (double)GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    /// <summary>Screen readers otherwise announce the raw glyph, e.g. "heart".</summary>
    public string? SemanticLabel { get => (string?)GetValue(SemanticLabelProperty); set => SetValue(SemanticLabelProperty, value); }

    public IShape Shape => new RoundRectangle { CornerRadius = Diameter / 2 };

    public CircleButton() => InitializeComponent();
}
