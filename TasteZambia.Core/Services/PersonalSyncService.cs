using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Services;

public interface IPersonalSyncService
{
    /// <summary>Pushes queued changes and adopts the server's state. False means nothing was lost and it will be retried.</summary>
    Task<bool> SyncAsync(CancellationToken ct = default);
}

public sealed class PersonalSyncService(PersonalStore store, HttpClient api) : IPersonalSyncService
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<bool> SyncAsync(CancellationToken ct = default)
    {
        // One at a time; a tick that arrives mid-flight is dropped and the next one catches up.
        if (!await _gate.WaitAsync(0, ct)) return false;

        var batch = store.DrainOutbox();
        try
        {
            var response = await api.PostAsJsonAsync(ApiRoutes.Me.Sync, new SyncRequest(batch), ct);
            if (!response.IsSuccessStatusCode)
            {
                store.Requeue(batch);
                return false;
            }

            store.Apply((await response.Content.ReadFromJsonAsync<SyncResponse>(ct))!);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            store.Requeue(batch);
            return false;
        }
        finally { _gate.Release(); }
    }
}
