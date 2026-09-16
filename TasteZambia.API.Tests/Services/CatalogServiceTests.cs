using TasteZambia.API.Data.Seed;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;

namespace TasteZambia.API.Tests.Services;

/// <summary>
/// These assertions are deliberately identical to the mobile CatalogServiceTests. The same
/// search must behave the same on both sides, or swapping the repository changes what
/// users see.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class CatalogServiceTests(DatabaseFixture fixture)
{
    private async Task<CatalogService> SutAsync()
    {
        var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);
        return new CatalogService(new DishRepository(db));
    }

    [Fact]
    public async Task EmptyQuery_ReturnsEveryDishInArchiveOrder()
    {
        var results = await (await SutAsync()).SearchAsync("", "All");
        Assert.Equal(8, results.Count);
        Assert.Equal("ifisashi", results[0].Id);
    }

    [Fact]
    public async Task Query_MatchesLocalNameCaseInsensitively()
    {
        var results = await (await SutAsync()).SearchAsync("IFISASHI", "All");
        Assert.Single(results);
    }

    [Fact]
    public async Task Query_AlsoMatchesEnglishNameRegionAndDescription()
    {
        var sut = await SutAsync();
        Assert.Equal("chikanda", (await sut.SearchAsync("orchid", "All"))[0].Id);
        Assert.Equal("kapenta", (await sut.SearchAsync("luapula", "All"))[0].Id);
        Assert.Equal("delele", (await sut.SearchAsync("bicarbonate", "All"))[0].Id);
    }

    [Fact]
    public async Task Query_IsTrimmed()
    {
        var sut = await SutAsync();
        var padded = await sut.SearchAsync("   nshima   ", "All");
        var exact = await sut.SearchAsync("nshima", "All");

        // "nshima" legitimately matches two dishes: Nshima itself, and Ifisashi,
        // whose description reads "Eaten with nshima across the country."
        Assert.Equal(exact.Select(d => d.Id), padded.Select(d => d.Id));
        Assert.Equal(2, padded.Count);
    }

    [Fact]
    public async Task Query_WithNoMatch_ReturnsEmpty()
    {
        Assert.Empty(await (await SutAsync()).SearchAsync("sushi", "All"));
    }

    [Fact]
    public async Task ETag_ChangesWhenTheArchiveChanges()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);
        var versions = new ArchiveVersionService(db);

        var before = await versions.GetETagAsync("dishes");
        Assert.StartsWith("\"dishes-", before);

        var dish = await db.Dishes.FindAsync("delele");
        dish!.Description += " ";
        await db.SaveChangesAsync();

        var after = await versions.GetETagAsync("dishes");
        Assert.NotEqual(before, after);
    }
}
