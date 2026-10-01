using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Contracts.Review;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

/// <summary>
/// ReviewEvents exist so a contributor can see what happened to their recipe, and their actor
/// is a display name the reviewer can change - which makes them a story, not a record. These
/// hold the separate, append-only log that keeps the account id.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class AuditLogTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task PublishingARecipe_IsRecordedAgainstTheReviewersAccount()
    {
        await using var api = new ApiFactory(fixture.ConnectionString);
        await api.SeedAsync();
        var (contributor, _) = await api.SignedInClientAsync();
        var submitted = await (await contributor.PostAsJsonAsync(
                ApiRoutes.Me.Contributions, ContributionEndpointTests.Chibwabwa() with { LocalName = "Audited Ifisashi" }))
            .Content.ReadFromJsonAsync<ContributionDetailDto>();

        var (reviewer, reviewerDevice) = await api.SignedInClientAsync("reviewer");
        await reviewer.PostAsync(ApiRoutes.Review.Publish.Replace("{id}", submitted!.Id.ToString()), null);

        await using var db = fixture.NewContext();
        var entry = await db.AuditEntries
            .Where(e => e.Subject == submitted.Id.ToString() && e.Action == AuditActions.ReviewPublish)
            .OrderByDescending(e => e.At)
            .FirstOrDefaultAsync();

        Assert.NotNull(entry);

        // The account, not only the name - the name is theirs to change.
        var reviewerId = (await db.Users.FirstAsync(u => u.UserName == reviewerDevice)).Id;
        Assert.Equal(reviewerId, entry!.ActorUserId);
        Assert.NotEqual(default, entry.At);
        Assert.Contains("published as dish", entry.Detail);

        await fixture.RemoveCommunityDishesAsync();
    }

    [Fact]
    public async Task RequestingChanges_IsRecordedWithHowManyFieldsWereFlagged()
    {
        await using var api = new ApiFactory(fixture.ConnectionString);
        await api.SeedAsync();
        var (contributor, _) = await api.SignedInClientAsync();
        var submitted = await (await contributor.PostAsJsonAsync(
                ApiRoutes.Me.Contributions, ContributionEndpointTests.Chibwabwa() with { LocalName = "Audited Chikanda" }))
            .Content.ReadFromJsonAsync<ContributionDetailDto>();

        var (reviewer, _) = await api.SignedInClientAsync("reviewer");
        await reviewer.PostAsJsonAsync(
            ApiRoutes.Review.RequestChanges.Replace("{id}", submitted!.Id.ToString()),
            new RequestChangesRequest("Two things to check.",
            [
                new FlagRequestDto("Origin", "Which district?", ""),
                new FlagRequestDto("TaughtBy", "Who taught you this?", ""),
            ]));

        await using var db = fixture.NewContext();
        var entry = await db.AuditEntries
            .FirstOrDefaultAsync(e => e.Subject == submitted.Id.ToString() && e.Action == AuditActions.ReviewRequestChanges);

        Assert.NotNull(entry);
        Assert.Contains("2 field", entry!.Detail);
    }

    [Fact]
    public async Task GrantingARole_RecordsWhatItWasBeforeAndAfter()
    {
        await using var api = new ApiFactory(fixture.ConnectionString);
        await api.SeedAsync();
        var (_, subjectDevice) = await api.SignedInClientAsync();
        var (admin, _) = await api.SignedInClientAsync("admin");

        await admin.PutAsJsonAsync(
            ApiRoutes.Admin.UserRoles.Replace("{userName}", subjectDevice),
            new[] { "contributor", "reviewer" });

        await using var db = fixture.NewContext();
        var entry = await db.AuditEntries
            .FirstOrDefaultAsync(e => e.Subject == subjectDevice && e.Action == AuditActions.AdminSetRoles);

        Assert.NotNull(entry);

        // The most consequential thing anyone can do here, so before as well as after.
        Assert.Contains("contributor", entry!.Detail);
        Assert.Contains("reviewer", entry.Detail);
        Assert.Contains("->", entry.Detail);
    }

    [Fact]
    public async Task AReaderJustReadingTheArchive_WritesNothingToTheLog()
    {
        await using var api = new ApiFactory(fixture.ConnectionString);
        await api.SeedAsync();
        var (client, _) = await api.SignedInClientAsync();

        await using var before = fixture.NewContext();
        var countBefore = await before.AuditEntries.CountAsync();

        await client.GetAsync(ApiRoutes.Dishes.Collection);
        await client.GetAsync(ApiRoutes.Me.Profile);

        await using var after = fixture.NewContext();
        Assert.Equal(countBefore, await after.AuditEntries.CountAsync());
    }
}
