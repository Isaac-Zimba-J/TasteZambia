using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

[Collection(nameof(DatabaseCollection))]
public class MeEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        _client = _factory.CreateClient();

        var tokens = await (await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest(
            $"device-{Guid.NewGuid():N}", new string('s', 40)))).Content.ReadFromJsonAsync<AuthTokensDto>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task WithoutAToken_MeIs401()
    {
        using var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(ApiRoutes.Me.Profile)).StatusCode);
    }

    [Fact]
    public async Task Profile_StartsEmptyAndRoundTrips()
    {
        var empty = await _client.GetFromJsonAsync<ProfileDto>(ApiRoutes.Me.Profile);
        Assert.Equal("", empty!.DisplayName);

        var put = await _client.PutAsJsonAsync(ApiRoutes.Me.Profile, new UpdateProfileRequest("Chanda Mwaba", "Kitwe, Copperbelt", "Bemba, English"));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var after = await _client.GetFromJsonAsync<ProfileDto>(ApiRoutes.Me.Profile);
        Assert.Equal("Chanda Mwaba", after!.DisplayName);
        Assert.Equal("Kitwe, Copperbelt", after.Location);
    }

    [Fact]
    public async Task Onboarding_DefaultsThenPersistsCompletion()
    {
        var before = await _client.GetFromJsonAsync<OnboardingChoicesDto>(ApiRoutes.Me.Onboarding);
        Assert.False(before!.IsComplete);
        Assert.Equal("English", before.Language);
        Assert.Equal(["traditional", "veg"], before.Tastes.OrderBy(t => t));

        var put = await _client.PutAsJsonAsync(ApiRoutes.Me.Onboarding,
            new OnboardingChoicesDto("Bemba", "I live abroad", ["quick", "family"], true, true, true));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var after = await _client.GetFromJsonAsync<OnboardingChoicesDto>(ApiRoutes.Me.Onboarding);
        Assert.True(after!.IsComplete);
        Assert.Equal("Bemba", after.Language);
        Assert.Equal(["family", "quick"], after.Tastes.OrderBy(t => t));
    }

    [Fact]
    public async Task Sync_ThenSavedAndProgressReadBack()
    {
        var now = DateTimeOffset.UtcNow;
        var sync = await _client.PostAsJsonAsync(ApiRoutes.Me.Sync, new SyncRequest(
        [
            new(SyncChangeDto.Saved, "ifisashi", null, true, now),
            new(SyncChangeDto.Saved, "delele", null, true, now),
            new(SyncChangeDto.Saved, "delele", null, false, now.AddSeconds(1)),
            new(SyncChangeDto.Progress, "ifisashi", 2, true, now),
        ]));
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);

        var saved = await _client.GetFromJsonAsync<List<SavedDishDto>>(ApiRoutes.Me.Saved);
        Assert.Equal(["ifisashi"], saved!.Select(s => s.DishId));   // delele was unsaved a second later

        var progress = await _client.GetFromJsonAsync<List<CookProgressDto>>(ApiRoutes.Me.Progress.Replace("{dishId}", "ifisashi"));
        Assert.Single(progress!, p => p.StepNumber == 2 && p.IsDone);
    }
}
