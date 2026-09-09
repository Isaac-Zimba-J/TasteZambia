using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels.Items;

namespace TasteZambia.Core.ViewModels;

public sealed partial class IngredientViewModel(
    IIngredientRepository ingredients,
    ICatalogService catalog,
    IFavouritesService favourites,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    /// <summary>Set from the navigation parameters. Defaults to the one seeded profile.</summary>
    public string Key { get; set; } = "chibwabwa";

    public ObservableCollection<LocalNameRow> LocalNames { get; } = [];
    public ObservableCollection<DishItemViewModel> UsedIn { get; } = [];

    [ObservableProperty] private string _englishLine = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _whereFound = "";
    [ObservableProperty] private string _traditionalPreparation = "";
    [ObservableProperty] private string _pendingLanguages = "";
    [ObservableProperty] private string? _heroAsset;

    public override async Task InitializeAsync()
    {
        if (LocalNames.Count > 0 || UsedIn.Count > 0) return;

        var ingredient = await ingredients.GetByKeyAsync(Key);
        if (ingredient is null) return;

        var name = preferences.Resolve(ingredient.LocalName, ingredient.EnglishName);

        Title = name.Title;
        EnglishLine = $"English name: {name.Subtitle}";
        Description = ingredient.Description;
        WhereFound = ingredient.WhereFound;
        TraditionalPreparation = ingredient.TraditionalPreparation;
        PendingLanguages = ingredient.PendingLanguages;
        HeroAsset = ingredient.ImageAsset;

        foreach (var local in ingredient.LocalNames)
            LocalNames.Add(new LocalNameRow(local.Language, local.Name));

        foreach (var dish in await catalog.GetDishesByIdsAsync(ingredient.UsedInDishIds))
            UsedIn.Add(new DishItemViewModel(dish, favourites, preferences, Navigation));
    }

    [RelayCommand]
    private Task Back() => Navigation.GoBackAsync();
}
