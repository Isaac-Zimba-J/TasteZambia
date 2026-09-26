namespace TasteZambia.Core.Services;

/// <summary>
/// Syncs personal data and drains the media upload queue on launch, on resume, and two
/// seconds after the last local change. Media gets the same treatment as personal data:
/// a photograph taken with no signal is worth nothing if nothing ever retries it.
/// </summary>
public sealed class SyncScheduler(IPersonalSyncService sync, PersonalStore store, IMediaUploader media) : IDisposable
{
    private static readonly TimeSpan Debounce = TimeSpan.FromSeconds(2);
    private CancellationTokenSource? _debounce;

    public void Start()
    {
        store.Changed += OnChanged;
        media.Changed += OnMediaChanged;
        _ = sync.SyncAsync();
        _ = media.DrainAsync();
    }

    public void OnResumed()
    {
        _ = sync.SyncAsync();
        _ = media.DrainAsync();
    }

    private void OnChanged(object? sender, string dishId)
    {
        // Apply() raises Changed with an empty id; that is the sync landing, not a tap.
        if (dishId.Length == 0) return;
        ScheduleDebounced();
    }

    private void OnMediaChanged(object? sender, EventArgs e) => ScheduleDebounced();

    private void ScheduleDebounced()
    {
        _debounce?.Cancel();
        var cts = _debounce = new CancellationTokenSource();
        _ = Task.Delay(Debounce, cts.Token).ContinueWith(
            t => { if (!t.IsCanceled) { _ = sync.SyncAsync(); _ = media.DrainAsync(); } },
            TaskScheduler.Default);
    }

    public void Dispose()
    {
        store.Changed -= OnChanged;
        media.Changed -= OnMediaChanged;
        _debounce?.Cancel();
    }
}
