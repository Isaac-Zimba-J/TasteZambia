using System.Net;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;
using TasteZambia.Shared.Contracts.Me;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public List<string> Routes { get; } = [];
    public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoBackAsync() => Task.CompletedTask;
}

/// <summary>Records what it was asked to do, and can refuse the way an unreachable archive does.</summary>
file sealed class FakeAccountService(DeleteAccountResultDto? result = null, Exception? throws = null) : IAccountService
{
    public int Deletions { get; private set; }

    public Task<string> DeviceIdAsync(CancellationToken ct = default) => Task.FromResult("device-abc123");

    public Task<DeleteAccountResultDto> DeleteAsync(CancellationToken ct = default)
    {
        Deletions++;
        if (throws is not null) throw throws;
        return Task.FromResult(result ?? new DeleteAccountResultDto(0, 0, 0, 0, 0, 0));
    }
}

/// <summary>
/// There is nothing behind this action, so the screen has to be hard to use by accident and
/// honest about what it does not delete.
/// </summary>
public class DeleteAccountTests
{
    private static DeleteAccountViewModel Sut(IAccountService accounts, INavigationService? nav = null)
        => new(accounts, nav ?? new Nav());

    [Fact]
    public async Task ItShowsWhichAccountIsAboutToGo()
    {
        var vm = Sut(new FakeAccountService());
        await vm.InitializeAsync();

        Assert.Equal("device-abc123", vm.DeviceId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("delet")]
    [InlineData("yes")]
    [InlineData("DELETE MY ACCOUNT")]
    public void NothingShortOfTheWord_Arms(string typed)
    {
        var vm = Sut(new FakeAccountService());
        vm.Typed = typed;

        Assert.False(vm.CanDelete);
        Assert.False(vm.DeleteCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("DELETE")]
    [InlineData("delete")]
    [InlineData("  DELETE  ")]
    public void TheWord_Arms(string typed)
    {
        var vm = Sut(new FakeAccountService());
        vm.Typed = typed;

        Assert.True(vm.CanDelete);
        Assert.True(vm.DeleteCommand.CanExecute(null));
    }

    [Fact]
    public async Task TheCommandDoesNothing_UntilItIsArmed()
    {
        var accounts = new FakeAccountService();
        var vm = Sut(accounts);

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(0, accounts.Deletions);
        Assert.False(vm.IsDone);
    }

    [Fact]
    public async Task OnceItHasHappened_TheScreenStopsOfferingToDoItAgain()
    {
        var vm = Sut(new FakeAccountService());
        vm.Typed = "DELETE";

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.True(vm.IsDone);
        Assert.False(vm.IsPending);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public async Task WhenTheArchiveCannotBeReached_NothingIsClaimedToHaveHappened()
    {
        // A reader who believed this had worked would stop expecting their recipes to exist.
        var vm = Sut(new FakeAccountService(throws: new HttpRequestException("offline")));
        vm.Typed = "DELETE";

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.False(vm.IsDone);
        Assert.Contains("nothing has been deleted", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task TheSummarySaysWhatStayedInTheArchive_NotJustWhatWent()
    {
        var vm = Sut(new FakeAccountService(new DeleteAccountResultDto(
            DraftsDeleted: 2, PublishedRecipesAnonymised: 1, FamilyRecipesDeleted: 3,
            FamilyRecipesLeft: 1, NotesAnonymised: 2, FilesDeleted: 9)));
        vm.Typed = "DELETE";

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Contains("2 recipes waiting for review were deleted", vm.Summary);
        Assert.Contains("3 family recipes you preserved were deleted", vm.Summary);
        Assert.Contains("removed from 1 family recipe", vm.Summary);

        // The part that is not what "delete everything" sounds like it would do.
        Assert.Contains("stays in the archive, with your name removed", vm.Summary);
        Assert.Contains("2 notes you added to other families' recipes stay with them", vm.Summary);
        Assert.Contains("this account are gone", vm.Summary);
    }

    [Fact]
    public async Task AReaderWhoHadContributedNothing_IsStillToldWhatWent()
    {
        var vm = Sut(new FakeAccountService());
        vm.Typed = "DELETE";

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(
            "Your profile, your saved recipes and this account are gone.",
            vm.Summary);
    }

    [Fact]
    public async Task TheOnlyWayOn_IsBackToTheStart()
    {
        // There is no account left to return to, so it cannot go back to Settings.
        var nav = new Nav();
        var vm = Sut(new FakeAccountService(), nav);
        vm.Typed = "DELETE";
        await vm.DeleteCommand.ExecuteAsync(null);

        await vm.FinishCommand.ExecuteAsync(null);

        Assert.Equal("//home", nav.Routes[^1]);
    }
}

/// <summary>
/// The order matters: the archive goes first, and the phone is wiped only once the archive has
/// confirmed. Wiping first would leave a reader signed out of an account that still exists,
/// holding the only copy of the secret that could have reached it.
/// </summary>
public class AccountServiceTests
{
    private sealed class Store : ISecureStore
    {
        public Dictionary<string, string> Values { get; } = new()
        {
            ["device.id"] = "device-abc123",
            ["device.secret"] = "a-secret",
        };

        public Task<string?> GetAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { Values[key] = value; return Task.CompletedTask; }
        public void Remove(string key) => Values.Remove(key);
    }

    private sealed class Handler(HttpStatusCode status, string body = "") : HttpMessageHandler
    {
        public HttpRequestMessage? Seen { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Seen = request;
            // Read it here: the body is not available once the message is disposed.
            if (request.Content is not null) Body = await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(body) };
        }

        public string Body { get; private set; } = "";
    }

    private static (AccountService Service, Store Secure, InMemoryLocalStore Local, Handler Http) Sut(
        HttpStatusCode status, string body = "{}")
    {
        var handler = new Handler(status, body);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://archive.test") };
        var secure = new Store();
        var local = new InMemoryLocalStore();
        local.Set("personal", new { saved = 1 });
        local.Set("drafts", new { count = 2 });
        local.Set("onboarding", new { done = true });

        return (new AccountService(client, new Fakes.FixedDeviceIdentity("device-abc123"), secure, local), secure, local, handler);
    }

    [Fact]
    public async Task ItConfirmsWithTheAccountsOwnDeviceId()
    {
        var (service, _, _, http) = Sut(HttpStatusCode.OK);

        await service.DeleteAsync();

        Assert.Equal(HttpMethod.Delete, http.Seen!.Method);
        Assert.Contains("device-abc123", http.Body);
    }

    [Fact]
    public async Task OnceTheArchiveHasConfirmed_ThePhoneKeepsNothing()
    {
        var (service, secure, local, _) = Sut(HttpStatusCode.OK);

        await service.DeleteAsync();

        Assert.Empty(secure.Values);                      // no secret, so no way back in
        Assert.Null(local.Get<object>("personal"));
        Assert.Null(local.Get<object>("drafts"));
        Assert.Null(local.Get<object>("onboarding"));
    }

    [Fact]
    public async Task WhenTheArchiveRefuses_ThePhoneKeepsEverything()
    {
        // Nothing was deleted there, so signing the reader out here would strand them.
        var (service, secure, local, _) = Sut(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.DeleteAsync());

        Assert.Equal(2, secure.Values.Count);
        Assert.NotNull(local.Get<object>("onboarding"));
    }

    [Fact]
    public async Task AnAccountAlreadyGone_StillLeavesThePhoneClean()
    {
        // A retry after a reply that never arrived. The end state is what was asked for.
        var (service, secure, local, _) = Sut(HttpStatusCode.Unauthorized);

        await service.DeleteAsync();

        Assert.Empty(secure.Values);
        Assert.Null(local.Get<object>("onboarding"));
    }
}
