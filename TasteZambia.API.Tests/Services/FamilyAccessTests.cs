using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Tests.Services;

[Collection(nameof(DatabaseCollection))]
public class FamilyAccessTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private async Task<(string owner, string member, string stranger)> UsersAsync()
    {
        await using var db = fixture.NewContext();
        var users = Enumerable.Range(0, 3).Select(_ => new ArchiveUser { UserName = $"device-{Guid.NewGuid():N}" }).ToList();
        db.Users.AddRange(users);
        await db.SaveChangesAsync();
        return (users[0].Id, users[1].Id, users[2].Id);
    }

    private async Task<Guid> PreserveAsync(string ownerId, PrivacyLevel privacy, (string userId, MemberState state)? member = null)
    {
        await using var db = fixture.NewContext();
        var recipe = new FamilyRecipe
        {
            OwnerId = ownerId,
            LocalName = "Ifisashi ya Banakulu",
            Description = "Pumpkin leaves in groundnuts, the way my grandmother made it",
            Province = "Northern",
            TaughtBy = "Banakulu Mwaba",
            Privacy = privacy,
            CreatedAt = T0,
            UpdatedAt = T0,
        };
        if (member is { } m)
            recipe.Members.Add(new FamilyMember { UserId = m.userId, DisplayName = "Mutinta", Relation = "Sister", State = m.state, InvitedAt = T0 });
        db.Set<FamilyRecipe>().Add(recipe);
        await db.SaveChangesAsync();
        return recipe.Id;
    }

    private FamilyRepository Repository() => new(fixture.NewContext(), new FamilyAccessService(fixture.NewContext()));

    [Fact]
    public async Task TheOwner_CanAlwaysRead()
    {
        var (owner, _, _) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.PrivateToMe);

        Assert.NotNull(await Repository().GetAsync(id, owner));
    }

    [Fact]
    public async Task AJoinedMember_CanRead_AndAStrangerCannot()
    {
        var (owner, member, stranger) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Joined));

        Assert.NotNull(await Repository().GetAsync(id, member));
        Assert.Null(await Repository().GetAsync(id, stranger));
    }

    [Fact]
    public async Task AnInvitedMemberWhoHasNotJoined_CannotReadYet()
    {
        var (owner, member, _) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Invited));

        Assert.Null(await Repository().GetAsync(id, member));
    }

    [Fact]
    public async Task ARemovedMember_LosesAccessImmediately()
    {
        var (owner, member, _) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Joined));
        Assert.NotNull(await Repository().GetAsync(id, member));

        await using (var db = fixture.NewContext())
        {
            var row = await db.Set<FamilyMember>().FirstAsync(m => m.FamilyRecipeId == id && m.UserId == member);
            row.State = MemberState.Removed;
            await db.SaveChangesAsync();
        }

        Assert.Null(await Repository().GetAsync(id, member));
    }

    [Fact]
    public async Task PublicInArchive_IsReadableByAnyone()
    {
        var (owner, _, stranger) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.PublicInArchive);

        Assert.NotNull(await Repository().GetAsync(id, stranger));
    }

    [Fact]
    public async Task PrivacyMovingBackToFamily_ShutsStrangersOutAgain()
    {
        var (owner, _, stranger) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.PublicInArchive);
        Assert.NotNull(await Repository().GetAsync(id, stranger));

        await using (var db = fixture.NewContext())
        {
            var row = await db.Set<FamilyRecipe>().FirstAsync(r => r.Id == id);
            row.Privacy = PrivacyLevel.SharedWithFamily;
            await db.SaveChangesAsync();
        }

        Assert.Null(await Repository().GetAsync(id, stranger));
    }

    // VisibleTo (LINQ-to-entities, used by the repository) and CanReadAsync (in-memory,
    // used once an object is already loaded) state the same rule twice. Nothing above
    // exercises CanReadAsync directly, so the two could silently drift apart - a member
    // getting a 404, or worse, a stranger getting in. This pins CanReadAsync to the same
    // cases, plus the PrivateToMe+joined-member case the rule allows but nothing asserted.
    [Theory]
    [InlineData(true, null, PrivacyLevel.PrivateToMe, true)] // the owner always reads their own, regardless of privacy
    [InlineData(false, MemberState.Joined, PrivacyLevel.SharedWithFamily, true)] // a joined member reads
    [InlineData(false, MemberState.Joined, PrivacyLevel.PrivateToMe, true)] // membership grants access even when Privacy says otherwise
    [InlineData(false, MemberState.Invited, PrivacyLevel.SharedWithFamily, false)] // not yet joined
    [InlineData(false, MemberState.Removed, PrivacyLevel.SharedWithFamily, false)] // removed loses access
    [InlineData(false, null, PrivacyLevel.SharedWithFamily, false)] // a stranger cannot read a family-only recipe
    [InlineData(false, null, PrivacyLevel.PublicInArchive, true)] // a stranger can read a public one
    public async Task CanReadAsync_AgreesWithVisibleTo(bool isOwner, MemberState? memberState, PrivacyLevel privacy, bool expected)
    {
        const string ownerId = "owner-id";
        const string memberId = "member-id";
        const string strangerId = "stranger-id";

        var recipe = new FamilyRecipe
        {
            OwnerId = ownerId,
            LocalName = "Ifisashi ya Banakulu",
            Privacy = privacy,
        };
        if (memberState is { } state)
            recipe.Members.Add(new FamilyMember { UserId = memberId, DisplayName = "Mutinta", State = state, InvitedAt = T0 });

        var userId = isOwner ? ownerId : memberState is null ? strangerId : memberId;
        var access = new FamilyAccessService(fixture.NewContext());

        Assert.Equal(expected, await access.CanReadAsync(recipe, userId, default));
    }

    [Fact]
    public async Task OnlyTheOwner_CanEdit()
    {
        var (owner, member, _) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Joined));

        await using var db = fixture.NewContext();
        var recipe = await db.Set<FamilyRecipe>().Include(r => r.Members).FirstAsync(r => r.Id == id);
        var access = new FamilyAccessService(fixture.NewContext());

        Assert.True(await access.CanEditAsync(recipe, owner, default));
        Assert.False(await access.CanEditAsync(recipe, member, default));
    }

    [Fact]
    public async Task ListForUser_ShowsOwnedAndJoined_ButNotStrangersPrivateOnes()
    {
        var (owner, member, stranger) = await UsersAsync();
        await PreserveAsync(owner, PrivacyLevel.PrivateToMe);
        await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Joined));

        Assert.Equal(2, (await Repository().ListForUserAsync(owner)).Count);
        Assert.Single(await Repository().ListForUserAsync(member));
        Assert.Empty(await Repository().ListForUserAsync(stranger));
    }

    [Fact]
    public async Task AMembersMediaIsReadableByTheFamily_AndNotByStrangers()
    {
        var (owner, member, stranger) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Joined));

        await using var db = fixture.NewContext();
        var asset = new MediaAsset { UserId = owner, Kind = MediaKind.Photo, ContentType = "image/jpeg", Length = 4, CreatedAt = T0, FamilyRecipeId = id };
        db.MediaAssets.Add(asset);
        await db.SaveChangesAsync();

        var access = new FamilyAccessService(fixture.NewContext());
        Assert.True(await access.CanReadMediaAsync(asset, member, default));
        Assert.False(await access.CanReadMediaAsync(asset, stranger, default));
    }

    [Fact]
    public async Task ARemovedMembersMediaAccess_IsRevoked()
    {
        var (owner, member, _) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Joined));

        await using var db = fixture.NewContext();
        var asset = new MediaAsset { UserId = owner, Kind = MediaKind.Photo, ContentType = "image/jpeg", Length = 4, CreatedAt = T0, FamilyRecipeId = id };
        db.MediaAssets.Add(asset);
        await db.SaveChangesAsync();

        var row = await db.Set<FamilyMember>().FirstAsync(m => m.FamilyRecipeId == id && m.UserId == member);
        row.State = MemberState.Removed;
        await db.SaveChangesAsync();

        var access = new FamilyAccessService(fixture.NewContext());
        Assert.False(await access.CanReadMediaAsync(asset, member, default));
    }

    [Fact]
    public async Task PublicRecipesMedia_IsReadableByAStranger()
    {
        var (owner, _, stranger) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.PublicInArchive);

        await using var db = fixture.NewContext();
        var asset = new MediaAsset { UserId = owner, Kind = MediaKind.Photo, ContentType = "image/jpeg", Length = 4, CreatedAt = T0, FamilyRecipeId = id };
        db.MediaAssets.Add(asset);
        await db.SaveChangesAsync();

        var access = new FamilyAccessService(fixture.NewContext());
        Assert.True(await access.CanReadMediaAsync(asset, stranger, default));
    }
}
