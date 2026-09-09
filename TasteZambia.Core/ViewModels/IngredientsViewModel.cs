using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class IngredientTileViewModel(
    Ingredient ingredient, IPreferenceService preferences, INavigationService navigation)
{
    private readonly DisplayName _name = preferences.Resolve(ingredient.LocalName, ingredient.EnglishName);

    public string Key { get; } = ingredient.Key;
    public string Title => _name.Title;
    public string Subtitle => _name.Subtitle;
    public string? ImageAsset { get; } = ingredient.ImageAsset;

    [RelayCommand]
    private Task Open()
        => navigation.GoToAsync("ingredient", new Dictionary<string, object> { ["key"] = Key });
}

public sealed class IngredientsViewModel(
    IIngredientRepository ingredients,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<IngredientTileViewModel> Items { get; } = [];

    public override async Task InitializeAsync()
    {
        if (Items.Count > 0) return;

        foreach (var ingredient in await ingredients.GetAllAsync())
            Items.Add(new IngredientTileViewModel(ingredient, preferences, Navigation));
    }
}
