using TasteZambia.Core.Models;

namespace TasteZambia.Core.Data;

public sealed class InMemoryDishRepository : IDishRepository
{
    public Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Dishes);

    public Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(SeedData.Dishes.FirstOrDefault(d => d.Id == id));

    public Task<RecipeDetail?> GetRecipeAsync(string dishId, CancellationToken ct = default)
        => Task.FromResult<RecipeDetail?>(dishId == "ifisashi" ? SeedData.IfisashiRecipe() : null);
}

public sealed class InMemoryIngredientRepository : IIngredientRepository
{
    public Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Ingredients);

    public Task<Ingredient?> GetByKeyAsync(string key, CancellationToken ct = default)
        => Task.FromResult(SeedData.Ingredients.FirstOrDefault(i => i.Key == key));
}

public sealed class InMemoryRegionRepository : IRegionRepository
{
    public Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Provinces);
}

public sealed class InMemoryArticleRepository : IArticleRepository
{
    public Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Articles);

    public Task<Article?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(SeedData.Articles.FirstOrDefault(a => a.Id == id));
}

public sealed class InMemoryCategoryRepository : ICategoryRepository
{
    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Categories);
}

public sealed class InMemoryProfileRepository : IProfileRepository
{
    public Task<UserProfile> GetAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Profile);

    public Task<IReadOnlyList<RecipeCollection>> GetCollectionsAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Collections);

    public Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Contributions);
}
