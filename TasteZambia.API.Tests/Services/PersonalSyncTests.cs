using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Me;

namespace TasteZambia.API.Tests.Services;

[Collection(nameof(DatabaseCollection))]
public class PersonalSyncTests(DatabaseFixture fixture)
{
    private async Task<(PersonalSyncService svc, string userId)> SutAsync()
    {
        var db = fixture.NewContext();
        var user = new ArchiveUser { UserName = $"device-{Guid.NewGuid():N}" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (new PersonalSyncService(new PersonalDataRepository(db), TimeProvider.System), user.Id);
    }

    private static SyncChangeDto Save(string dish, bool value, DateTimeOffset at) => new(SyncChangeDto.Saved, dish, null, value, at);
    private static SyncChangeDto Step(string dish, int step, bool value, DateTimeOffset at) => new(SyncChangeDto.Progress, dish, step, value, at);

    [Fact]
    public async Task NewerClientChange_Wins()
    {
        var (svc, uid) = await SutAsync();
        var t0 = DateTimeOffset.UtcNow.AddMinutes(-10);

        await svc.ApplyAsync(uid, new SyncRequest([Save("ifisashi", true, t0)]), default);
        var result = await svc.ApplyAsync(uid, new SyncRequest([Save("ifisashi", false, t0.AddMinutes(1))]), default);

        Assert.False(result.Saved.Single(s => s.DishId == "ifisashi").IsSaved);
    }

    [Fact]
    public async Task OlderClientChange_Loses()
    {
        var (svc, uid) = await SutAsync();
        var t0 = DateTimeOffset.UtcNow.AddMinutes(-10);

        await svc.ApplyAsync(uid, new SyncRequest([Save("ifisashi", true, t0)]), default);
        var result = await svc.ApplyAsync(uid, new SyncRequest([Save("ifisashi", false, t0.AddMinutes(-1))]), default);

        Assert.True(result.Saved.Single(s => s.DishId == "ifisashi").IsSaved);   // stale unsave ignored
    }

    [Fact]
    public async Task ProgressIsPerStep_AndReturnedInFull()
    {
        var (svc, uid) = await SutAsync();
        var t = DateTimeOffset.UtcNow.AddMinutes(-5);

        var result = await svc.ApplyAsync(uid, new SyncRequest([Step("ifisashi", 1, true, t), Step("ifisashi", 3, true, t)]), default);

        Assert.Equal(2, result.Progress.Count(p => p.DishId == "ifisashi" && p.IsDone));
        Assert.DoesNotContain(result.Progress, p => p.StepNumber == 2 && p.IsDone);
    }

    [Fact]
    public async Task EmptyRequest_ReturnsCurrentState()
    {
        var (svc, uid) = await SutAsync();
        await svc.ApplyAsync(uid, new SyncRequest([Save("chikanda", true, DateTimeOffset.UtcNow.AddMinutes(-1))]), default);

        var result = await svc.ApplyAsync(uid, new SyncRequest([]), default);

        Assert.Single(result.Saved, s => s.DishId == "chikanda" && s.IsSaved);
        Assert.True(result.ServerTime <= DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task UsersDoNotSeeEachOther()
    {
        var (svc, a) = await SutAsync();
        var (_, b) = await SutAsync();
        await svc.ApplyAsync(a, new SyncRequest([Save("delele", true, DateTimeOffset.UtcNow)]), default);

        var forB = await svc.ApplyAsync(b, new SyncRequest([]), default);
        Assert.Empty(forB.Saved);
    }
}
