using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Contracts.Review;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Tests.Services;

[Collection(nameof(DatabaseCollection))]
public class ContributionStateMachineTests(DatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    // Published dishes land in the shared archive tables; the read-side tests count them.
    public Task DisposeAsync() => fixture.RemoveCommunityDishesAsync();

    private static readonly DateTimeOffset T0 = new(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

    private async Task<(ContributionService svc, string userId, FakeTimeProvider clock)> SutAsync()
    {
        var db = fixture.NewContext();
        var user = new ArchiveUser { UserName = $"device-{Guid.NewGuid():N}" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var clock = new FakeTimeProvider(T0);
        return (new ContributionService(new ContributionRepository(db), new UserProfileRepository(db), clock), user.Id, clock);
    }

    private static SubmitContributionRequest Chibwabwa() => new(
        "Chibwabwa na Mbalala", "Pumpkin leaves cooked with pounded groundnuts and nothing else", "Northern", "Relish", "",
        [new("chibwabwa", "Chibwabwa", "Pumpkin leaves", "2 bundles"), new(null, "Salt", "Mucele", "To taste")],
        ["Shred the leaves fine and rinse them twice.", "Pound the groundnuts until the oil starts to show."],
        "Cooked in Mungwi and the villages around Kasama.", "This is the relish cooked when there is no money for meat.",
        "Clay pot on charcoal.", "", "", true);

    [Fact]
    public async Task Submit_ArrivesInReviewWithASubmittedEvent()
    {
        var (svc, uid, _) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);

        Assert.Equal(ContributionStatus.InReview, c.Status);
        Assert.Equal(T0, c.SubmittedAt);
        Assert.Single(c.Events, e => e.Kind == ReviewEventKind.Submitted);
        Assert.Equal(ContributionService.AnonymousContributor, c.ContributorName);   // profile has no name yet
        Assert.Equal(2, c.Ingredients.Count);
        Assert.Equal(2, c.Steps.Count);
    }

    [Fact]
    public async Task RequestChanges_ThenResubmit_ThenPublish_IsTheHappyPath()
    {
        var (svc, uid, clock) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);

        clock.Advance(TimeSpan.FromDays(1));
        c = await svc.RequestChangesAsync(c.Id, "Namakau Sitali", "Two things I want to get right.",
            [new("Local name", "Is this the Mungwi name or the Kasama town name?", "Chibwabwa na Mbalala")], default);
        Assert.Equal(ContributionStatus.ChangesRequested, c.Status);
        var flag = Assert.Single(c.Flags);

        clock.Advance(TimeSpan.FromDays(1));
        c = await svc.ResubmitAsync(c.Id, uid, [new(flag.Id, "Mungwi specifically.")], default);
        Assert.Equal(ContributionStatus.InReview, c.Status);
        Assert.Equal("Mungwi specifically.", c.Flags.Single().Answer);

        clock.Advance(TimeSpan.FromDays(1));
        c = await svc.PublishAsync(c.Id, "Namakau Sitali", default);
        Assert.Equal(ContributionStatus.Published, c.Status);
        Assert.Equal("chibwabwa-na-mbalala", c.PublishedDishId);
        Assert.Equal(
            [ReviewEventKind.Submitted, ReviewEventKind.ChangesRequested, ReviewEventKind.Resubmitted, ReviewEventKind.Published],
            c.Events.OrderBy(e => e.At).Select(e => e.Kind));
    }

    [Fact]
    public async Task Publish_CreatesACommunityDishWithARecipe()
    {
        var (svc, uid, _) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa() with { LocalName = $"Publish test {Guid.NewGuid():N}"[..40] }, default);
        var published = await svc.PublishAsync(c.Id, "Namakau Sitali", default);

        await using var db = fixture.NewContext();
        var dish = await db.Dishes.SingleAsync(d => d.Id == published.PublishedDishId);
        Assert.Equal(Provenance.Community, dish.Provenance);
        Assert.Equal(c.Id, dish.ContributionId);
        Assert.Equal("Northern", dish.Region);

        var recipe = await new DishRepository(db).GetRecipeAsync(dish.Id);
        Assert.NotNull(recipe);
        Assert.True(recipe!.IsVerified);
        Assert.Equal(2, recipe.Ingredients.Count);
        Assert.Equal(2, recipe.Steps.Count);
        Assert.Equal("Shred the leaves fine and rinse them twice.", recipe.Steps[0].Body);
    }

    [Fact]
    public async Task Publish_Twice_ThrowsAndDoesNotMakeASecondDish()
    {
        var (svc, uid, _) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);
        await svc.PublishAsync(c.Id, "Namakau Sitali", default);

        await Assert.ThrowsAsync<InvalidContributionTransitionException>(() => svc.PublishAsync(c.Id, "Namakau Sitali", default));

        await using var db = fixture.NewContext();
        Assert.Equal(1, await db.Dishes.CountAsync(d => d.ContributionId == c.Id));
    }

    [Fact]
    public async Task Publish_WithACollidingName_GetsANumberedSlug()
    {
        var (svc, uid, _) = await SutAsync();
        var name = $"Slug test {Guid.NewGuid():N}"[..30];
        var a = await svc.SubmitAsync(uid, Chibwabwa() with { LocalName = name }, default);
        var b = await svc.SubmitAsync(uid, Chibwabwa() with { LocalName = name }, default);
        var first = await svc.PublishAsync(a.Id, "R", default);
        var second = await svc.PublishAsync(b.Id, "R", default);

        Assert.Equal(first.PublishedDishId + "-2", second.PublishedDishId);
    }

    [Fact]
    public async Task Resubmit_WithAnUnansweredFlag_IsRejected()
    {
        var (svc, uid, _) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);
        c = await svc.RequestChangesAsync(c.Id, "R", "note",
            [new("Local name", "q", "v"), new("Cooking step 2", "q2", "v2")], default);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.ResubmitAsync(c.Id, uid, [new(c.Flags[0].Id, "only one")], default));
    }

    [Theory]
    [InlineData("requestChanges", ContributionStatus.ChangesRequested)]
    [InlineData("publish", ContributionStatus.ChangesRequested)]
    [InlineData("resubmit", ContributionStatus.InReview)]
    [InlineData("withdraw", ContributionStatus.Published)]
    [InlineData("requestChanges", ContributionStatus.Withdrawn)]
    public async Task IllegalTransitions_Throw(string action, ContributionStatus from)
    {
        var (submitter, uid, _) = await SutAsync();
        var c = await submitter.SubmitAsync(uid, Chibwabwa(), default);
        await ForceStatusAsync(c.Id, from);
        var svc = NewService();   // a fresh context, as a new request would have

        Task Act() => action switch
        {
            "requestChanges" => svc.RequestChangesAsync(c.Id, "R", "n", [], default),
            "publish" => svc.PublishAsync(c.Id, "R", default),
            "resubmit" => svc.ResubmitAsync(c.Id, uid, [], default),
            "withdraw" => svc.WithdrawAsync(c.Id, uid, default),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };
        await Assert.ThrowsAsync<InvalidContributionTransitionException>(Act);
    }

    [Fact]
    public async Task Withdraw_FromInReviewAndFromChangesRequested_Works()
    {
        var (svc, uid, _) = await SutAsync();
        var a = await svc.SubmitAsync(uid, Chibwabwa(), default);
        Assert.Equal(ContributionStatus.Withdrawn, (await svc.WithdrawAsync(a.Id, uid, default)).Status);

        var b = await svc.SubmitAsync(uid, Chibwabwa(), default);
        await svc.RequestChangesAsync(b.Id, "R", "n", [], default);
        Assert.Equal(ContributionStatus.Withdrawn, (await svc.WithdrawAsync(b.Id, uid, default)).Status);
    }

    [Fact]
    public async Task MarkRead_AddsOneReadEventOnly()
    {
        var (svc, uid, _) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);
        await svc.MarkReadAsync(c.Id, "Namakau Sitali", default);
        await svc.MarkReadAsync(c.Id, "Namakau Sitali", default);

        await using var db = fixture.NewContext();
        Assert.Equal(1, await db.Set<ReviewEvent>().CountAsync(e => e.ContributionId == c.Id && e.Kind == ReviewEventKind.Read));
    }

    private ContributionService NewService()
    {
        var db = fixture.NewContext();
        return new ContributionService(new ContributionRepository(db), new UserProfileRepository(db), new FakeTimeProvider(T0));
    }

    private async Task ForceStatusAsync(Guid id, ContributionStatus status)
    {
        await using var db = fixture.NewContext();
        var c = await db.Contributions.SingleAsync(x => x.Id == id);
        c.Status = status;
        await db.SaveChangesAsync();
    }
}
