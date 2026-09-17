using System.Net;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Contracts.Dishes;
using TasteZambia.Shared.Contracts.Review;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

[Collection(nameof(DatabaseCollection))]
public class ReviewEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _contributor = null!;
    private HttpClient _reviewer = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        (_contributor, _) = await _factory.SignedInClientAsync();
        (_reviewer, _) = await _factory.SignedInClientAsync(role: "reviewer");
    }

    public async Task DisposeAsync()
    {
        _contributor.Dispose();
        _reviewer.Dispose();
        _factory.Dispose();
        await fixture.RemoveCommunityDishesAsync();
    }

    private static string Route(string template, Guid id) => template.Replace("{id}", id.ToString());

    private async Task<ContributionDetailDto> SubmitAsync(string name = "Chibwabwa na Mbalala")
        => (await (await _contributor.PostAsJsonAsync(ApiRoutes.Me.Contributions, ContributionEndpointTests.Chibwabwa() with { LocalName = name }))
            .Content.ReadFromJsonAsync<ContributionDetailDto>())!;

    [Fact]
    public async Task AContributorCannotSeeTheQueue()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await _contributor.GetAsync(ApiRoutes.Review.Queue)).StatusCode);
    }

    [Fact]
    public async Task Queue_ListsInReviewOldestFirst()
    {
        var a = await SubmitAsync("A");
        var b = await SubmitAsync("B");

        var queue = await _reviewer.GetFromJsonAsync<List<ReviewQueueItemDto>>(ApiRoutes.Review.Queue);
        var ids = queue!.Select(q => q.Id).ToList();
        Assert.True(ids.IndexOf(a.Id) < ids.IndexOf(b.Id));
    }

    [Fact]
    public async Task RequestChanges_FlagsFieldsAndTheContributorSeesThem()
    {
        var c = await SubmitAsync();
        var response = await _reviewer.PostAsJsonAsync(Route(ApiRoutes.Review.RequestChanges, c.Id),
            new RequestChangesRequest("Two things I want to get right before it goes public.",
                [new("Local name", "Is this the Mungwi name?", "Chibwabwa na Mbalala")]));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var seen = await _contributor.GetFromJsonAsync<ContributionDetailDto>(Route(ApiRoutes.Me.ContributionById, c.Id));
        Assert.Equal(ContributionStatus.ChangesRequested, seen!.Status);
        Assert.Single(seen.Flags);
        Assert.Contains(seen.Events, e => e.Kind == ReviewEventKind.Read);   // first reviewer action marks it read
        Assert.Contains(seen.Events, e => e.Kind == ReviewEventKind.ChangesRequested && e.Note!.StartsWith("Two things"));
    }

    [Fact]
    public async Task Publish_PutsTheDishInTheArchive()
    {
        var c = await SubmitAsync();
        var response = await _reviewer.PostAsync(Route(ApiRoutes.Review.Publish, c.Id), null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PublishResultDto>();

        var dishes = await _contributor.GetFromJsonAsync<List<DishDto>>(ApiRoutes.Dishes.Collection);
        var published = Assert.Single(dishes!, d => d.Id == result!.DishId);
        Assert.Equal(Provenance.Community, published.Provenance);

        var recipe = await _contributor.GetAsync(ApiRoutes.Dishes.Recipe.Replace("{id}", result!.DishId));
        Assert.Equal(HttpStatusCode.OK, recipe.StatusCode);
    }

    [Fact]
    public async Task Publish_ChangesTheDishesETag()
    {
        var before = (await _contributor.GetAsync(ApiRoutes.Dishes.Collection)).Headers.ETag!.Tag;
        var c = await SubmitAsync();
        await _reviewer.PostAsync(Route(ApiRoutes.Review.Publish, c.Id), null);
        var after = (await _contributor.GetAsync(ApiRoutes.Dishes.Collection)).Headers.ETag!.Tag;
        Assert.NotEqual(before, after);
    }

    [Fact]
    public async Task Publish_OnAWithdrawnContribution_Is409()
    {
        var c = await SubmitAsync();
        await _contributor.PostAsync(Route(ApiRoutes.Me.Withdraw, c.Id), null);
        var response = await _reviewer.PostAsync(Route(ApiRoutes.Review.Publish, c.Id), null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanGrantReviewer_AndAContributorCannot()
    {
        var (admin, _) = await _factory.SignedInClientAsync(role: "admin");
        var (newbie, newbieName) = await _factory.SignedInClientAsync();
        var route = ApiRoutes.Admin.UserRoles.Replace("{userName}", newbieName);

        Assert.Equal(HttpStatusCode.Forbidden, (await newbie.PutAsJsonAsync(route, new[] { "reviewer" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync(route, new[] { "reviewer" })).StatusCode);

        // A fresh token carries the new role.
        var (promoted, _) = await _factory.SignedInClientAsync(existingDeviceId: newbieName);
        Assert.Equal(HttpStatusCode.OK, (await promoted.GetAsync(ApiRoutes.Review.Queue)).StatusCode);

        admin.Dispose();
        newbie.Dispose();
        promoted.Dispose();
    }
}
