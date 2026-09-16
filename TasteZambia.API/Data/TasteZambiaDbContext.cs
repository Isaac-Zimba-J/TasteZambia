using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data;

public class TasteZambiaDbContext(DbContextOptions<TasteZambiaDbContext> options)
    : DbContext(options)
{
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(TasteZambiaDbContext).Assembly);
        base.OnModelCreating(b);
    }

    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAll, CancellationToken ct = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAll, ct);
    }

    /// <summary>
    /// Every archive row records when it last changed. Stage 5's delta sync reads this
    /// column, so it must be stamped on every write without exception.
    /// </summary>
    private void StampTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ArchiveEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
