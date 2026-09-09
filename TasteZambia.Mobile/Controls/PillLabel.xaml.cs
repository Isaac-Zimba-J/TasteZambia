namespace TasteZambia.Mobile.Controls;

public partial class PillLabel : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(PillLabel), "");

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(PillLabel), Colors.Black);

    public static readonly BindableProperty PillBackgroundProperty =
        BindableProperty.Create(nameof(PillBackground), typeof(Color), typeof(PillLabel), Colors.Transparent);

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public Color PillBackground { get => (Color)GetValue(PillBackgroundProperty); set => SetValue(PillBackgroundProperty, value); }

    public PillLabel() => InitializeComponent();
}
