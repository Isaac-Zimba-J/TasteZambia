using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class ProvinceChipViewModel(string name, Action<ProvinceChipViewModel> onSelect)
    : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty]
    private bool _isSelected;

    [RelayCommand]
    private void Select() => onSelect(this);
}

public sealed partial class ProvinceFoodViewModel(
    string name, string subtitle, INavigationService navigation, string? dishId)
{
    public string Name { get; } = name;
    public string Subtitle { get; } = subtitle;
    public bool HasSubtitle => Subtitle.Length > 0;

    [RelayCommand]
    private Task Open() => dishId is null
        ? Task.CompletedTask
        : navigation.GoToAsync("recipe", new Dictionary<string, object> { ["dishId"] = dishId });
}

public sealed partial class RegionsViewModel(
    IRegionRepository regions,
    IDishRepository dishes,
    INavigationService navigation) : BaseViewModel(navigation)
{
    private IReadOnlyList<Province> _all = [];
    private IReadOnlyList<Dish> _allDishes = [];

    public ObservableCollection<ProvinceChipViewModel> Provinces { get; } = [];
    public ObservableCollection<string> SelectedIngredients { get; } = [];
    public ObservableCollection<ProvinceFoodViewModel> SelectedFoods { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FoodsHeading))]
    private string _selectedName = "";

    [ObservableProperty] private string _selectedSeat = "";
    [ObservableProperty] private string _selectedBlurb = "";
    [ObservableProperty] private string _selectedTradition = "";

    public string FoodsHeading => $"Foods of {SelectedName} Province";

    public override async Task InitializeAsync()
    {
        if (Provinces.Count > 0) return;

        _all = await regions.GetAllAsync();

        // Fetched once, not per province: selecting a chip must not re-query.
        _allDishes = await dishes.GetAllAsync();

        foreach (var province in _all)
            Provinces.Add(new ProvinceChipViewModel(province.Name, Select));

        // Index 6 is Northern - the design opens on it.
        Select(Provinces[6]);
    }

    private void Select(ProvinceChipViewModel chip)
    {
        foreach (var c in Provinces)
            c.IsSelected = ReferenceEquals(c, chip);

        var province = _all.First(p => p.Name == chip.Name);

        SelectedName = province.Name;
        SelectedSeat = province.Seat;
        SelectedBlurb = province.Blurb;
        SelectedTradition = province.CookingTradition;

        SelectedIngredients.Clear();
        foreach (var ingredient in province.CommonIngredients)
            SelectedIngredients.Add(ingredient);

        SelectedFoods.Clear();
        foreach (var food in province.SignatureFoods)
        {
            var dish = _allDishes.FirstOrDefault(d => d.LocalName == food);

            // Some provincial foods are not seeded dishes; the archive still knows
            // their English name, e.g. Katapa.
            var subtitle = dish?.EnglishName
                ?? (SeedData.ExtraFoodSubtitles.TryGetValue(food, out var extra) ? extra : "");

            SelectedFoods.Add(new ProvinceFoodViewModel(food, subtitle, Navigation, dish?.Id));
        }
    }
}
