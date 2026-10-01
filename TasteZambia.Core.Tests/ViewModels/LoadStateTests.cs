using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.Tests.Fakes;
using TasteZambia.Core.ViewModels;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public List<string> Routes { get; } = [];
    public bool WentBack { get; private set; }
    public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoBackAsync() { WentBack = true; return Task.CompletedTask; }
}

/// <summary>Fails on demand, so a screen's offline state can be driven from a test.</summary>
internal sealed class SwitchableDishRepository : IDishRepository
{
    private readonly InMemoryDishRepository _real = new();

    public bool IsOffline { get; set; }

    public Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default)
        => IsOffline ? throw new HttpRequestException("offline") : _real.GetAllAsync(ct);

    public Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default)
        => IsOffline ? throw new HttpRequestException("offline") : _real.GetByIdAsync(id, ct);

    public Task<RecipeDetail?> GetRecipeAsync(string dishId, CancellationToken ct = default)
        => IsOffline ? throw new HttpRequestException("offline") : _real.GetRecipeAsync(dishId, ct);
}

public class LoadStateTests
{
    private static HomeViewModel Home(SwitchableDishRepository dishes)
        => TestServices.Home(new Nav(), dishes);

    [Fact]
    public async Task ASuccessfulLoad_ClearsTheSpinnerAndReportsNoError()
    {
        var vm = Home(new SwitchableDishRepository());

        Assert.True(await vm.LoadAsync());

        Assert.False(vm.IsLoading);
        Assert.False(vm.IsFirstLoad);
        Assert.False(vm.HasLoadError);
        Assert.Equal("", vm.LoadError);
    }

    [Fact]
    public async Task AnOfflineLoad_SaysSoAndDoesNotCountAsLoaded()
    {
        var vm = Home(new SwitchableDishRepository { IsOffline = true });

        Assert.False(await vm.LoadAsync());

        Assert.False(vm.IsLoading);
        Assert.True(vm.HasLoadError);
        Assert.Equal(BaseViewModel.OfflineMessage, vm.LoadError);
        Assert.Empty(vm.Dishes);
    }

    [Fact]
    public async Task Retrying_AfterTheArchiveComesBack_LoadsAndClearsTheError()
    {
        var dishes = new SwitchableDishRepository { IsOffline = true };
        var vm = Home(dishes);
        await vm.LoadAsync();
        Assert.True(vm.HasLoadError);

        dishes.IsOffline = false;
        await vm.RefreshCommand.ExecuteAsync(null);   // the retry button runs the same command

        Assert.False(vm.HasLoadError);
        Assert.NotEmpty(vm.Dishes);
        Assert.False(vm.IsRefreshing);
    }

    [Fact]
    public async Task AFailedRefresh_KeepsWhatIsAlreadyOnScreenVisible()
    {
        var dishes = new SwitchableDishRepository();
        var vm = Home(dishes);
        await vm.LoadAsync();

        dishes.IsOffline = true;
        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.True(vm.HasLoadError);
        // Nothing is shown in place of content the reader can already see: the spinner
        // overlay is for a screen with nothing on it.
        Assert.False(vm.IsFirstLoad);
    }

    [Fact]
    public async Task Withdraw_WhenTheArchiveIsUnreachable_SaysSoInsteadOfLeavingTheScreen()
    {
        var fake = new FailingContributionService();
        var nav = new Nav();
        var vm = new ShareReviewViewModel(fake, nav) { Id = fake.KnownId };
        await vm.LoadAsync();

        await vm.WithdrawCommand.ExecuteAsync(null);

        Assert.Equal(BaseViewModel.OfflineMessage, vm.ActionError);
        Assert.False(nav.WentBack);
    }

    /// <summary>Reads fine, but every write fails as it would with no signal.</summary>
    private sealed class FailingContributionService : FakeContributionService
    {
        public Guid KnownId { get; }

        public FailingContributionService()
            => KnownId = Add(ShareLifecycleTests.Detail(ContributionStatus.InReview, [ShareLifecycleTests.Submitted()]));

        public override Task<ContributionDetailDto> WithdrawAsync(Guid id, CancellationToken ct = default)
            => throw new HttpRequestException("offline");
    }

    [Fact]
    public async Task BeingRateLimited_IsNotReportedAsBeingOffline()
    {
        // The archive answered. Telling this reader to check their connection would send them
        // to fix something that is not broken.
        var vm = TestServices.Home(new Nav(), new RefusingDishRepository(System.Net.HttpStatusCode.TooManyRequests));

        Assert.False(await vm.LoadAsync());

        Assert.Equal(LoadFailure.TooManyRequests, vm.Failure);
        Assert.Equal(BaseViewModel.TooManyRequestsMessage, vm.LoadError);
        Assert.DoesNotContain("connection", vm.LoadError);
    }

    [Fact]
    public async Task ARealNetworkFailure_IsStillReportedAsBeingOffline()
    {
        var dishes = new SwitchableDishRepository { IsOffline = true };
        var vm = TestServices.Home(new Nav(), dishes);

        Assert.False(await vm.LoadAsync());

        Assert.Equal(LoadFailure.Offline, vm.Failure);
        Assert.Equal(BaseViewModel.OfflineMessage, vm.LoadError);
    }

    [Fact]
    public async Task ASuccessfulReload_ClearsTheFailureKindToo()
    {
        var dishes = new SwitchableDishRepository { IsOffline = true };
        var vm = TestServices.Home(new Nav(), dishes);
        await vm.LoadAsync();
        Assert.Equal(LoadFailure.Offline, vm.Failure);

        dishes.IsOffline = false;
        Assert.True(await vm.LoadAsync());

        Assert.Equal(LoadFailure.None, vm.Failure);   // or the screen keeps drawing the emblem
        Assert.Empty(vm.LoadError);
    }

    /// <summary>Answers every read with one chosen status, the way the real HTTP layer would.</summary>
    private sealed class RefusingDishRepository(System.Net.HttpStatusCode status) : IDishRepository
    {
        private HttpRequestException Refuse() => new("refused", null, status);

        public Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default) => throw Refuse();
        public Task<IReadOnlyList<Dish>> SearchAsync(string q, string c, CancellationToken ct = default) => throw Refuse();
        public Task<IReadOnlyList<Dish>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default) => throw Refuse();
        public Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default) => throw Refuse();
        public Task<RecipeDetail?> GetRecipeAsync(string dishId, CancellationToken ct = default) => throw Refuse();
    }
}
