using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;

namespace TasteZambia.API.Services;

public interface IArchiveVersionService
{
    Task<string> GetETagAsync(string resource, CancellationToken ct = default);
}

/// <summary>
/// An ETag per resource, derived from the newest UpdatedAt in that table. Cheap now, and
/// the same column Stage 5's delta sync will page on.
///
/// This is the one deliberate exception to "no service touches DbContext": it needs
/// MAX(UpdatedAt) across several tables, and pushing that into five repositories to
/// avoid one direct dependency would be worse.
/// </summary>
public sealed class ArchiveVersionService(TasteZambiaDbContext db) : IArchiveVersionService
{
    public async Task<string> GetETagAsync(string resource, CancellationToken ct = default)
    {
        DateTimeOffset? latest = resource switch
        {
            "dishes"      => await db.Dishes.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct),
            "ingredients" => await db.Ingredients.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct),
            "regions"     => await db.Provinces.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct),
            "articles"    => await db.Articles.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct),
            "categories"  => await db.Categories.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unknown archive resource"),
        };

        // Ticks, not milliseconds: two writes inside one millisecond would otherwise
        // produce the same tag and a client would keep a stale copy.
        return $"\"{resource}-{latest?.UtcTicks ?? 0}\"";
    }
}
