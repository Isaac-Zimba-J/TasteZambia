using TasteZambia.Core.Services;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.Services;

public class SyncSchedulerTests
{
    private sealed class FakeSync : IPersonalSyncService
    {
        public int Calls;
        public Task<bool> SyncAsync(CancellationToken ct = default) { Calls++; return Task.FromResult(true); }
    }

    /// <summary>Records drains; nothing here needs to actually reach a server.</summary>
    private sealed class FakeMediaUploader : IMediaUploader
    {
        public int DrainCalls;

        public Task<UploadOutcome> UploadAsync(PickedFile file, MediaKind kind, UploadTarget target, CancellationToken ct = default)
            => Task.FromResult<UploadOutcome>(new UploadOutcome.Queued(Guid.NewGuid()));

        public Task<int> DrainAsync(CancellationToken ct = default) { DrainCalls++; return Task.FromResult(0); }
        public void Cancel(Guid localId) { }
        public IReadOnlyList<Guid> ClaimFor(Guid draftId) => [];
        public void ForgetClaims(Guid draftId) { }
        public IReadOnlyList<PendingUpload> Pending => [];
        public event EventHandler? Changed;
        public event EventHandler<Guid>? Lost;
        public event EventHandler<string>? Rejected;
        public event EventHandler<(Guid LocalId, Guid MediaId)>? Uploaded;
    }

    [Fact]
    public void Start_SyncsPersonalDataAndDrainsTheMediaQueue()
    {
        var sync = new FakeSync();
        var media = new FakeMediaUploader();
        var scheduler = new SyncScheduler(sync, TestServices.Personal(), media);

        scheduler.Start();

        Assert.Equal(1, sync.Calls);
        Assert.Equal(1, media.DrainCalls);
    }

    [Fact]
    public void OnResumed_SyncsAndDrainsAgain()
    {
        var sync = new FakeSync();
        var media = new FakeMediaUploader();
        var scheduler = new SyncScheduler(sync, TestServices.Personal(), media);

        scheduler.OnResumed();

        Assert.Equal(1, sync.Calls);
        Assert.Equal(1, media.DrainCalls);
    }
}
