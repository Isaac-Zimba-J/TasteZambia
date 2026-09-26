using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.Tests.Fakes;
using TasteZambia.Core.ViewModels;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.ViewModels;

public class CollectionsTests
{
    private sealed class Nav : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoBackAsync() => Task.CompletedTask;
    }

    private static (ICatalogService cat, IFavouritesService fav, IPreferenceService pref) Deps()
        => (new CatalogService(new InMemoryDishRepository()), TestServices.Favourites(), new PreferenceService());

    [Fact]
    public async Task Favourites_AreSixDishesWithTwoCarryingNotes()
    {
        var (cat, fav, pref) = Deps();
        var vm = new FavouritesViewModel(new CollectionsService(), cat, fav, pref, new Nav());
        await vm.InitializeAsync();

        Assert.Equal(6, vm.Items.Count);
        Assert.Equal("Ifisashi", vm.Items[0].Dish.Title);
        Assert.Equal("Saved March 2026", vm.Items[0].When);
        Assert.True(vm.Items[0].HasNote);
        Assert.Equal("The one I cook most", vm.Items[0].Note);
        Assert.False(vm.Items[1].HasNote);
        Assert.Equal(2, vm.Items.Count(i => i.HasNote));
        Assert.Equal("14 recipes", vm.CountLabel);
    }

    [Fact]
    public async Task WantToTry_HasThreeReasonsOutOfFive()
    {
        var (cat, fav, pref) = Deps();
        var vm = new WantToTryViewModel(new CollectionsService(), cat, fav, pref, new Nav());
        await vm.InitializeAsync();

        Assert.Equal(5, vm.Items.Count);
        Assert.Equal("Munkoyo", vm.Items[0].Dish.Title);
        Assert.Contains("Grandfather", vm.Items[0].Why);
        Assert.Equal(3, vm.Items.Count(i => i.HasWhy));
        Assert.Equal("9 recipes", vm.CountLabel);
    }

    [Fact]
    public async Task Cooked_LeadsWithNshimaAtThirtyOneTimes()
    {
        var (cat, fav, pref) = Deps();
        var vm = new CookedViewModel(new CollectionsService(), cat, fav, pref, new Nav());
        await vm.InitializeAsync();

        Assert.Equal(5, vm.Items.Count);
        Assert.Equal("Nshima", vm.Items[0].Dish.Title);
        Assert.Equal("Cooked 31 times", vm.Items[0].Times);
        Assert.Equal("Yesterday", vm.Items[0].Last);
        Assert.Equal("23 recipes · 62 times cooked", vm.CountLabel);
    }

    [Fact]
    public async Task FamilyRecipes_ShowMixedPrivacy()
    {
        var nav = new Nav();
        var family = new FakeFamilyService();
        family.Add(NewRecipe("Ifisashi ya Banakulu", PrivacyLevel.PublicInArchive));
        family.Add(NewRecipe("Inkoko ya Bataata", PrivacyLevel.SharedWithFamily));
        family.Add(NewRecipe("Munkoyo wa Ba Shikulu", PrivacyLevel.PrivateToMe));
        var vm = new FamilyRecipesViewModel(family, nav);
        await vm.InitializeAsync();

        Assert.Equal(3, vm.Items.Count);
        Assert.Contains(vm.Items, i => i.Name == "Ifisashi ya Banakulu" && i.Privacy == "Public");
        Assert.Contains(vm.Items, i => i.Name == "Munkoyo wa Ba Shikulu" && i.Privacy == "Private");
        Assert.Equal("3 preserved", vm.CountLabel);

        vm.PreserveAnotherCommand.Execute(null);
        Assert.Equal("famStart", nav.Routes.Single());
    }

    private static FamilyRecipeDto NewRecipe(string name, PrivacyLevel privacy) => new(
        Guid.NewGuid(), name, "", "Northern", "Bemba", "", "", "", "",
        privacy, TranscriptState.None, null, 100, true, DateTimeOffset.UtcNow, [], [], []);

    [Fact]
    public async Task Settings_HasEightLanguagesAndFiveToggles()
    {
        var vm = new SettingsViewModel(new CollectionsService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(8, vm.Languages.Count);
        Assert.True(vm.Languages[0].IsCurrent);
        Assert.Equal("English", vm.Languages[0].Name);
        Assert.Contains("In progress", vm.Languages[3].Note);

        Assert.Equal(5, vm.Toggles.Count);
        Assert.True(vm.Toggles[0].IsOn);
        Assert.False(vm.Toggles[4].IsOn);
        Assert.Equal(4, vm.AccountRows.Count);
    }

    [Fact]
    public async Task SavedHeartsAreLive_TogglingWritesThroughFavourites()
    {
        var (cat, fav, pref) = Deps();
        var vm = new FavouritesViewModel(new CollectionsService(), cat, fav, pref, new Nav());
        await vm.InitializeAsync();

        var ifisashi = vm.Items[0].Dish;
        Assert.False(ifisashi.IsSaved);

        ifisashi.ToggleSaveCommand.Execute(null);

        Assert.True(ifisashi.IsSaved);
        Assert.True(fav.IsSaved("ifisashi"));
    }
}
