using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Repositories;

public interface IPersonalDataRepository
{
    Task<IReadOnlyList<SavedDish>> GetSavedAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<CookProgress>> GetProgressAsync(string userId, string? dishId = null, CancellationToken ct = default);

    /// <summary>Applies only if <paramref name="at"/> is newer than the stored row. Returns whether it applied.</summary>
    Task<bool> UpsertSavedAsync(string userId, string dishId, bool isSaved, DateTimeOffset at, CancellationToken ct = default);
    Task<bool> UpsertProgressAsync(string userId, string dishId, int step, bool isDone, DateTimeOffset at, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class PersonalDataRepository(TasteZambiaDbContext db) : IPersonalDataRepository
{
    public async Task<IReadOnlyList<SavedDish>> GetSavedAsync(string userId, CancellationToken ct = default)
        => await db.SavedDishes.AsNoTracking().Where(s => s.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<CookProgress>> GetProgressAsync(string userId, string? dishId = null, CancellationToken ct = default)
    {
        var q = db.CookProgress.AsNoTracking().Where(p => p.UserId == userId);
        if (dishId is not null) q = q.Where(p => p.DishId == dishId);
        return await q.ToListAsync(ct);
    }

    public async Task<bool> UpsertSavedAsync(string userId, string dishId, bool isSaved, DateTimeOffset at, CancellationToken ct = default)
    {
        var row = await db.SavedDishes.FindAsync([userId, dishId], ct);
        if (row is null)
        {
            db.SavedDishes.Add(new SavedDish { UserId = userId, DishId = dishId, IsSaved = isSaved, UpdatedAt = at });
            return true;
        }
        if (at <= row.UpdatedAt) return false;   // last write wins; this one is older
        row.IsSaved = isSaved;
        row.UpdatedAt = at;
        return true;
    }

    public async Task<bool> UpsertProgressAsync(string userId, string dishId, int step, bool isDone, DateTimeOffset at, CancellationToken ct = default)
    {
        var row = await db.CookProgress.FindAsync([userId, dishId, step], ct);
        if (row is null)
        {
            db.CookProgress.Add(new CookProgress { UserId = userId, DishId = dishId, StepNumber = step, IsDone = isDone, UpdatedAt = at });
            return true;
        }
        if (at <= row.UpdatedAt) return false;
        row.IsDone = isDone;
        row.UpdatedAt = at;
        return true;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

public interface IUserProfileRepository
{
    Task<UserProfile> GetOrCreateProfileAsync(string userId, CancellationToken ct = default);
    Task<OnboardingChoices> GetOrCreateOnboardingAsync(string userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class UserProfileRepository(TasteZambiaDbContext db) : IUserProfileRepository
{
    public async Task<UserProfile> GetOrCreateProfileAsync(string userId, CancellationToken ct = default)
    {
        var row = await db.UserProfiles.FindAsync([userId], ct);
        if (row is not null) return row;
        row = new UserProfile { UserId = userId, UpdatedAt = DateTimeOffset.UtcNow };
        db.UserProfiles.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    public async Task<OnboardingChoices> GetOrCreateOnboardingAsync(string userId, CancellationToken ct = default)
    {
        var row = await db.OnboardingChoices.FindAsync([userId], ct);
        if (row is not null) return row;
        row = new OnboardingChoices { UserId = userId, UpdatedAt = DateTimeOffset.UtcNow };
        db.OnboardingChoices.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
