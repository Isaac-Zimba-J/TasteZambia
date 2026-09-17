using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Repositories;

// Repositories fetch; services decide. These mirror the mobile plan's interfaces so the
// two sides stay legible to one another. Every read is AsNoTracking.

public interface IDishRepository
{
    Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default);
    Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Recipe?> GetRecipeAsync(string dishId, CancellationToken ct = default);
}

public sealed class DishRepository(TasteZambiaDbContext db) : IDishRepository
{
    public async Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default)
        => await db.Dishes.AsNoTracking().OrderBy(d => d.SortOrder).ToListAsync(ct);

    public Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default)
        => db.Dishes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Recipe?> GetRecipeAsync(string dishId, CancellationToken ct = default)
        => db.Recipes
            .AsNoTracking()
            .Include(r => r.Dish)
            .Include(r => r.CulturalContext.OrderBy(p => p.SortOrder))
            .Include(r => r.Ingredients.OrderBy(i => i.SortOrder))
            .Include(r => r.Steps.OrderBy(s => s.Number))
            .Include(r => r.Methods).ThenInclude(m => m.Paragraphs.OrderBy(p => p.SortOrder))
            .Include(r => r.Variations.OrderBy(v => v.SortOrder))
            .FirstOrDefaultAsync(r => r.DishId == dishId, ct);
}

public interface IIngredientRepository
{
    Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken ct = default);
    Task<Ingredient?> GetByKeyAsync(string key, CancellationToken ct = default);
}

public sealed class IngredientRepository(TasteZambiaDbContext db) : IIngredientRepository
{
    public async Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken ct = default)
        => await db.Ingredients.AsNoTracking()
            .Include(i => i.LocalNames.OrderBy(l => l.SortOrder))
            .Include(i => i.Usages.OrderBy(u => u.SortOrder))
            .OrderBy(i => i.SortOrder)
            .ToListAsync(ct);

    public Task<Ingredient?> GetByKeyAsync(string key, CancellationToken ct = default)
        => db.Ingredients.AsNoTracking()
            .Include(i => i.LocalNames.OrderBy(l => l.SortOrder))
            .Include(i => i.Usages.OrderBy(u => u.SortOrder))
            .FirstOrDefaultAsync(i => i.Key == key, ct);
}

public interface IRegionRepository
{
    Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken ct = default);
}

public sealed class RegionRepository(TasteZambiaDbContext db) : IRegionRepository
{
    public async Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken ct = default)
        => await db.Provinces.AsNoTracking()
            .Include(p => p.Foods.OrderBy(f => f.SortOrder))
            .Include(p => p.CommonIngredients.OrderBy(i => i.SortOrder))
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);
}

public interface IArticleRepository
{
    Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default);
    Task<Article?> GetByIdAsync(string id, CancellationToken ct = default);
}

public sealed class ArticleRepository(TasteZambiaDbContext db) : IArticleRepository
{
    /// <summary>List view: metadata only. The Culture screen shows kicker, title and
    /// byline, so pulling every essay's prose to render six rows would be waste.</summary>
    public async Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default)
        => await db.Articles.AsNoTracking().OrderBy(a => a.SortOrder).ToListAsync(ct);

    public Task<Article?> GetByIdAsync(string id, CancellationToken ct = default)
        => db.Articles.AsNoTracking()
            .Include(a => a.Body.OrderBy(b => b.SortOrder))
            .Include(a => a.RelatedDishes.OrderBy(r => r.SortOrder))
            .FirstOrDefaultAsync(a => a.Id == id, ct);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
}

public sealed class CategoryRepository(TasteZambiaDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => await db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync(ct);
}
