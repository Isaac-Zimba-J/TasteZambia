using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;

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
        var (latest, count) = resource switch
        {
            "dishes"      => await Stamp(db.Dishes, ct),
            "ingredients" => await Stamp(db.Ingredients, ct),
            "regions"     => await Stamp(db.Provinces, ct),
            "articles"    => await Stamp(db.Articles, ct),
            "categories"  => await Stamp(db.Categories, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unknown archive resource"),
        };

        // Ticks, not milliseconds: two writes inside one millisecond would otherwise
        // produce the same tag. The row count is included because MAX(UpdatedAt) alone
        // cannot see a delete - removing the newest row leaves it unchanged.
        return $"\"{resource}-{latest?.UtcTicks ?? 0}-{count}\"";
    }

    private static async Task<(DateTimeOffset? Latest, int Count)> Stamp<T>(IQueryable<T> rows, CancellationToken ct)
        where T : ArchiveEntity
        => (await rows.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct), await rows.CountAsync(ct));
}
