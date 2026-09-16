using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data.Seed;

namespace TasteZambia.API.Tests.Data;

[Collection(nameof(DatabaseCollection))]
public class SeederTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Seed_LoadsTheWholeArchive()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);

        Assert.Equal(8, await db.Dishes.CountAsync());
        Assert.Equal(9, await db.Ingredients.CountAsync());
        Assert.Equal(10, await db.Provinces.CountAsync());
        Assert.Equal(8, await db.Categories.CountAsync());
        Assert.Equal(6, await db.Articles.CountAsync());
    }

    [Fact]
    public async Task Seed_IsIdempotent()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);

        Assert.Equal(8, await db.Dishes.CountAsync());
    }

    [Fact]
    public async Task Ifisashi_HasItsFullRecipe()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);

        var recipe = await db.Recipes
            .Include(r => r.Ingredients).Include(r => r.Steps)
            .Include(r => r.Methods).ThenInclude(m => m.Paragraphs)
            .Include(r => r.Variations).Include(r => r.CulturalContext)
            .SingleAsync(r => r.DishId == "ifisashi");

        Assert.Equal(6, recipe.Ingredients.Count);
        Assert.Equal(4, recipe.Steps.Count);
        Assert.Equal(4, recipe.Variations.Count);
        Assert.Equal(3, recipe.CulturalContext.Count);
        Assert.Equal(2, recipe.Methods.Count);
        Assert.All(recipe.Methods, m => Assert.Equal(3, m.Paragraphs.Count));
        Assert.True(recipe.IsVerified);
    }

    [Fact]
    public async Task FiveDishesStillAwaitPhotographyAndSayWhatIsNeeded()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);

        var unphotographed = await db.Dishes.Where(d => d.ImageAsset == null).ToListAsync();

        Assert.Equal(5, unphotographed.Count);
        Assert.All(unphotographed, d => Assert.StartsWith("photo:", d.PhotoNeededCaption));
    }

    [Fact]
    public async Task Chibwabwa_LinksToThreeDishesInArchiveOrder()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);

        var usages = await db.Ingredients
            .Where(i => i.Key == "chibwabwa")
            .SelectMany(i => i.Usages.OrderBy(u => u.SortOrder))
            .Select(u => u.DishId)
            .ToListAsync();

        Assert.Equal(["ifisashi", "nshima", "delele"], usages);
    }

    [Fact]
    public async Task NshimaEssay_HasSixBlocksAndAudio()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);

        var essay = await db.Articles.Include(a => a.Body).Include(a => a.RelatedDishes)
            .SingleAsync(a => a.Id == "nshima");

        Assert.True(essay.IsLead);
        Assert.Equal(6, essay.Body.Count);
        Assert.Equal("Listen in Bemba", essay.AudioLabel);
        Assert.Single(essay.RelatedDishes);
    }
}
