using System.Net;
using System.Net.Http.Json;
using TasteZambia.Core.Models;
using TasteZambia.Shared.Contracts.Common;
using TasteZambia.Shared.Contracts.Culture;
using TasteZambia.Shared.Contracts.Dishes;
using TasteZambia.Shared.Contracts.Ingredients;
using TasteZambia.Shared.Contracts.Regions;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Data.Http;

// The HTTP implementations of the same five interfaces the seeded repositories
// implement. Swapping them in MauiProgram is the ONLY change Stage 1 asks of the app.

internal static class Http
{
    /// <summary>A missing row is a null model, not an exception - the same contract the seeded repositories keep.</summary>
    public static async Task<T?> GetOrNullAsync<T>(HttpClient http, string url, CancellationToken ct) where T : class
    {
        using var response = await http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    public static string Path(string template, string name, string value)
        => template.Replace("{" + name + "}", Uri.EscapeDataString(value));
}

public sealed class HttpDishRepository(HttpClient http) : IDishRepository
{
    public async Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default)
        => (await http.GetFromJsonAsync<List<DishDto>>(ApiRoutes.Dishes.Collection, ct))?
               .Select(d => d.ToModel()).ToList() ?? [];

    public async Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default)
        => (await Http.GetOrNullAsync<DishDto>(http, Http.Path(ApiRoutes.Dishes.ById, "id", id), ct))?.ToModel();

    public async Task<RecipeDetail?> GetRecipeAsync(string dishId, CancellationToken ct = default)
        => (await Http.GetOrNullAsync<RecipeDto>(http, Http.Path(ApiRoutes.Dishes.Recipe, "id", dishId), ct))?.ToModel();
}

public sealed class HttpIngredientRepository(HttpClient http) : IIngredientRepository
{
    public async Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken ct = default)
        => (await http.GetFromJsonAsync<List<IngredientDto>>(ApiRoutes.Ingredients.Collection, ct))?
               .Select(i => i.ToModel()).ToList() ?? [];

    public async Task<Ingredient?> GetByKeyAsync(string key, CancellationToken ct = default)
        => (await Http.GetOrNullAsync<IngredientDto>(http, Http.Path(ApiRoutes.Ingredients.ByKey, "key", key), ct))?.ToModel();
}

public sealed class HttpRegionRepository(HttpClient http) : IRegionRepository
{
    public async Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken ct = default)
        => (await http.GetFromJsonAsync<List<ProvinceDto>>(ApiRoutes.Regions.Collection, ct))?
               .Select(p => p.ToModel()).ToList() ?? [];
}

public sealed class HttpArticleRepository(HttpClient http) : IArticleRepository
{
    public async Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default)
        => (await http.GetFromJsonAsync<List<ArticleDto>>(ApiRoutes.Culture.Articles, ct))?
               .Select(a => a.ToModel()).ToList() ?? [];

    public async Task<Article?> GetByIdAsync(string id, CancellationToken ct = default)
        => (await Http.GetOrNullAsync<ArticleDto>(http, Http.Path(ApiRoutes.Culture.ArticleById, "id", id), ct))?.ToModel();
}

public sealed class HttpCategoryRepository(HttpClient http) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => (await http.GetFromJsonAsync<List<CategoryDto>>(ApiRoutes.Categories.Collection, ct))?
               .Select(c => c.ToModel()).ToList() ?? [];
}
