using System.Net;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

[Collection(nameof(DatabaseCollection))]
public class FamilyEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _owner = null!;
    private HttpClient _relative = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        (_owner, _) = await _factory.SignedInClientAsync();
        (_relative, _) = await _factory.SignedInClientAsync();
    }

    public Task DisposeAsync() { _owner.Dispose(); _relative.Dispose(); _factory.Dispose(); return Task.CompletedTask; }

    private static string Route(string template, Guid id) => template.Replace("{id}", id.ToString());

    private static CreateFamilyRecipeRequest Ifisashi() => new(
        "Ifisashi ya Banakulu", "Pumpkin leaves in groundnuts, the way my grandmother made it",
        "Northern", "Bemba", "Banakulu Mwaba, my father's mother",
        "Mungwi, outside Kasama. She was taught by her own mother.",
        "She cooked this every time we arrived from Kitwe, before we had even put our bags down.",
        "Clay pot on the mbaula. Groundnuts pounded, never blended.",
        PrivacyLevel.SharedWithFamily);

    private async Task<FamilyRecipeDto> PreserveAsync()
        => (await (await _owner.PostAsJsonAsync(ApiRoutes.Family.Collection, Ifisashi()))
            .Content.ReadFromJsonAsync<FamilyRecipeDto>())!;

    [Fact]
    public async Task Preserving_ReturnsTheRecipeWithTheOwnerAsTheOnlyMember()
    {
        var recipe = await PreserveAsync();

        Assert.Equal("Ifisashi ya Banakulu", recipe.LocalName);
        Assert.True(recipe.IsOwner);
        Assert.Equal(PrivacyLevel.SharedWithFamily, recipe.Privacy);
        Assert.Equal(100, recipe.PercentComplete);   // name, story, method and privacy all present
        Assert.Empty(recipe.Media);
    }

    [Fact]
    public async Task ADraftMissingItsStoryAndMethod_ReportsPartialCompletion()
    {
        var sparse = Ifisashi() with { Story = "", TraditionalMethod = "", Privacy = PrivacyLevel.PrivateToMe };
        var recipe = await (await _owner.PostAsJsonAsync(ApiRoutes.Family.Collection, sparse))
            .Content.ReadFromJsonAsync<FamilyRecipeDto>();

        Assert.Equal(50, recipe!.PercentComplete);   // name+province and taught-by only
    }

    [Fact]
    public async Task AStranger_CannotSeeIt()
    {
        var recipe = await PreserveAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await _relative.GetAsync(Route(ApiRoutes.Family.ById, recipe.Id))).StatusCode);
    }

    [Fact]
    public async Task Inviting_ThenAccepting_LetsARelativeReadItAndAddANote()
    {
        var recipe = await PreserveAsync();

        var invite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipe.Id),
            new AddMemberRequest("Mutinta Mwaba", "Sister, Lusaka"))).Content.ReadFromJsonAsync<InviteDto>();
        Assert.Equal(8, invite!.Code.Length);

        var accepted = await _relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite.Code));
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        var seen = await _relative.GetFromJsonAsync<FamilyRecipeDto>(Route(ApiRoutes.Family.ById, recipe.Id));
        Assert.False(seen!.IsOwner);
        Assert.Contains(seen.Members, m => m.DisplayName == "Mutinta Mwaba" && m.State == MemberState.Joined);

        var noted = await _relative.PostAsJsonAsync(Route(ApiRoutes.Family.Notes, recipe.Id),
            new AddNoteRequest("She never used tomato in this."));
        Assert.Equal(HttpStatusCode.Created, noted.StatusCode);

        var withNote = await _owner.GetFromJsonAsync<FamilyRecipeDto>(Route(ApiRoutes.Family.ById, recipe.Id));
        Assert.Single(withNote!.Notes, n => n.Body.StartsWith("She never used tomato"));
    }

    [Fact]
    public async Task AnInviteCode_CannotBeRedeemedTwice()
    {
        var recipe = await PreserveAsync();
        var invite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipe.Id),
            new AddMemberRequest("Mutinta", "Sister"))).Content.ReadFromJsonAsync<InviteDto>();

        await _relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite!.Code));

        var (third, _) = await _factory.SignedInClientAsync();
        using (third)
            Assert.Equal(HttpStatusCode.Conflict,
                (await third.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite.Code))).StatusCode);
    }

    [Fact]
    public async Task ARemovedMember_NoLongerSeesIt()
    {
        var recipe = await PreserveAsync();
        var invite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipe.Id),
            new AddMemberRequest("Mutinta", "Sister"))).Content.ReadFromJsonAsync<InviteDto>();
        await _relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite!.Code));
        Assert.Equal(HttpStatusCode.OK, (await _relative.GetAsync(Route(ApiRoutes.Family.ById, recipe.Id))).StatusCode);

        var removed = await _owner.DeleteAsync(ApiRoutes.Family.MemberById
            .Replace("{id}", recipe.Id.ToString()).Replace("{memberId}", invite.MemberId.ToString()));
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _relative.GetAsync(Route(ApiRoutes.Family.ById, recipe.Id))).StatusCode);
    }

    [Fact]
    public async Task OnlyTheOwner_ChangesPrivacy()
    {
        var recipe = await PreserveAsync();
        var invite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipe.Id),
            new AddMemberRequest("Mutinta", "Sister"))).Content.ReadFromJsonAsync<InviteDto>();
        await _relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite!.Code));

        var byMember = await _relative.PutAsJsonAsync(Route(ApiRoutes.Family.Privacy, recipe.Id),
            new SetPrivacyRequest(PrivacyLevel.PublicInArchive));
        Assert.Equal(HttpStatusCode.NotFound, byMember.StatusCode);   // not 403: do not confirm it exists to edit

        var byOwner = await _owner.PutAsJsonAsync(Route(ApiRoutes.Family.Privacy, recipe.Id),
            new SetPrivacyRequest(PrivacyLevel.PublicInArchive));
        Assert.Equal(HttpStatusCode.NoContent, byOwner.StatusCode);
    }

    [Fact]
    public async Task MyShelf_ListsWhatIKeepAndWhatIWasInvitedInto_ButNotStrangersPublicOnes()
    {
        var mine = await PreserveAsync();
        await _owner.PutAsJsonAsync(Route(ApiRoutes.Family.Privacy, mine.Id), new SetPrivacyRequest(PrivacyLevel.PublicInArchive));

        var shelf = await _relative.GetFromJsonAsync<List<FamilyRecipeSummaryDto>>(ApiRoutes.Family.Collection);
        Assert.Empty(shelf!);   // public, but not theirs to keep
    }
}
