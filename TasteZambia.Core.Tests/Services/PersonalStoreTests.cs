using Microsoft.Extensions.Time.Testing;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Contracts.Me;

namespace TasteZambia.Core.Tests.Services;

public class PersonalStoreTests
{
    private static (PersonalStore store, FakeTimeProvider clock) Sut(ILocalStore? local = null)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero));
        return (new PersonalStore(local ?? new InMemoryLocalStore(), clock), clock);
    }

    [Fact]
    public void NothingIsSavedUntilTheReaderSavesIt()
    {
        var (store, _) = Sut();
        Assert.False(store.IsSaved("ifisashi"));
        Assert.False(store.IsSaved("chikanda"));
        Assert.Empty(store.SavedDishIds);
    }

    [Fact]
    public void FreshStore_HasNothingToSync()
    {
        var (store, _) = Sut();
        Assert.Empty(store.DrainOutbox());
    }

    [Fact]
    public void SetSaved_WritesLocallyAndQueuesAStampedChange()
    {
        var (store, clock) = Sut();
        store.SetSaved("chikanda", true);

        Assert.True(store.IsSaved("chikanda"));
        var change = Assert.Single(store.DrainOutbox());
        Assert.Equal(SyncChangeDto.Saved, change.Kind);
        Assert.Equal("chikanda", change.DishId);
        Assert.True(change.Value);
        Assert.Equal(clock.GetUtcNow(), change.At);
    }

    [Fact]
    public void State_SurvivesANewStoreOverTheSameLocalStore()
    {
        var local = new InMemoryLocalStore();
        var (first, _) = Sut(local);
        first.SetDone("ifisashi", 2, true);

        var (second, _) = Sut(local);
        Assert.True(second.IsDone("ifisashi", 2));
        Assert.Single(second.DrainOutbox());   // the outbox persisted too
    }

    [Fact]
    public void Apply_ReplacesLocalStateWithTheServers()
    {
        var (store, clock) = Sut();
        store.SetSaved("chikanda", true);
        store.DrainOutbox();

        store.Apply(new SyncResponse(
            [new("delele", true, clock.GetUtcNow()), new("ifisashi", false, clock.GetUtcNow())],
            [new("nshima", 1, true, clock.GetUtcNow())],
            clock.GetUtcNow()));

        Assert.True(store.IsSaved("delele"));
        Assert.False(store.IsSaved("ifisashi"));   // server said unsaved; server is the merged truth
        Assert.False(store.IsSaved("chikanda"));   // not in the response: gone
        Assert.True(store.IsDone("nshima", 1));
    }

    [Fact]
    public void Apply_DoesNotDiscardChangesMadeAfterTheDrain()
    {
        var (store, clock) = Sut();
        store.DrainOutbox();
        store.SetSaved("kapenta", true);   // user tapped while the request was in flight

        store.Apply(new SyncResponse([], [], clock.GetUtcNow()));

        Assert.True(store.IsSaved("kapenta"));      // local change kept ...
        Assert.Single(store.DrainOutbox());         // ... and still queued for the next sync
    }

    [Fact]
    public void Requeue_PutsAFailedBatchBackAheadOfNewerChanges()
    {
        var (store, _) = Sut();
        store.SetSaved("kapenta", true);
        var batch = store.DrainOutbox();
        store.SetSaved("delele", true);

        store.Requeue(batch);

        Assert.Equal(["kapenta", "delele"], store.DrainOutbox().Select(c => c.DishId));
    }

    [Fact]
    public void Changed_FiresWithTheDishId()
    {
        var (store, _) = Sut();
        string? raised = null;
        store.Changed += (_, id) => raised = id;
        store.SetDone("delele", 3, true);
        Assert.Equal("delele", raised);
    }
}
