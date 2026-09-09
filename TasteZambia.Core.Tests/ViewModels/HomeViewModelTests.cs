using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class RecordingNavigation : INavigationService
{
    public List<string> Routes { get; } = [];
    public Task GoToAsync(string route) { Routes.Add(route); return Task.CompletedTask; }
    public Task GoToAsync(string route, IDictionary<string, object> p) { Routes.Add(route); return Task.CompletedTask; }
    public Task GoBackAsync() => Task.CompletedTask;
}

public class HomeViewModelTests
{
    private static HomeViewModel Sut(INavigationService? nav = null) => new(
        new CatalogService(new InMemoryDishRepository()),
        new InMemoryCategoryRepository(),
        new FavouritesService(),
        new PreferenceService(),
        nav ?? new RecordingNavigation());

    [Fact]
    public async Task Initialize_LoadsCategoriesDishesAndStories()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(8, vm.Categories.Count);
        Assert.Equal(8, vm.Dishes.Count);
        Assert.Equal(3, vm.Stories.Count);
    }

    [Fact]
    public async Task DishItems_PutTheLocalNameInTheTitle()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("Ifisashi", vm.Dishes[0].Title);
        Assert.Equal("Groundnut and greens relish", vm.Dishes[0].Subtitle);
    }

    [Fact]
    public async Task IfisashiStartsSaved_AndToggleFlipsTheGlyph()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        var ifisashi = vm.Dishes[0];

        Assert.True(ifisashi.IsSaved);
        Assert.Equal("♥", ifisashi.SaveGlyph);

        ifisashi.ToggleSaveCommand.Execute(null);

        Assert.False(ifisashi.IsSaved);
        Assert.Equal("♡", ifisashi.SaveGlyph);
        Assert.Contains("Save Ifisashi", ifisashi.SaveSemanticLabel);
    }

    [Fact]
    public async Task OpeningADish_NavigatesToTheRecipeRoute()
    {
        var nav = new RecordingNavigation();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        vm.Dishes[0].OpenCommand.Execute(null);

        Assert.Equal("recipe", nav.Routes.Single());
    }
}
