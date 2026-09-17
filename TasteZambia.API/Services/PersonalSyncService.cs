using TasteZambia.API.Repositories;
using TasteZambia.Shared.Contracts.Me;

namespace TasteZambia.API.Services;

public interface IPersonalSyncService
{
    Task<SyncResponse> ApplyAsync(string userId, SyncRequest request, CancellationToken ct);
}

/// <summary>
/// Last-write-wins reconciliation. Each incoming change carries the client's timestamp;
/// it applies only if newer than what the server holds. The response is the account's
/// complete state, which the client adopts wholesale - there is no partial merge.
/// </summary>
public sealed class PersonalSyncService(IPersonalDataRepository personal, TimeProvider clock) : IPersonalSyncService
{
    public async Task<SyncResponse> ApplyAsync(string userId, SyncRequest request, CancellationToken ct)
    {
        foreach (var change in request.Changes)
        {
            switch (change.Kind)
            {
                case SyncChangeDto.Saved:
                    await personal.UpsertSavedAsync(userId, change.DishId, change.Value, change.At, ct);
                    break;
                case SyncChangeDto.Progress when change.Step is { } step:
                    await personal.UpsertProgressAsync(userId, change.DishId, step, change.Value, change.At, ct);
                    break;
                // Unknown kinds are ignored, not rejected: an older client must not be
                // able to break sync for itself by sending something this server predates.
            }
        }

        if (request.Changes.Count > 0)
            await personal.SaveChangesAsync(ct);

        var saved = (await personal.GetSavedAsync(userId, ct))
            .Select(s => new SavedDishDto(s.DishId, s.IsSaved, s.UpdatedAt)).ToList();
        var progress = (await personal.GetProgressAsync(userId, null, ct))
            .Select(p => new CookProgressDto(p.DishId, p.StepNumber, p.IsDone, p.UpdatedAt)).ToList();

        return new SyncResponse(saved, progress, clock.GetUtcNow());
    }
}
