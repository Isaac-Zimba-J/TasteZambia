using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data;

public class TasteZambiaDbContext(DbContextOptions<TasteZambiaDbContext> options)
    : IdentityDbContext<ArchiveUser>(options)
{
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // The personal layer: one row per user, or per (user, dish[, step]).
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<OnboardingChoices> OnboardingChoices => Set<OnboardingChoices>();
    public DbSet<SavedDish> SavedDishes => Set<SavedDish>();
    public DbSet<CookProgress> CookProgress => Set<CookProgress>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Identity configures its own tables here. It MUST run before ours.
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(TasteZambiaDbContext).Assembly);

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UserId);
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
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
    /// column and the ETag is derived from it, so it must be stamped on every write
    /// without exception - including writes to child rows. Editing one cooking step is a
    /// change to the recipe as far as any client is concerned, so the step's owning root
    /// is stamped too. Without that, a client holding the old ETag would keep getting 304.
    /// </summary>
    private void StampTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            if (entry.Entity is ArchiveEntity root)
            {
                if (entry.State != EntityState.Deleted)
                    root.UpdatedAt = now;
                continue;
            }

            // A child row: walk up its foreign keys to whichever tracked archive root owns it.
            foreach (var fk in entry.Metadata.GetForeignKeys())
            {
                // Most children are configured WithOne() and carry no back-reference, so
                // only consult the navigation when one exists.
                if (fk.DependentToPrincipal is { Name: var navName }
                    && entry.Navigation(navName).CurrentValue is ArchiveEntity owner)
                {
                    owner.UpdatedAt = now;
                    continue;
                }

                // No navigation loaded (children are often configured WithOne() and no
                // back-reference). Find the principal by key among tracked entries.
                var keyValues = fk.Properties.Select(pr => entry.Property(pr.Name).CurrentValue).ToArray();
                var tracked = ChangeTracker.Entries()
                    .FirstOrDefault(e => e.Metadata == fk.PrincipalEntityType
                        && fk.PrincipalKey.Properties.Select(pk => e.Property(pk.Name).CurrentValue)
                              .SequenceEqual(keyValues));
                if (tracked?.Entity is ArchiveEntity trackedOwner)
                {
                    trackedOwner.UpdatedAt = now;
                    if (tracked.State == EntityState.Unchanged) tracked.State = EntityState.Modified;
                }
            }
        }
    }
}
