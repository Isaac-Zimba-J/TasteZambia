using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels.Items;

namespace TasteZambia.Core.ViewModels;

public sealed class SavedRowViewModel(DishItemViewModel dish, SavedEntry entry)
{
    public DishItemViewModel Dish { get; } = dish;
    public string When { get; } = entry.When;
    public string Note { get; } = entry.Note;
    public bool HasNote => Note.Length > 0;
}

public sealed class WishlistRowViewModel(DishItemViewModel dish, WishlistEntry entry)
{
    public DishItemViewModel Dish { get; } = dish;
    public string Why { get; } = entry.Why;
    public bool HasWhy => Why.Length > 0;
}

public sealed class CookedRowViewModel(DishItemViewModel dish, CookedEntry entry)
{
    public DishItemViewModel Dish { get; } = dish;
    public string Times { get; } = entry.Times;
    public string Last { get; } = entry.Last;
    public string Note { get; } = entry.Note;
    public bool HasNote => Note.Length > 0;
}

/// <summary>
/// Shared plumbing: resolve dish ids to DishItemViewModels in one catalog call,
/// so a collection screen is one round trip against HTTP rather than one per row.
/// </summary>
public abstract partial class CollectionViewModelBase(
    ICatalogService catalog,
    IFavouritesService favourites,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    protected async Task<Dictionary<string, DishItemViewModel>> ResolveAsync(IEnumerable<string> ids)
    {
        var dishes = await catalog.GetDishesByIdsAsync(ids.Distinct().ToList());
        return dishes.ToDictionary(
            d => d.Id,
            d => new DishItemViewModel(d, favourites, preferences, Navigation));
    }

    [RelayCommand]
    private Task BackToProfile() => Navigation.GoBackAsync();
}

public sealed class FavouritesViewModel(
    ICollectionsService collections, ICatalogService catalog, IFavouritesService favourites,
    IPreferenceService preferences, INavigationService navigation)
    : CollectionViewModelBase(catalog, favourites, preferences, navigation)
{
    public ObservableCollection<SavedRowViewModel> Items { get; } = [];
    public string CountLabel => "14 recipes";

    public override async Task InitializeAsync()
    {
        if (Items.Count > 0) return;

        var lookup = await ResolveAsync(collections.Saved.Select(s => s.DishId));
        foreach (var entry in collections.Saved)
            if (lookup.TryGetValue(entry.DishId, out var dish))
                Items.Add(new SavedRowViewModel(dish, entry));
    }
}

public sealed class WantToTryViewModel(
    ICollectionsService collections, ICatalogService catalog, IFavouritesService favourites,
    IPreferenceService preferences, INavigationService navigation)
    : CollectionViewModelBase(catalog, favourites, preferences, navigation)
{
    public ObservableCollection<WishlistRowViewModel> Items { get; } = [];
    public string CountLabel => "9 recipes";

    public override async Task InitializeAsync()
    {
        if (Items.Count > 0) return;

        var lookup = await ResolveAsync(collections.Wishlist.Select(w => w.DishId));
        foreach (var entry in collections.Wishlist)
            if (lookup.TryGetValue(entry.DishId, out var dish))
                Items.Add(new WishlistRowViewModel(dish, entry));
    }
}

public sealed class CookedViewModel(
    ICollectionsService collections, ICatalogService catalog, IFavouritesService favourites,
    IPreferenceService preferences, INavigationService navigation)
    : CollectionViewModelBase(catalog, favourites, preferences, navigation)
{
    public ObservableCollection<CookedRowViewModel> Items { get; } = [];
    public string CountLabel => "23 recipes · 62 times cooked";

    public override async Task InitializeAsync()
    {
        if (Items.Count > 0) return;

        var lookup = await ResolveAsync(collections.Cooked.Select(c => c.DishId));
        foreach (var entry in collections.Cooked)
            if (lookup.TryGetValue(entry.DishId, out var dish))
                Items.Add(new CookedRowViewModel(dish, entry));
    }
}

public sealed partial class FamilyRecipesViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<PreservedRecipe> Items { get; } = [];

    [ObservableProperty] private string _countLabel = "";

    protected override bool HasContent => Items.Count > 0;

    public override async Task InitializeAsync()
    {
        Items.Clear();
        foreach (var r in await archive.GetShelfAsync()) Items.Add(r);
        CountLabel = Items.Count == 1 ? "1 preserved" : $"{Items.Count} preserved";
    }

    [RelayCommand] private Task PreserveAnother() => Navigation.GoToAsync("famStart");
    [RelayCommand] private Task BackToProfile() => Navigation.GoBackAsync();
}

public sealed partial class SettingsViewModel(
    ICollectionsService collections, IDeviceIdentity identity, INavigationService navigation) : BaseViewModel(navigation)
{
    /// <summary>
    /// The account's own identifier. Shown because the privacy policy asks a reader to quote
    /// it when they write in to have their account deleted or to ask for a copy of what the
    /// archive holds - and without it on screen there is no way for them to say which account
    /// is theirs. It identifies the install, not the person.
    /// </summary>
    [ObservableProperty] private string _deviceId = "";

    public ObservableCollection<LanguageStatus> Languages { get; } = [];
    public ObservableCollection<SettingToggle> Toggles { get; } = [];

    public IReadOnlyList<string> AccountRows { get; } =
    [
        "Profile and photo",
        "Who can see my contributions",
        "Download everything I have added",
        "About the archive",
    ];

    public override async Task InitializeAsync()
    {
        DeviceId = (await identity.GetOrCreateAsync()).Id;

        if (Languages.Count > 0) return;
        foreach (var l in collections.Languages) Languages.Add(l);
        foreach (var t in collections.Toggles) Toggles.Add(t);
    }

    [RelayCommand] private Task BackToProfile() => Navigation.GoBackAsync();
}
