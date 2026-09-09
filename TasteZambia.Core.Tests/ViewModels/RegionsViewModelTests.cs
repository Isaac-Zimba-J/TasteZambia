using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class RegionsViewModelTests
{
    private sealed class Nav : INavigationService
    {
        public List<string> Routes { get; } = [];
        public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
        public Task GoBackAsync() => Task.CompletedTask;
    }

    private static RegionsViewModel Sut(INavigationService? nav = null)
        => new(new InMemoryRegionRepository(), new InMemoryDishRepository(), nav ?? new Nav());

    [Fact]
    public async Task DefaultsToNorthern_MatchingTheDesign()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(10, vm.Provinces.Count);
        Assert.Equal("Northern", vm.SelectedName);
        Assert.Equal("Kasama", vm.SelectedSeat);
        Assert.True(vm.Provinces[6].IsSelected);
        Assert.Equal("Foods of Northern Province", vm.FoodsHeading);
    }

    [Fact]
    public async Task SelectingAProvince_SwapsEveryDetailField()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Provinces[3].SelectCommand.Execute(null);

        Assert.Equal("Luapula", vm.SelectedName);
        Assert.Equal("Mansa", vm.SelectedSeat);
        Assert.False(vm.Provinces[6].IsSelected);
        Assert.True(vm.Provinces[3].IsSelected);
        Assert.Equal(3, vm.SelectedIngredients.Count);
        Assert.Equal("Foods of Luapula Province", vm.FoodsHeading);
    }

    [Fact]
    public async Task FoodsNotInTheDishArchive_StillGetTheirEnglishSubtitle()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        var katapa = vm.SelectedFoods.Single(f => f.Name == "Katapa");
        Assert.True(katapa.HasSubtitle);
        Assert.Equal("Cassava leaf relish", katapa.Subtitle);
    }

    [Fact]
    public async Task OpeningASeededFood_NavigatesToTheRecipeRoute()
    {
        var nav = new Nav();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        vm.SelectedFoods[0].OpenCommand.Execute(null);   // Ifisashi

        Assert.Equal("recipe", nav.Routes.Single());
    }

    [Fact]
    public async Task OpeningAFoodWithNoDish_DoesNothing()
    {
        var nav = new Nav();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        vm.SelectedFoods.Single(f => f.Name == "Katapa").OpenCommand.Execute(null);

        Assert.Empty(nav.Routes);
    }
}
