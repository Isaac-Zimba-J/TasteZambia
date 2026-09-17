using TasteZambia.Core.Data;
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<Dish>> SearchAsync(string query, string filter, CancellationToken ct = default);
    Task<IReadOnlyList<Dish>> GetDishesByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default);
}

public sealed class CatalogService(IDishRepository dishes) : ICatalogService
{
    public async Task<IReadOnlyList<Dish>> SearchAsync(string query, string filter, CancellationToken ct = default)
    {
        var all = await dishes.GetAllAsync(ct);
        ct.ThrowIfCancellationRequested();
        var q = query.Trim();

        if (q.Length == 0)
            return all;

        // The design searches local name, English name, region and description as one haystack.
        return all
            .Where(d => $"{d.LocalName} {d.EnglishName} {d.Region} {d.Description}"
                .Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<Dish>> GetDishesByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var all = await dishes.GetAllAsync(ct);
        var lookup = all.ToDictionary(d => d.Id);

        return ids.Where(lookup.ContainsKey).Select(id => lookup[id]).ToList();
    }
}
