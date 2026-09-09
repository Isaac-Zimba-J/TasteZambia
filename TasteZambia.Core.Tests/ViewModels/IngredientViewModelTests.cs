using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public List<string> Routes { get; } = [];
    public int BackCount { get; private set; }
    public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoBackAsync() { BackCount++; return Task.CompletedTask; }
}

public class IngredientsViewModelTests
{
    [Fact]
    public async Task Initialize_LoadsNineIngredientsWithLocalNamesAsTitles()
    {
        var vm = new IngredientsViewModel(new InMemoryIngredientRepository(), new PreferenceService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(9, vm.Items.Count);
        Assert.Equal("Chibwabwa", vm.Items[0].Title);
        Assert.Equal("Pumpkin leaves", vm.Items[0].Subtitle);
    }

    [Fact]
    public async Task OnlyChibwabwaHasPhotography()
    {
        var vm = new IngredientsViewModel(new InMemoryIngredientRepository(), new PreferenceService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("market_ingredients.png", vm.Items[0].ImageAsset);
        Assert.All(vm.Items.Skip(1), i => Assert.Null(i.ImageAsset));
    }

    [Fact]
    public async Task OpeningATile_NavigatesToTheIngredientRoute()
    {
        var nav = new Nav();
        var vm = new IngredientsViewModel(new InMemoryIngredientRepository(), new PreferenceService(), nav);
        await vm.InitializeAsync();

        vm.Items[0].OpenCommand.Execute(null);

        Assert.Equal("ingredient", nav.Routes.Single());
    }
}

public class IngredientViewModelTests
{
    private static IngredientViewModel Sut(INavigationService nav) => new(
        new InMemoryIngredientRepository(),
        new CatalogService(new InMemoryDishRepository()),
        new FavouritesService(), new PreferenceService(), nav) { Key = "chibwabwa" };

    [Fact]
    public async Task Initialize_LoadsChibwabwa()
    {
        var vm = Sut(new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Chibwabwa", vm.Title);
        Assert.Equal("English name: Pumpkin leaves", vm.EnglishLine);
        Assert.Equal("market_ingredients.png", vm.HeroAsset);
        Assert.Equal("Tonga, Lozi, Kaonde, Lunda, Luvale", vm.PendingLanguages);
        Assert.Single(vm.LocalNames);
        Assert.Equal("Bemba, Nyanja", vm.LocalNames[0].Language);
    }

    [Fact]
    public async Task UsedIn_ResolvesDishesInTheOrderTheIngredientLists()
    {
        var vm = Sut(new Nav());
        await vm.InitializeAsync();

        Assert.Equal(["ifisashi", "nshima", "delele"], vm.UsedIn.Select(d => d.Id));
    }

    [Fact]
    public async Task Back_PopsTheStack()
    {
        var nav = new Nav();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        await vm.BackCommand.ExecuteAsync(null);

        Assert.Equal(1, nav.BackCount);
    }
}
