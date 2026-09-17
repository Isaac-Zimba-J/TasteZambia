using TasteZambia.Core.Data;
using TasteZambia.Core.Data.Http;

namespace TasteZambia.API.Tests.Features;

/// <summary>
/// The whole point of Stage 1: the mobile app swaps InMemory* repositories for Http*
/// ones and nothing above the repository layer changes. These tests drive both
/// implementations of each interface - one over seeded data, one over the live API -
/// and assert they return the same models. If they match, the app cannot tell the
/// difference, and no ViewModel test needs to run against HTTP.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class RepositoryParityTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() { _client.Dispose(); _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Dishes_MatchTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryDishRepository().GetAllAsync();
        var http = await new HttpDishRepository(_client).GetAllAsync();

        Assert.Equal(seeded, http);   // Dish is a record: value equality on every field
    }

    [Fact]
    public async Task Recipe_MatchesTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryDishRepository().GetRecipeAsync("ifisashi");
        var http = await new HttpDishRepository(_client).GetRecipeAsync("ifisashi");

        Assert.NotNull(http);
        Assert.Equal(seeded!.Subtitle, http!.Subtitle);
        Assert.Equal(seeded.Dish, http.Dish);
        Assert.Equal(seeded.CulturalContext, http.CulturalContext);
        Assert.Equal(seeded.Ingredients, http.Ingredients);
        Assert.Equal(seeded.Steps, http.Steps);
        Assert.Equal(seeded.TraditionalMethod.Heading, http.TraditionalMethod.Heading);
        Assert.Equal(seeded.TraditionalMethod.Paragraphs, http.TraditionalMethod.Paragraphs);
        Assert.Equal(seeded.ModernMethod.Paragraphs, http.ModernMethod.Paragraphs);
        Assert.Equal(seeded.Variations, http.Variations);
        Assert.Equal(seeded.Contributor, http.Contributor);
    }

    [Fact]
    public async Task MissingRecipe_IsNullOnBothSides()
    {
        Assert.Null(await new InMemoryDishRepository().GetRecipeAsync("kapenta"));
        Assert.Null(await new HttpDishRepository(_client).GetRecipeAsync("kapenta"));
    }

    [Fact]
    public async Task Ingredients_MatchTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryIngredientRepository().GetAllAsync();
        var http = await new HttpIngredientRepository(_client).GetAllAsync();

        Assert.Equal(seeded.Count, http.Count);
        foreach (var (s, h) in seeded.Zip(http))
        {
            Assert.Equal(s.Key, h.Key);
            Assert.Equal(s.LocalNames, h.LocalNames);
            Assert.Equal(s.UsedInDishIds, h.UsedInDishIds);
            Assert.Equal(s.PendingLanguages, h.PendingLanguages);
            Assert.Equal(s.Description, h.Description);
        }
    }

    [Fact]
    public async Task Provinces_MatchTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryRegionRepository().GetAllAsync();
        var http = await new HttpRegionRepository(_client).GetAllAsync();

        Assert.Equal(seeded.Count, http.Count);
        foreach (var (s, h) in seeded.Zip(http))
        {
            Assert.Equal(s.Name, h.Name);
            Assert.Equal(s.SignatureFoods, h.SignatureFoods);
            Assert.Equal(s.CommonIngredients, h.CommonIngredients);
            Assert.Equal(s.CookingTradition, h.CookingTradition);
        }
    }

    [Fact]
    public async Task Articles_MatchTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryArticleRepository().GetByIdAsync("nshima");
        var http = await new HttpArticleRepository(_client).GetByIdAsync("nshima");

        Assert.NotNull(http);
        Assert.Equal(seeded!.Body, http!.Body);
        Assert.Equal(seeded.Audio, http.Audio);
        Assert.Equal(seeded.RelatedDishIds, http.RelatedDishIds);
    }

    [Fact]
    public async Task Categories_MatchTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryCategoryRepository().GetAllAsync();
        var http = await new HttpCategoryRepository(_client).GetAllAsync();

        Assert.Equal(seeded, http);
    }
}
