using System.Net;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

[Collection(nameof(DatabaseCollection))]
public class ContributionEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        (_client, _) = await _factory.SignedInClientAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    public static SubmitContributionRequest Chibwabwa() => new(
        "Chibwabwa na Mbalala", "Pumpkin leaves cooked with pounded groundnuts and nothing else", "Northern", "Relish", "",
        [new("chibwabwa", "Chibwabwa", "Pumpkin leaves", "2 bundles")],
        ["Shred the leaves fine and rinse them twice.", "Pound the groundnuts until the oil starts to show."],
        "Cooked in Mungwi.", "The relish cooked when there is no money for meat.", "Clay pot on charcoal.", "", "", true, []);

    private static string Route(string template, Guid id) => template.Replace("{id}", id.ToString());

    [Fact]
    public async Task Submit_Returns201WithTheDetail()
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var detail = await response.Content.ReadFromJsonAsync<ContributionDetailDto>();
        Assert.Equal(ContributionStatus.InReview, detail!.Status);
        Assert.Single(detail.Events, e => e.Kind == ReviewEventKind.Submitted);
        Assert.EndsWith($"/me/contributions/{detail.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Submit_WithoutSteps_Is400()
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa() with { Steps = [] });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_IsNewestFirstAndOnlyMine()
    {
        await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa() with { LocalName = "First" });
        await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa() with { LocalName = "Second" });
        var (other, _) = await _factory.SignedInClientAsync();
        using (other)
            await other.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa() with { LocalName = "Not mine" });

        var mine = await _client.GetFromJsonAsync<List<ContributionSummaryDto>>(ApiRoutes.Me.Contributions);
        Assert.Equal(["Second", "First"], mine!.Select(c => c.LocalName));
    }

    [Fact]
    public async Task SomeoneElsesContribution_Is404NotForbidden()
    {
        var created = await (await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa())).Content.ReadFromJsonAsync<ContributionDetailDto>();
        var (other, _) = await _factory.SignedInClientAsync();
        using (other)
        {
            var response = await other.GetAsync(Route(ApiRoutes.Me.ContributionById, created!.Id));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task Withdraw_ThenWithdrawAgain_Is409ProblemDetails()
    {
        var created = await (await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa())).Content.ReadFromJsonAsync<ContributionDetailDto>();
        var route = Route(ApiRoutes.Me.Withdraw, created!.Id);

        var first = await _client.PostAsync(route, null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(ContributionStatus.Withdrawn, (await first.Content.ReadFromJsonAsync<ContributionDetailDto>())!.Status);

        var second = await _client.PostAsync(route, null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Resubmit_WhenNothingWasRequested_Is409()
    {
        var created = await (await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa())).Content.ReadFromJsonAsync<ContributionDetailDto>();
        var response = await _client.PostAsJsonAsync(Route(ApiRoutes.Me.Resubmit, created!.Id), new ResubmitRequest([]));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
