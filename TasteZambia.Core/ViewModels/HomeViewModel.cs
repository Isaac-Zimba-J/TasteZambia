using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels.Items;

namespace TasteZambia.Core.ViewModels;

public sealed record StoryTeaser(string Title, string Meta);

public sealed partial class HomeViewModel(
    ICatalogService catalog,
    ICategoryRepository categories,
    IFavouritesService favourites,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<DishItemViewModel> Dishes { get; } = [];
    public ObservableCollection<StoryTeaser> Stories { get; } = [];

    protected override void ClearForReload()
    {
        Categories.Clear();
        Dishes.Clear();
        Stories.Clear();
    }

    public override async Task InitializeAsync()
    {
        if (Dishes.Count > 0) return;

        IsBusy = true;
        try
        {
            foreach (var category in await categories.GetAllAsync())
                Categories.Add(category);

            foreach (var dish in await catalog.SearchAsync("", "All"))
                Dishes.Add(new DishItemViewModel(dish, favourites, preferences, Navigation));

            foreach (var (title, meta) in SeedData.HomeStories)
                Stories.Add(new StoryTeaser(title, meta));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand] private Task OpenExplore() => Navigation.GoToAsync("//explore");
    [RelayCommand] private Task OpenCulture() => Navigation.GoToAsync("//culture");
}
