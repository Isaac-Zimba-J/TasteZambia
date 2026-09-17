using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Configurations;

/// <summary>
/// Postgres' xmin system column as an optimistic-concurrency token. Npgsql 10 removed
/// UseXminAsConcurrencyToken(); the current form is a uint property mapped to the
/// "xmin" column with the xid store type, generated on add or update. It is a shadow
/// property here so the entities carry no Version field of their own.
/// </summary>
internal static class ConcurrencyExtensions
{
    public static EntityTypeBuilder<T> UseXminConcurrency<T>(this EntityTypeBuilder<T> e) where T : class
    {
        e.Property<uint>("xmin")
         .HasColumnName("xmin")
         .HasColumnType("xid")
         .ValueGeneratedOnAddOrUpdate()
         .IsConcurrencyToken();
        return e;
    }
}

public class DishConfiguration : IEntityTypeConfiguration<Dish>
{
    public void Configure(EntityTypeBuilder<Dish> e)
    {
        e.ToTable("dishes");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasMaxLength(64);
        e.Property(x => x.LocalName).HasMaxLength(120).IsRequired();
        e.Property(x => x.EnglishName).HasMaxLength(160).IsRequired();
        e.Property(x => x.Region).HasMaxLength(80).IsRequired();
        e.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        e.Property(x => x.PhotoNeededCaption).HasMaxLength(200);
        e.HasIndex(x => x.SortOrder);
        e.Property(x => x.Provenance).HasConversion<int>();
        e.HasIndex(x => x.ContributionId).IsUnique().HasFilter("\"ContributionId\" IS NOT NULL");

        // Postgres system column: free optimistic concurrency, no extra column to maintain.
        e.UseXminConcurrency();

        e.HasOne(x => x.Recipe)
         .WithOne(r => r.Dish)
         .HasForeignKey<Recipe>(r => r.DishId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> e)
    {
        e.ToTable("recipes");
        e.HasKey(x => x.Id);
        e.Property(x => x.DishId).HasMaxLength(64).IsRequired();
        e.HasIndex(x => x.DishId).IsUnique();
        e.UseXminConcurrency();

        e.HasMany(x => x.CulturalContext).WithOne().HasForeignKey(p => p.RecipeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Ingredients).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Steps).WithOne().HasForeignKey(s => s.RecipeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Methods).WithOne().HasForeignKey(m => m.RecipeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Variations).WithOne().HasForeignKey(v => v.RecipeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class MethodNarrativeConfiguration : IEntityTypeConfiguration<MethodNarrative>
{
    public void Configure(EntityTypeBuilder<MethodNarrative> e)
    {
        e.ToTable("method_narratives");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.RecipeId, x.Kind }).IsUnique();
        e.HasMany(x => x.Paragraphs).WithOne().HasForeignKey(p => p.MethodNarrativeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> e)
    {
        e.ToTable("ingredients");
        e.HasKey(x => x.Key);
        e.Property(x => x.Key).HasMaxLength(64);
        e.Property(x => x.LocalName).HasMaxLength(120).IsRequired();
        e.Property(x => x.EnglishName).HasMaxLength(160).IsRequired();
        e.HasIndex(x => x.SortOrder);
        e.UseXminConcurrency();

        e.HasMany(x => x.LocalNames).WithOne().HasForeignKey(l => l.IngredientKey).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Usages).WithOne().HasForeignKey(u => u.IngredientKey).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> e)
    {
        e.ToTable("provinces");
        e.HasKey(x => x.Name);
        e.Property(x => x.Name).HasMaxLength(80);
        e.HasIndex(x => x.SortOrder);
        e.UseXminConcurrency();

        e.HasMany(x => x.Foods).WithOne().HasForeignKey(f => f.ProvinceName).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.CommonIngredients).WithOne().HasForeignKey(i => i.ProvinceName).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> e)
    {
        e.ToTable("articles");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasMaxLength(64);
        e.Property(x => x.Title).HasMaxLength(200).IsRequired();
        e.HasIndex(x => x.SortOrder);
        e.UseXminConcurrency();

        e.HasMany(x => x.Body).WithOne().HasForeignKey(bl => bl.ArticleId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.RelatedDishes).WithOne().HasForeignKey(r => r.ArticleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> e)
    {
        e.ToTable("categories");
        e.HasKey(x => x.Id);
        e.Property(x => x.Name).HasMaxLength(120).IsRequired();
        e.HasIndex(x => x.SortOrder);
        e.UseXminConcurrency();
    }
}

// Child tables: snake_case names and an index on (parent, order). They carry no
// UpdatedAt of their own - the parent's timestamp covers them.
public class RecipeParagraphConfiguration : IEntityTypeConfiguration<RecipeParagraph>
{
    public void Configure(EntityTypeBuilder<RecipeParagraph> e)
    { e.ToTable("recipe_paragraphs"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.RecipeId, x.SortOrder }); }
}
public class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> e)
    { e.ToTable("recipe_ingredients"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.RecipeId, x.SortOrder }); e.Property(x => x.IngredientKey).HasMaxLength(64); }
}
public class CookingStepConfiguration : IEntityTypeConfiguration<CookingStep>
{
    public void Configure(EntityTypeBuilder<CookingStep> e)
    { e.ToTable("cooking_steps"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.RecipeId, x.Number }).IsUnique(); }
}
public class MethodParagraphConfiguration : IEntityTypeConfiguration<MethodParagraph>
{
    public void Configure(EntityTypeBuilder<MethodParagraph> e)
    { e.ToTable("method_paragraphs"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.MethodNarrativeId, x.SortOrder }); }
}
public class RegionalVariationConfiguration : IEntityTypeConfiguration<RegionalVariation>
{
    public void Configure(EntityTypeBuilder<RegionalVariation> e)
    { e.ToTable("regional_variations"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.RecipeId, x.SortOrder }); }
}
public class IngredientLocalNameConfiguration : IEntityTypeConfiguration<IngredientLocalName>
{
    public void Configure(EntityTypeBuilder<IngredientLocalName> e)
    { e.ToTable("ingredient_local_names"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.IngredientKey, x.SortOrder }); }
}
public class IngredientUsageConfiguration : IEntityTypeConfiguration<IngredientUsage>
{
    public void Configure(EntityTypeBuilder<IngredientUsage> e)
    { e.ToTable("ingredient_usages"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.IngredientKey, x.SortOrder }); e.Property(x => x.DishId).HasMaxLength(64); }
}
public class ProvinceFoodConfiguration : IEntityTypeConfiguration<ProvinceFood>
{
    public void Configure(EntityTypeBuilder<ProvinceFood> e)
    { e.ToTable("province_foods"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ProvinceName, x.SortOrder }); }
}
public class ProvinceIngredientConfiguration : IEntityTypeConfiguration<ProvinceIngredient>
{
    public void Configure(EntityTypeBuilder<ProvinceIngredient> e)
    { e.ToTable("province_ingredients"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ProvinceName, x.SortOrder }); }
}
public class ArticleBlockConfiguration : IEntityTypeConfiguration<ArticleBlock>
{
    public void Configure(EntityTypeBuilder<ArticleBlock> e)
    { e.ToTable("article_blocks"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ArticleId, x.SortOrder }); }
}
public class ArticleRelatedDishConfiguration : IEntityTypeConfiguration<ArticleRelatedDish>
{
    public void Configure(EntityTypeBuilder<ArticleRelatedDish> e)
    { e.ToTable("article_related_dishes"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ArticleId, x.SortOrder }); e.Property(x => x.DishId).HasMaxLength(64); }
}
