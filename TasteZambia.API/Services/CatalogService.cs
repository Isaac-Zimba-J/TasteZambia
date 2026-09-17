using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;

namespace TasteZambia.API.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<Dish>> SearchAsync(string query, string filter, CancellationToken ct = default);
}

public sealed class CatalogService(IDishRepository dishes) : ICatalogService
{
    /// <summary>
    /// Filters in memory over the archive, matching the mobile client exactly. Correct at
    /// this size and it keeps parity; past a few hundred dishes, move the predicate into the
    /// repository as a Postgres tsvector index. That is a repository change only - this
    /// signature and every caller stay put.
    /// </summary>
    public async Task<IReadOnlyList<Dish>> SearchAsync(string query, string filter, CancellationToken ct = default)
    {
        var all = await dishes.GetAllAsync(ct);
        var q = query.Trim();

        if (q.Length == 0)
            return all;

        // Local name, English name, region and description are one haystack.
        // `filter` is accepted and not yet applied: the design's chips change
        // appearance without narrowing results. Keeping it in the signature means
        // neither the endpoint nor the mobile ViewModel changes when filtering lands.
        return all
            .Where(d => $"{d.LocalName} {d.EnglishName} {d.Region} {d.Description}"
                .Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
