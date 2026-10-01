using FluentIcons.Common;
using FluentIcons.Maui;
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

    /// <summary>
    /// The icon is part of what each tab means, not decoration: a reader glancing down should
    /// recognise where they are before reading the word. Filled when active, outline when not,
    /// which is the convention every Android user already knows.
    /// </summary>
    private static readonly (string Key, string Label, Icon Glyph)[] Sections =
    [
        ("home", "Home", Icon.Home),
        ("explore", "Explore", Icon.Food),
        ("regions", "Regions", Icon.Map),
        ("culture", "Culture", Icon.Book),
        ("profile", "Profile", Icon.Person),
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
            var (key, label, glyph) = Sections[i];
            var active = key == ActiveSection;
            var slot = slots[i];

            slot.Children.Clear();

            // 4pt dot: clay when active, transparent otherwise. Kept from the design - it is
            // the one marker that does not rely on telling two colours apart.
            slot.Children.Add(new BoxView
            {
                WidthRequest = 4,
                HeightRequest = 4,
                CornerRadius = 2,
                HorizontalOptions = LayoutOptions.Center,
                Color = active ? GetColor("TzClay") : Colors.Transparent,
            });

            var icon = new FluentIcon
            {
                Icon = glyph,
                IconVariant = active ? IconVariant.Filled : IconVariant.Regular,
                IconSize = IconSize.Size20,
                ForegroundColor = GetColor(active ? "TzGreenDeep" : "TzMuted"),
                HorizontalOptions = LayoutOptions.Center,
            };

            // The slot already announces the tab and whether it is selected; letting the icon
            // speak too would read every tab twice.
            AutomationProperties.SetIsInAccessibleTree(icon, false);
            slot.Children.Add(icon);

            slot.Children.Add(new Label
            {
                Text = label,
                FontFamily = active ? "ArchivoSemiBold" : "ArchivoRegular",
                FontSize = 10,
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
