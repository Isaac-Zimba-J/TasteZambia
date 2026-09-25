using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.Tests.Fakes;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public Task GoToAsync(string r) => Task.CompletedTask;
    public Task GoToAsync(string r, IDictionary<string, object> p) => Task.CompletedTask;
    public Task GoBackAsync() => Task.CompletedTask;
}

/// <summary>Counts reads so a refresh can be told apart from the initial load.</summary>
file sealed class CountingProfileRepository(IContributionService contributions) : IProfileRepository
{
    public int Reads { get; private set; }

    public Task<UserProfile> GetAsync(CancellationToken ct = default)
    {
        Reads++;
        return Task.FromResult(SeedData.Profile);
    }

    public Task UpdateAsync(string name, string location, string languages, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<RecipeCollection>> GetCollectionsAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Collections);

    public Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default)
        => contributions.GetContributionsAsync(ct);
}

public class RefreshTests
{
    [Fact]
    public async Task Profile_Refresh_PicksUpAContributionSubmittedAfterTheFirstLoad()
    {
        var fake = new FakeContributionService();
        var repository = new CountingProfileRepository(fake);
        var vm = new ProfileViewModel(repository, new Nav());

        await vm.InitializeAsync();
        Assert.Empty(vm.Contributions);

        fake.Add(ShareLifecycleTests.Detail(Shared.Enums.ContributionStatus.InReview, [ShareLifecycleTests.Submitted()]));

        // Initialize alone would return early: the collections are already filled.
        await vm.InitializeAsync();
        Assert.Empty(vm.Contributions);

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Single(vm.Contributions);
        Assert.Equal(2, repository.Reads);
        Assert.False(vm.IsRefreshing);
    }

    [Fact]
    public async Task Refresh_DoesNotDuplicateRows()
    {
        var vm = new HomeViewModel(
            new CatalogService(new InMemoryDishRepository()),
            new InMemoryCategoryRepository(),
            TestServices.Favourites(),
            new PreferenceService(),
            new Nav());

        await vm.InitializeAsync();
        var first = vm.Dishes.Count;

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(first, vm.Dishes.Count);
        Assert.Equal(8, vm.Categories.Count);
        Assert.False(vm.IsRefreshing);
    }

    [Fact]
    public async Task Refresh_WhenTheLoadFails_StopsTheSpinnerAndSaysWhy()
    {
        var vm = new ProfileViewModel(new ThrowingProfileRepository(), new Nav());

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.False(vm.IsRefreshing);
        Assert.Equal(BaseViewModel.OfflineMessage, vm.LoadError);   // reported, not thrown at the reader
    }

    private sealed class ThrowingProfileRepository : IProfileRepository
    {
        public Task<UserProfile> GetAsync(CancellationToken ct = default) => throw new HttpRequestException("offline");
        public Task UpdateAsync(string name, string location, string languages, CancellationToken ct = default) => throw new HttpRequestException("offline");
        public Task<IReadOnlyList<RecipeCollection>> GetCollectionsAsync(CancellationToken ct = default) => throw new HttpRequestException("offline");
        public Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default) => throw new HttpRequestException("offline");
    }
}
