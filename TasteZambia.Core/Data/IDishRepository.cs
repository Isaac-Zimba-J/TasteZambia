using TasteZambia.Core.Models;

namespace TasteZambia.Core.Data;

public interface IDishRepository
{
    Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default);
    Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<RecipeDetail?> GetRecipeAsync(string dishId, CancellationToken ct = default);
}

public interface IIngredientRepository
{
    Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken ct = default);
    Task<Ingredient?> GetByKeyAsync(string key, CancellationToken ct = default);
}

public interface IRegionRepository
{
    Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken ct = default);
}

public interface IArticleRepository
{
    Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default);
    Task<Article?> GetByIdAsync(string id, CancellationToken ct = default);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
}

public interface IProfileRepository
{
    Task<UserProfile> GetAsync(CancellationToken ct = default);

    /// <summary>Saves the reader's own name, location and languages to their account.</summary>
    Task UpdateAsync(string name, string location, string languages, CancellationToken ct = default);

    Task<IReadOnlyList<RecipeCollection>> GetCollectionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default);
}
