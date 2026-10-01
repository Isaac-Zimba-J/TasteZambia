using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

/// <summary>
/// Deleting an account cannot be undone, which is exactly why it is worth holding in place:
/// that it takes everything it promises, that it does not take what belongs to other people,
/// and that it cannot happen by accident.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class AccountDeletionTests(DatabaseFixture fixture)
{
    private static SubmitContributionRequest ARecipe(string name)
        => ContributionEndpointTests.Chibwabwa() with { LocalName = name };

    private static CreateFamilyRecipeRequest AFamilyRecipe(string name) => new(
        name, "As she made it", "Northern", "Bemba", "Banakulu Mwansa", "Kasama",
        "Cooked at every funeral gathering", "Pounded in a wooden mortar",
        PrivacyLevel.PrivateToMe);

    [Fact]
    public async Task DeletingAnAccount_TakesTheProfileTheListsAndTheDraftsWithIt()
    {
        await using var api = new ApiFactory(fixture.ConnectionString);
        await api.SeedAsync();
        var (client, deviceId) = await api.SignedInClientAsync();

        await client.PutAsJsonAsync(ApiRoutes.Me.Profile, new UpdateProfileRequest("Mwansa Chanda", "Kasama", "Bemba"));
        await client.PostAsJsonAsync(ApiRoutes.Me.Contributions, ARecipe("Ifisashi ya Mwansa"));

        var response = await client.SendAsync(Delete(deviceId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DeleteAccountResultDto>();
        Assert.Equal(1, result!.DraftsDeleted);

        await using var db = fixture.NewContext();
        var userId = await UserIdOrNullAsync(db, deviceId);
        Assert.Null(userId);   // the account itself is gone, not just emptied
    }

    [Fact]
    public async Task TheTokenItWasDeletedWith_StopsWorking()
    {
        await using var api = new ApiFactory(fixture.ConnectionString);
        await api.SeedAsync();
        var (client, deviceId) = await api.SignedInClientAsync();

        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Delete(deviceId))).StatusCode);

        // The bearer token is still signed and unexpired, but the account behind it is not
        // there - so anything that reads the account's own data must not answer.
        var after = await client.GetAsync(ApiRoutes.Me.Contributions);
        Assert.NotEqual(HttpStatusCode.OK, after.StatusCode);
    }

    [Fact]
    public async Task APublishedRecipe_StaysInTheArchiveWithTheCreditWithdrawn()
    {
        await using var api = new ApiFactory(fixture.ConnectionString);
        await api.SeedAsync();
        var (author, authorDevice) = await api.SignedInClientAsync();
        await author.PutAsJsonAsync(ApiRoutes.Me.Profile, new UpdateProfileRequest("Mwansa Chanda", "Kasama", "Bemba"));

        var submitted = await (await author.PostAsJsonAsync(ApiRoutes.Me.Contributions, ARecipe("Chikanda ya Mwansa")))
            .Content.ReadFromJsonAsync<ContributionDetailDto>();

        var (reviewer, _) = await api.SignedInClientAsync("reviewer");
        Assert.Equal(HttpStatusCode.OK, (await reviewer.PostAsync(
            ApiRoutes.Review.Publish.Replace("{id}", submitted!.Id.ToString()), null)).StatusCode);

        await using (var check = fixture.NewContext())
            Assert.NotNull(await check.Dishes.FirstOrDefaultAsync(d => d.LocalName == "Chikanda ya Mwansa"));

        Assert.Equal(HttpStatusCode.OK, (await author.SendAsync(Delete(authorDevice))).StatusCode);

        await using var db = fixture.NewContext();
        var dish = await db.Dishes.FirstOrDefaultAsync(d => d.LocalName == "Chikanda ya Mwansa");
        Assert.NotNull(dish);   // other readers have this saved; it is the archive's now

        var recipe = await db.Recipes.FirstAsync(r => r.DishId == dish!.Id);
        Assert.Equal(AccountDeletionService.AnonymousCredit, recipe.ContributorName);
        Assert.Equal("", recipe.ContributorLocation);
        Assert.DoesNotContain("Mwansa Chanda", recipe.ContributorName);

        // The recipe did come from a contributor; saying otherwise would falsify the record.
        Assert.Equal(Provenance.Community, dish!.Provenance);

        // The whole suite shares this archive, and later tests count the eight seeded dishes.
        await fixture.RemoveCommunityDishesAsync();
    }

    [Fact]
    public async Task AFamilyRecipeTheReaderPreserved_IsDeletedWithIt()
    {
        await using var api = new ApiFactory(fixture.ConnectionString);
        await api.SeedAsync();
        var (owner, ownerDevice) = await api.SignedInClientAsync();

        var recipe = await (await owner.PostAsJsonAsync(ApiRoutes.Family.Collection, AFamilyRecipe("Ifisashi ya Banakulu")))
            .Content.ReadFromJsonAsync<FamilyRecipeDto>();

        await owner.SendAsync(Delete(ownerDevice));

        await using var db = fixture.NewContext();
        Assert.Null(await db.FamilyRecipes.FirstOrDefaultAsync(r => r.Id == recipe!.Id));
    }

    [Fact]
    public async Task ANoteLeftOnSomebodyElsesRecipe_StaysWithThatFamily_Anonymised()
    {
        await using var api = new ApiFactory(fixture.ConnectionString);
        await api.SeedAsync();
        var (owner, _) = await api.SignedInClientAsync();

        var recipe = await (await owner.PostAsJsonAsync(ApiRoutes.Family.Collection, AFamilyRecipe("Chikanda ya Banakulu")))
            .Content.ReadFromJsonAsync<FamilyRecipeDto>();

        var invite = await (await owner.PostAsJsonAsync(
            ApiRoutes.Family.Members.Replace("{id}", recipe!.Id.ToString()),
            new AddMemberRequest("Mutinta Mwaba", "Sister, Lusaka")))
            .Content.ReadFromJsonAsync<InviteDto>();

        var (relative, relativeDevice) = await api.SignedInClientAsync();
        await relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite!.Code));
        await relative.PostAsJsonAsync(
            ApiRoutes.Family.Notes.Replace("{id}", recipe.Id.ToString()),
            new AddNoteRequest("Mama added bicarbonate to the chikanda."));

        await relative.SendAsync(Delete(relativeDevice));

        await using var db = fixture.NewContext();

        // The family keeps what was written for them - it was never only the writer's.
        var note = await db.FamilyNotes.FirstOrDefaultAsync(n => n.FamilyRecipeId == recipe.Id);
        Assert.NotNull(note);
        Assert.Equal("Mama added bicarbonate to the chikanda.", note!.Body);
        Assert.Equal(AccountDeletionService.AnonymousCredit, note.AuthorName);
        Assert.Equal("", note.AuthorUserId);

        // But they are no longer a member, so they could not open it again. The owner's own
        // row is still there - it is theirs, and they are not the one leaving.
        var them = await db.FamilyMembers.FirstAsync(m => m.FamilyRecipeId == recipe.Id && m.DisplayName == "Mutinta Mwaba");
        Assert.Equal(MemberState.Removed, them.State);
        Assert.Null(them.UserId);   // nothing links the family's record to the deleted account

        // And the recipe itself is untouched.
        Assert.NotNull(await db.FamilyRecipes.FirstOrDefaultAsync(r => r.Id == recipe.Id));
    }

    [Fact]
    public async Task AWrongConfirmation_DeletesNothing()
    {
        await using var api = new ApiFactory(fixture.ConnectionString);
        await api.SeedAsync();
        var (client, deviceId) = await api.SignedInClientAsync();

        var response = await client.SendAsync(Delete("device-somebody-elses-id"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var db = fixture.NewContext();
        Assert.NotNull(await UserIdOrNullAsync(db, deviceId));   // still there
    }

    [Fact]
    public async Task DeletingWithoutSigningIn_IsRefused()
    {
        await using var api = new ApiFactory(fixture.ConnectionString);
        await api.SeedAsync();

        var response = await api.CreateClient().SendAsync(Delete("device-anything"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpRequestMessage Delete(string confirmDeviceId) => new(HttpMethod.Delete, ApiRoutes.Me.Account)
    {
        Content = JsonContent.Create(new DeleteAccountRequest(confirmDeviceId)),
    };

    private static async Task<string?> UserIdOrNullAsync(TasteZambia.API.Data.TasteZambiaDbContext db, string deviceId)
        => (await db.Users.FirstOrDefaultAsync(u => u.UserName == deviceId))?.Id;
}
