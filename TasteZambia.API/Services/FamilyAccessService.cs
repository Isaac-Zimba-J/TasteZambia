using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Services;

public interface IFamilyAccessService
{
    /// <summary>Whether this user may read this blob. Task 3 widens it to family members.</summary>
    Task<bool> CanReadMediaAsync(MediaAsset asset, string userId, CancellationToken ct);
}

// db is unused until Task 3 adds the family-membership branch; the constructor shape
// is fixed now so DI registration and test construction do not change between tasks.
public sealed class FamilyAccessService(TasteZambiaDbContext db) : IFamilyAccessService
{
    public Task<bool> CanReadMediaAsync(MediaAsset asset, string userId, CancellationToken ct)
        => Task.FromResult(asset.UserId == userId);
}
