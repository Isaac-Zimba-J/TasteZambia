using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Controls;

public partial class BottomNavBar : ContentView
{
    public static readonly BindableProperty ActiveSectionProperty =
        BindableProperty.Create(nameof(ActiveSection), typeof(string), typeof(BottomNavBar), "home",
            propertyChanged: (b, _, _) => ((BottomNavBar)b).Rebuild());

    public string ActiveSection
    {
        get => (string)GetValue(ActiveSectionProperty);
        set => SetValue(ActiveSectionProperty, value);
    }

    private static readonly (string Key, string Label)[] Sections =
    [
        ("home", "Home"), ("explore", "Explore"), ("regions", "Regions"),
        ("culture", "Culture"), ("profile", "Profile"),
    ];

    public BottomNavBar()
    {
        InitializeComponent();
        Rebuild();
    }

    private void Rebuild()
    {
        VerticalStackLayout[] slots = [HomeItem, ExploreItem, RegionsItem, CultureItem, ProfileItem];

        for (var i = 0; i < Sections.Length; i++)
        {
            var (key, label) = Sections[i];
            var active = key == ActiveSection;
            var slot = slots[i];

            slot.Children.Clear();

            // 5pt dot: clay when active, transparent otherwise.
            slot.Children.Add(new BoxView
            {
                WidthRequest = 5,
                HeightRequest = 5,
                CornerRadius = 2.5,
                HorizontalOptions = LayoutOptions.Center,
                Color = active ? GetColor("TzClay") : Colors.Transparent,
            });

            slot.Children.Add(new Label
            {
                Text = label,
                FontFamily = active ? "ArchivoSemiBold" : "ArchivoRegular",
                FontSize = 10.5,
                CharacterSpacing = 0.1,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = GetColor(active ? "TzGreenDeep" : "TzMuted"),
            });

            var tap = new TapGestureRecognizer();
            var target = key;
            tap.Tapped += async (_, _) => await NavigateAsync(target);
            slot.GestureRecognizers.Clear();
            slot.GestureRecognizers.Add(tap);

            SemanticProperties.SetDescription(slot, label);
            SemanticProperties.SetHint(slot, active ? $"{label}, selected" : $"Go to {label}");
        }
    }

    private static async Task NavigateAsync(string section)
    {
        var nav = Application.Current?.Handler?.MauiContext?.Services
            .GetService<INavigationService>();

        if (nav is not null)
            await nav.GoToAsync($"//{section}");
    }

    private static Color GetColor(string key)
        => Application.Current!.Resources.TryGetValue(key, out var value) && value is Color c
            ? c
            : Colors.Black;
}
