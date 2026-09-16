using System.Windows.Input;
using FluentIcons.Common;
using Microsoft.Maui.Controls.Shapes;

namespace TasteZambia.Mobile.Controls;

public partial class CircleButton : ContentView
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(Icon), typeof(CircleButton), FluentIcons.Common.Icon.ArrowLeft);

    public static readonly BindableProperty IconVariantProperty =
        BindableProperty.Create(nameof(IconVariant), typeof(IconVariant), typeof(CircleButton), FluentIcons.Common.IconVariant.Regular);

    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(CircleButton), Colors.Black);

    public static readonly BindableProperty IconSizeProperty =
        BindableProperty.Create(nameof(IconSize), typeof(IconSize), typeof(CircleButton), FluentIcons.Common.IconSize.Size20);

    public static readonly BindableProperty DiameterProperty =
        BindableProperty.Create(nameof(Diameter), typeof(double), typeof(CircleButton), 36.0,
            propertyChanged: (b, _, _) => ((CircleButton)b).OnPropertyChanged(nameof(Shape)));

    public static readonly BindableProperty BackgroundProperty =
        BindableProperty.Create(nameof(Background), typeof(Color), typeof(CircleButton),
            Color.FromArgb("#E6FFFDF9"));

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(CircleButton), null);

    public static readonly BindableProperty SemanticLabelProperty =
        BindableProperty.Create(nameof(SemanticLabel), typeof(string), typeof(CircleButton), null,
            propertyChanged: (b, _, n) => ((CircleButton)b).ApplySemanticLabel((string?)n));

    public Icon Icon { get => (Icon)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public IconVariant IconVariant { get => (IconVariant)GetValue(IconVariantProperty); set => SetValue(IconVariantProperty, value); }
    public Color IconColor { get => (Color)GetValue(IconColorProperty); set => SetValue(IconColorProperty, value); }
    public IconSize IconSize { get => (IconSize)GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public double Diameter { get => (double)GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }
    public new Color Background { get => (Color)GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    /// <summary>Screen readers need words, not a shape.</summary>
    public string? SemanticLabel { get => (string?)GetValue(SemanticLabelProperty); set => SetValue(SemanticLabelProperty, value); }

    public IShape Shape => new RoundRectangle { CornerRadius = Diameter / 2 };

    public CircleButton()
    {
        InitializeComponent();
        ApplySemanticLabel(SemanticLabel);
    }

    /// <summary>
    /// Description and tap gesture must live on the same element: a screen reader
    /// dispatches double-tap to the node it has focused, so describing the outer
    /// ContentView while the gesture sits on the Grid would announce a button that
    /// cannot be activated.
    /// </summary>
    private void ApplySemanticLabel(string? label)
    {
        SemanticProperties.SetDescription(Surface, label ?? "");
        AutomationProperties.SetIsInAccessibleTree(Surface, !string.IsNullOrEmpty(label));
    }
}
