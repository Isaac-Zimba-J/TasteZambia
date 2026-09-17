using TasteZambia.Core.Data;
using Dish = TasteZambia.Core.Models.Dish;
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

public class ExploreSearchRaceTests
{
    /// <summary>A catalog whose answers arrive in whatever order the test releases them.</summary>
    private sealed class ControllableCatalog : ICatalogService
    {
        private readonly Dictionary<string, TaskCompletionSource<IReadOnlyList<Dish>>> _pending = [];
        public List<CancellationToken> Tokens { get; } = [];

        public Task<IReadOnlyList<Dish>> SearchAsync(string query, string filter, CancellationToken ct = default)
        {
            Tokens.Add(ct);
            var tcs = new TaskCompletionSource<IReadOnlyList<Dish>>();
            _pending[query] = tcs;
            return tcs.Task;
        }

        public Task<IReadOnlyList<Dish>> GetDishesByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Dish>>([]);

        public void Answer(string query, params Dish[] dishes) => _pending[query].SetResult(dishes);
    }

    private sealed class StubNavigation : INavigationService
    {
        public Task GoToAsync(string route) => Task.CompletedTask;
        public Task GoToAsync(string route, IDictionary<string, object> p) => Task.CompletedTask;
        public Task GoBackAsync() => Task.CompletedTask;
    }

    [Fact]
    public async Task AStaleSearchThatFinishesLate_DoesNotOverwriteTheNewerOne()
    {
        var catalog = new ControllableCatalog();
        var vm = new ExploreViewModel(catalog, new FavouritesService(), new PreferenceService(), new StubNavigation());
        var all = await new InMemoryDishRepository().GetAllAsync();

        var init = vm.InitializeAsync();
        catalog.Answer("", all.ToArray());
        await init;

        vm.Query = "chi";        // first request, slow
        vm.Query = "chikanda";   // second request, fast

        catalog.Answer("chikanda", all.Single(d => d.Id == "chikanda"));
        await vm.WaitForSearchAsync();
        Assert.Single(vm.Results);

        // Now the OLD request lands with a broader answer. It must be ignored.
        catalog.Answer("chi", all.Where(d => d.Id is "chikanda" or "chibwabwa" or "inkoko").ToArray());
        await Task.Yield();

        Assert.Single(vm.Results);
        Assert.Equal("chikanda", vm.Results[0].Id);
        Assert.True(catalog.Tokens[1].IsCancellationRequested, "the superseded search should have been cancelled");
    }
}
