using TasteZambia.Core.Data;
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Services;

/// <summary>What kind of thing a search turned up. Decides the icon and where tapping it goes.</summary>
public enum SearchHitKind
{
    Dish = 0,
    Ingredient = 1,
    Story = 2,
    Province = 3,
}

/// <summary>
/// One result. Carries the route and parameter to open it, so the list does not need to know
/// how each kind of thing is reached.
/// </summary>
public sealed record SearchHit(SearchHitKind Kind, string Title, string Subtitle, string Route, string ParameterName, string ParameterValue)
{
    public string KindLabel => Kind switch
    {
        SearchHitKind.Dish => "Recipe",
        SearchHitKind.Ingredient => "Ingredient",
        SearchHitKind.Story => "Story",
        _ => "Province",
    };
}

public interface IArchiveSearchService
{
    /// <summary>
    /// Everything in the archive matching the query - recipes, ingredients, stories and
    /// provinces - in that order, because a reader searching "chibwabwa" most often wants
    /// the dish, then the ingredient, then what has been written about it.
    /// </summary>
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken ct = default);
}

/// <summary>
/// Search across the whole archive rather than only its recipes. A reader who types
/// "groundnuts" means the ingredient as much as the dishes that use it, and one who types
/// "Northern" means the province.
/// </summary>
public sealed class ArchiveSearchService(
    IDishRepository dishes,
    IIngredientRepository ingredients,
    IArticleRepository articles,
    IRegionRepository regions) : IArchiveSearchService
{
    /// <summary>Enough to be worth a round trip, short enough that two letters of a local name work.</summary>
    public const int MinimumQueryLength = 2;

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken ct = default)
    {
        var needle = query.Trim();
        if (needle.Length < MinimumQueryLength) return [];

        var hits = new List<SearchHit>();

        foreach (var d in await dishes.GetAllAsync(ct))
            if (Matches(needle, d.LocalName, d.EnglishName, d.Region, d.Description))
                hits.Add(new SearchHit(SearchHitKind.Dish, d.LocalName, d.EnglishName, "recipe", "dishId", d.Id));

        foreach (var i in await ingredients.GetAllAsync(ct))
            if (Matches(needle, i.LocalName, i.EnglishName, i.Description, i.WhereFound)
                || i.LocalNames.Any(n => Contains(n.Name, needle)))
                hits.Add(new SearchHit(SearchHitKind.Ingredient, i.LocalName, i.EnglishName, "ingredient", "key", i.Key));

        foreach (var a in await articles.GetAllAsync(ct))
            if (Matches(needle, a.Title, a.Kicker, a.Author, a.Lede ?? ""))
                hits.Add(new SearchHit(SearchHitKind.Story, a.Title, a.Kicker, "story", "articleId", a.Id));

        foreach (var p in await regions.GetAllAsync(ct))
            if (Matches(needle, p.Name, p.Seat))
                hits.Add(new SearchHit(SearchHitKind.Province, $"{p.Name} Province", p.Seat, "regions", "", ""));

        return hits;
    }

    private static bool Matches(string needle, params string[] fields)
        => fields.Any(f => Contains(f, needle));

    private static bool Contains(string haystack, string needle)
        => haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
