using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class ExploreViewModelTests
{
    private sealed class StubNavigation : INavigationService
    {
        public Task GoToAsync(string route) => Task.CompletedTask;
        public Task GoToAsync(string route, IDictionary<string, object> p) => Task.CompletedTask;
        public Task GoBackAsync() => Task.CompletedTask;
    }

    private static ExploreViewModel Sut() => new(
        new CatalogService(new InMemoryDishRepository()),
        new FavouritesService(),
        new PreferenceService(),
        new StubNavigation());

    [Fact]
    public async Task Initialize_ShowsEveryDishAndEightChipsWithAllSelected()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(8, vm.Results.Count);
        Assert.Equal(8, vm.Chips.Count);
        Assert.Equal("All", vm.Chips[0].Label);
        Assert.True(vm.Chips[0].IsSelected);
        Assert.False(vm.HasQuery);
    }

    [Fact]
    public async Task ResultCountLabel_IsSingularForOneResult()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Query = "ifisashi";
        await vm.WaitForSearchAsync();

        Assert.Equal("1 recipe", vm.ResultCountLabel);
        Assert.True(vm.HasQuery);
    }

    [Fact]
    public async Task ResultCountLabel_IsPluralOtherwise()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        Assert.Equal("8 recipes", vm.ResultCountLabel);
    }

    [Fact]
    public async Task NoMatch_SetsNoResults()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Query = "sushi";
        await vm.WaitForSearchAsync();

        Assert.True(vm.NoResults);
        Assert.Equal("0 recipes", vm.ResultCountLabel);
    }

    [Fact]
    public async Task ClearQuery_RestoresTheFullList()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        vm.Query = "sushi";
        await vm.WaitForSearchAsync();

        vm.ClearQueryCommand.Execute(null);
        await vm.WaitForSearchAsync();

        Assert.Equal("", vm.Query);
        Assert.Equal(8, vm.Results.Count);
        Assert.False(vm.NoResults);
    }

    [Fact]
    public async Task SelectingAChip_MovesTheSelection()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Chips[2].SelectCommand.Execute(null);

        Assert.False(vm.Chips[0].IsSelected);
        Assert.True(vm.Chips[2].IsSelected);
    }
}
