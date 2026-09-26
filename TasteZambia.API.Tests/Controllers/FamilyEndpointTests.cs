using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Contracts.Media;
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

    private static string MediaRoute(Guid recipeId, Guid mediaId) => ApiRoutes.Family.Media
        .Replace("{id}", recipeId.ToString()).Replace("{mediaId}", mediaId.ToString());

    private static async Task<UploadResultDto> UploadPhotoAsync(HttpClient client)
    {
        var file = new ByteArrayContent([1, 2, 3]);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        var content = new MultipartFormDataContent
        {
            { file, "file", "photo.png" },
            { new StringContent(MediaKind.Photo.ToString()), "kind" },
        };
        var response = await client.PostAsync(ApiRoutes.Media.Collection, content);
        return (await response.Content.ReadFromJsonAsync<UploadResultDto>())!;
    }

    /// <summary>Invites and joins _relative to the given recipe, returning the invite (for its MemberId).</summary>
    private async Task<InviteDto> JoinRelativeAsync(Guid recipeId)
    {
        var invite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipeId),
            new AddMemberRequest("Mutinta", "Sister"))).Content.ReadFromJsonAsync<InviteDto>();
        await _relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite!.Code));
        return invite;
    }

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

    [Fact]
    public async Task SomeoneAlreadyJoined_CannotRedeemASecondCodeIntoTheSameFamily()
    {
        var recipe = await PreserveAsync();
        await JoinRelativeAsync(recipe.Id);   // _relative is already Joined

        var secondInvite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipe.Id),
            new AddMemberRequest("Mutinta Again", "Sister"))).Content.ReadFromJsonAsync<InviteDto>();

        var response = await _relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(secondInvite!.Code));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var seen = await _owner.GetFromJsonAsync<FamilyRecipeDto>(Route(ApiRoutes.Family.ById, recipe.Id));
        Assert.Single(seen!.Members, m => m.DisplayName == "Mutinta" && m.State == MemberState.Joined);
        Assert.Single(seen.Members, m => m.DisplayName == "Mutinta Again" && m.State == MemberState.Invited);   // never redeemed
    }

    [Fact]
    public async Task AcceptingAnInvite_KeepsTheOwnersChosenDisplayName()
    {
        // Give _relative's own profile a name, so an overwrite-with-the-joiner's-own-name
        // bug would actually show up rather than silently no-op against an empty string.
        await _relative.PutAsJsonAsync(ApiRoutes.Me.Profile, new UpdateProfileRequest("Mutinta as she calls herself", "", ""));

        var recipe = await PreserveAsync();
        var invite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipe.Id),
            new AddMemberRequest("Mutinta Mwaba", "Sister"))).Content.ReadFromJsonAsync<InviteDto>();

        await _relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite!.Code));

        var seen = await _owner.GetFromJsonAsync<FamilyRecipeDto>(Route(ApiRoutes.Family.ById, recipe.Id));
        // Not overwritten with whatever the joining account's own profile name happens to be.
        Assert.Contains(seen!.Members, m => m.DisplayName == "Mutinta Mwaba" && m.State == MemberState.Joined);
    }

    [Fact]
    public async Task RedeemingTheSameCode_Concurrently_OnlyOneWins()
    {
        var recipe = await PreserveAsync();
        var invite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipe.Id),
            new AddMemberRequest("Mutinta", "Sister"))).Content.ReadFromJsonAsync<InviteDto>();

        var (clientA, _) = await _factory.SignedInClientAsync();
        var (clientB, _) = await _factory.SignedInClientAsync();
        using (clientA)
        using (clientB)
        {
            var raceA = clientA.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite!.Code));
            var raceB = clientB.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite.Code));
            var results = await Task.WhenAll(raceA, raceB);

            var statuses = results.Select(r => r.StatusCode).OrderBy(s => s).ToList();
            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Conflict], statuses);
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TasteZambiaDbContext>();
        var joined = await db.Set<FamilyMember>().CountAsync(m => m.Id == invite!.MemberId && m.State == MemberState.Joined);
        Assert.Equal(1, joined);
    }

    [Fact]
    public async Task AttachingYourOwnUpload_AddsItToTheRecipesMedia()
    {
        var recipe = await PreserveAsync();
        var upload = await UploadPhotoAsync(_owner);

        var response = await _owner.PutAsync(MediaRoute(recipe.Id, upload.Id), null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var seen = await _owner.GetFromJsonAsync<FamilyRecipeDto>(Route(ApiRoutes.Family.ById, recipe.Id));
        Assert.Contains(seen!.Media, m => m.Id == upload.Id);
    }

    [Fact]
    public async Task AMemberWithReadAccess_MayAttachMediaToARecipeTheyDidNotCreate()
    {
        var recipe = await PreserveAsync();
        await JoinRelativeAsync(recipe.Id);

        var upload = await UploadPhotoAsync(_relative);
        var response = await _relative.PutAsync(MediaRoute(recipe.Id, upload.Id), null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var seen = await _relative.GetFromJsonAsync<FamilyRecipeDto>(Route(ApiRoutes.Family.ById, recipe.Id));
        Assert.Contains(seen!.Media, m => m.Id == upload.Id);
    }

    [Fact]
    public async Task AttachingSomeoneElsesMedia_Fails()
    {
        var recipe = await PreserveAsync();
        var strangersUpload = await UploadPhotoAsync(_relative);   // not the owner's own upload

        var response = await _owner.PutAsync(MediaRoute(recipe.Id, strangersUpload.Id), null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AttachingToARecipeYouCannotSee_Is404()
    {
        var recipe = await PreserveAsync();               // SharedWithFamily, _relative never invited
        var upload = await UploadPhotoAsync(_relative);    // a perfectly valid upload of their own

        var response = await _relative.PutAsync(MediaRoute(recipe.Id, upload.Id), null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AStranger_CannotAttachMediaToAPublicRecipe_EvenWithTheirOwnUpload()
    {
        var recipe = await PreserveAsync();
        await _owner.PutAsJsonAsync(Route(ApiRoutes.Family.Privacy, recipe.Id), new SetPrivacyRequest(PrivacyLevel.PublicInArchive));

        // The recipe is readable by anyone now, but "readable" is not "belongs to this family".
        var strangersOwnUpload = await UploadPhotoAsync(_relative);
        var response = await _relative.PutAsync(MediaRoute(recipe.Id, strangersOwnUpload.Id), null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var seen = await _owner.GetFromJsonAsync<FamilyRecipeDto>(Route(ApiRoutes.Family.ById, recipe.Id));
        Assert.Empty(seen!.Media);
    }

    [Fact]
    public async Task AStranger_CannotPostANoteOnAPublicRecipe()
    {
        var recipe = await PreserveAsync();
        await _owner.PutAsJsonAsync(Route(ApiRoutes.Family.Privacy, recipe.Id), new SetPrivacyRequest(PrivacyLevel.PublicInArchive));

        var response = await _relative.PostAsJsonAsync(Route(ApiRoutes.Family.Notes, recipe.Id),
            new AddNoteRequest("I saw this recipe and have thoughts."));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TheOwner_CanUpdateTheRecipe()
    {
        var recipe = await PreserveAsync();
        var edited = Ifisashi() with { LocalName = "Ifisashi ya Banakulu, revisited" };
        var request = new UpdateFamilyRecipeRequest(edited.LocalName, edited.Description, edited.Province, edited.Language,
            edited.TaughtBy, edited.TaughtByOrigin, edited.Story, edited.TraditionalMethod);

        var response = await _owner.PutAsJsonAsync(Route(ApiRoutes.Family.ById, recipe.Id), request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<FamilyRecipeDto>();
        Assert.Equal("Ifisashi ya Banakulu, revisited", updated!.LocalName);
    }

    [Fact]
    public async Task AJoinedMember_CannotUpdateTheRecipe_404NotForbidden()
    {
        var recipe = await PreserveAsync();
        await JoinRelativeAsync(recipe.Id);

        var request = new UpdateFamilyRecipeRequest("Hijacked", "", "", "", "", "", "", "");
        var response = await _relative.PutAsJsonAsync(Route(ApiRoutes.Family.ById, recipe.Id), request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);   // not 403, and not a silent success

        var stillOriginal = await _owner.GetFromJsonAsync<FamilyRecipeDto>(Route(ApiRoutes.Family.ById, recipe.Id));
        Assert.Equal("Ifisashi ya Banakulu", stillOriginal!.LocalName);
    }
}
