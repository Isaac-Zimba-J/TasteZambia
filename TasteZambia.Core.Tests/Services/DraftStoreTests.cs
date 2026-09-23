using Microsoft.Extensions.Time.Testing;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public class DraftStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    private static (DraftStore store, FakeTimeProvider clock) Sut(ILocalStore? local = null)
    {
        var clock = new FakeTimeProvider(Now);
        return (new DraftStore(local ?? new InMemoryLocalStore(), clock), clock);
    }

    [Fact]
    public void Create_ThenAll_IsNewestEditFirstAndSurvivesAReload()
    {
        var local = new InMemoryLocalStore();
        var (store, clock) = Sut(local);
        var first = store.Create(new ContributionDraft { LocalName = "Munkoyo" });
        clock.Advance(TimeSpan.FromHours(1));
        var second = store.Create(new ContributionDraft { LocalName = "Ubwali bwa Tute" });

        var (reloaded, _) = Sut(local);
        Assert.Equal([second.Id, first.Id], reloaded.All.Select(d => d.Id));
        Assert.Equal("Munkoyo", reloaded.All[1].Draft.LocalName);
    }

    [Fact]
    public void Save_BumpsEditedAtAndRaisesChanged()
    {
        var (store, clock) = Sut();
        var d = store.Create(new ContributionDraft { LocalName = "Munkoyo" });
        var raised = false;
        store.Changed += (_, _) => raised = true;

        clock.Advance(TimeSpan.FromMinutes(5));
        d.Draft.Province = "Northern";
        store.Save(d);

        Assert.True(raised);
        Assert.Equal(Now.AddMinutes(5), store.All.Single().EditedAt);
        Assert.Equal("Northern", store.All.Single().Draft.Province);
    }

    [Fact]
    public void Remove_DropsIt()
    {
        var (store, _) = Sut();
        var d = store.Create(new ContributionDraft());
        store.Remove(d.Id);
        Assert.Empty(store.All);
    }

    [Fact]
    public void IngredientsSurviveTheJsonRoundTrip()
    {
        var local = new InMemoryLocalStore();
        var (store, _) = Sut(local);
        store.Create(SeedData.WalkthroughShareDraft());

        var (reloaded, _) = Sut(local);
        var draft = reloaded.All.Single().Draft;
        Assert.Equal(3, draft.Ingredients.Count);
        Assert.Equal("chibwabwa", draft.Ingredients[0].IngredientKey);
        Assert.True(draft.Ingredients[0].IsLinked);
        Assert.Equal(2, draft.Steps.Count);
    }

    [Fact]
    public void RecipeDraftCard_ReflectsCompleteness()
    {
        var empty = new LocalDraft { Id = Guid.NewGuid(), EditedAt = Now.AddDays(-21), Draft = new() { LocalName = "Ubwali bwa Tute", Province = "Luapula" } };
        var card = RecipeDraft.From(empty, Now);
        Assert.Equal(22, card.PercentComplete);   // 2 of 9
        Assert.Equal("Needs a short English description", card.Missing);
        Assert.Equal("Edited 3 weeks ago", card.When);
        Assert.Equal("#A3452A", card.TintHex);
        Assert.Equal(empty.Id, card.Id);

        var full = new LocalDraft { Id = Guid.NewGuid(), EditedAt = Now.AddHours(-2), Draft = SeedData.WalkthroughShareDraft() };
        var fullCard = RecipeDraft.From(full, Now);
        Assert.Equal(100, fullCard.PercentComplete);
        Assert.Equal("Ready to submit", fullCard.Missing);
        Assert.Equal("Edited 2 hours ago", fullCard.When);
        Assert.Equal("#2F6A4D", fullCard.TintHex);
    }

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(1, "1 minute ago")]
    [InlineData(59, "59 minutes ago")]
    [InlineData(60, "1 hour ago")]
    [InlineData(60 * 24 * 4, "4 days ago")]
    [InlineData(60 * 24 * 7, "1 week ago")]
    [InlineData(60 * 24 * 45, "1 month ago")]
    public void Relative_ReadsLikeTheDesign(int minutesAgo, string expected)
        => Assert.Equal(expected, RecipeDraft.Relative(TimeSpan.FromMinutes(minutesAgo)));
}
