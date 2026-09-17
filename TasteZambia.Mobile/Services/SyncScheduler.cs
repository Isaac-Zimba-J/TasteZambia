using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

/// <summary>Syncs on launch, on resume, and two seconds after the last local change.</summary>
public sealed class SyncScheduler(IPersonalSyncService sync, PersonalStore store) : IDisposable
{
    private static readonly TimeSpan Debounce = TimeSpan.FromSeconds(2);
    private CancellationTokenSource? _debounce;

    public void Start()
    {
        store.Changed += OnChanged;
        _ = sync.SyncAsync();
    }

    public void OnResumed() => _ = sync.SyncAsync();

    private void OnChanged(object? sender, string dishId)
    {
        // Apply() raises Changed with an empty id; that is the sync landing, not a tap.
        if (dishId.Length == 0) return;

        _debounce?.Cancel();
        var cts = _debounce = new CancellationTokenSource();
        _ = Task.Delay(Debounce, cts.Token).ContinueWith(
            t => { if (!t.IsCanceled) _ = sync.SyncAsync(); },
            TaskScheduler.Default);
    }

    public void Dispose()
    {
        store.Changed -= OnChanged;
        _debounce?.Cancel();
    }
}
