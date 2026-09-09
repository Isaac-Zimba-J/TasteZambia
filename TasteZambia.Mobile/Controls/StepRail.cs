namespace TasteZambia.Mobile.Controls;

/// <summary>
/// Four-segment progress rail used by onboarding screens 3-6. Filled segments are
/// gold-light on the dark green grounds and TzGoldDecor on cream, because gold text
/// weight and gold decoration are separate roles after the v2 contrast pass.
/// </summary>
public sealed class StepRail : ContentView
{
    public static readonly BindableProperty StepProperty =
        BindableProperty.Create(nameof(Step), typeof(int), typeof(StepRail), 1,
            propertyChanged: (b, _, _) => ((StepRail)b).Rebuild());

    public static readonly BindableProperty OnDarkProperty =
        BindableProperty.Create(nameof(OnDark), typeof(bool), typeof(StepRail), false,
            propertyChanged: (b, _, _) => ((StepRail)b).Rebuild());

    public int Step { get => (int)GetValue(StepProperty); set => SetValue(StepProperty, value); }
    public bool OnDark { get => (bool)GetValue(OnDarkProperty); set => SetValue(OnDarkProperty, value); }

    private readonly Grid _rail = new()
    {
        ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)],
        ColumnSpacing = 6,
        HeightRequest = 3,
    };

    public StepRail()
    {
        Content = _rail;
        Rebuild();
    }

    private void Rebuild()
    {
        _rail.Children.Clear();

        var filled = Get(OnDark ? "TzGoldLight" : "TzGoldDecor");
        var empty = Get(OnDark ? "TzOnDark20" : "TzRule");

        for (var i = 0; i < 4; i++)
        {
            var bar = new BoxView { HeightRequest = 3, CornerRadius = 2, Color = i < Step ? filled : empty };
            _rail.Add(bar, i, 0);
        }

        SemanticProperties.SetDescription(this, $"Step {Step} of 4");
    }

    private static Color Get(string key)
        => Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c ? c : Colors.Gray;
}
