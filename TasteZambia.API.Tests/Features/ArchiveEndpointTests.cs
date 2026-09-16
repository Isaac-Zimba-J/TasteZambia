using System.Net;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Common;
using TasteZambia.Shared.Contracts.Culture;
using TasteZambia.Shared.Contracts.Dishes;
using TasteZambia.Shared.Contracts.Ingredients;
using TasteZambia.Shared.Contracts.Regions;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Features;

[Collection(nameof(DatabaseCollection))]
public class ArchiveEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
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
    public async Task GetDishes_ReturnsEightInArchiveOrder()
    {
        var dishes = await _client.GetFromJsonAsync<List<DishDto>>(ApiRoutes.Dishes.Collection);

        Assert.NotNull(dishes);
        Assert.Equal(8, dishes!.Count);
        Assert.Equal("ifisashi", dishes[0].Id);
        Assert.Equal("Ifisashi", dishes[0].LocalName);
    }

    [Fact]
    public async Task GetDishes_SendsAnETagAndHonoursIfNoneMatch()
    {
        var first = await _client.GetAsync(ApiRoutes.Dishes.Collection);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.NotNull(first.Headers.ETag);

        var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Dishes.Collection);
        request.Headers.IfNoneMatch.Add(first.Headers.ETag!);
        var second = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
    }

    [Fact]
    public async Task GetDishById_ReturnsTheDish()
    {
        var dish = await _client.GetFromJsonAsync<DishDto>("/api/v1/dishes/chikanda");
        Assert.Equal("Wild orchid cake", dish!.EnglishName);
    }

    [Fact]
    public async Task GetRecipe_ReturnsTheFullIfisashiRecipe()
    {
        var recipe = await _client.GetFromJsonAsync<RecipeDto>("/api/v1/dishes/ifisashi/recipe");

        Assert.NotNull(recipe);
        Assert.Equal("Traditional Zambian vegetable dish", recipe!.Subtitle);
        Assert.True(recipe.IsVerified);
        Assert.Equal(6, recipe.Ingredients.Count);
        Assert.Equal(4, recipe.Steps.Count);
        Assert.Equal(3, recipe.CulturalContext.Count);
        Assert.Equal("Over charcoal, in a clay pot", recipe.TraditionalMethod.Heading);
        Assert.Equal("In a flat you rent abroad", recipe.ModernMethod.Heading);
        Assert.Equal("Chanda M.", recipe.Contributor.Name);
        Assert.Equal("chibwabwa", recipe.Ingredients[0].IngredientKey);
        Assert.Null(recipe.Ingredients[4].IngredientKey);   // Salt
    }

    [Fact]
    public async Task GetRecipe_ForADishWithoutOne_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/dishes/kapenta/recipe");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SearchDishes_NarrowsAndIsCaseInsensitive()
    {
        var results = await _client.GetFromJsonAsync<List<DishDto>>($"{ApiRoutes.Dishes.Search}?q=ORCHID");
        Assert.Single(results!);
        Assert.Equal("chikanda", results![0].Id);
    }

    [Fact]
    public async Task SearchDishes_WithNoMatch_ReturnsEmptyNot404()
    {
        var response = await _client.GetAsync($"{ApiRoutes.Dishes.Search}?q=sushi");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<List<DishDto>>())!);
    }

    [Fact]
    public async Task GetIngredient_CarriesLocalNamesAndUsagesInOrder()
    {
        var ingredient = await _client.GetFromJsonAsync<IngredientDto>("/api/v1/ingredients/chibwabwa");

        Assert.NotNull(ingredient);
        Assert.Equal("Pumpkin leaves", ingredient!.EnglishName);
        Assert.Single(ingredient.LocalNames);
        Assert.Equal("Bemba, Nyanja", ingredient.LocalNames[0].Language);
        Assert.Equal(["ifisashi", "nshima", "delele"], ingredient.UsedInDishIds);
        Assert.Contains("Tonga", ingredient.PendingLanguages);
    }

    [Fact]
    public async Task GetIngredients_ReturnsNine()
    {
        var all = await _client.GetFromJsonAsync<List<IngredientDto>>(ApiRoutes.Ingredients.Collection);
        Assert.Equal(9, all!.Count);
    }

    [Fact]
    public async Task GetIngredient_Unknown_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/ingredients/nope");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRegions_ReturnsTenWithNorthernAtIndexSix()
    {
        var provinces = await _client.GetFromJsonAsync<List<ProvinceDto>>(ApiRoutes.Regions.Collection);

        Assert.Equal(10, provinces!.Count);
        Assert.Equal("Northern", provinces[6].Name);
        Assert.Equal("Kasama", provinces[6].Seat);
        Assert.Equal(3, provinces[6].SignatureFoods.Count);
    }

    [Fact]
    public async Task GetArticles_ListsSixWithoutBodies()
    {
        var articles = await _client.GetFromJsonAsync<List<ArticleDto>>(ApiRoutes.Culture.Articles);
        Assert.Equal(6, articles!.Count);
        Assert.All(articles, a => Assert.Empty(a.Body));
    }

    [Fact]
    public async Task GetArticle_ReturnsBodyBlocksAndAudio()
    {
        var article = await _client.GetFromJsonAsync<ArticleDto>("/api/v1/articles/nshima");

        Assert.NotNull(article);
        Assert.True(article!.IsLead);
        Assert.Equal(6, article.Body.Count);
        Assert.Equal(ArticleBlockKind.PullQuote, article.Body[3].Kind);
        Assert.NotNull(article.Audio);
        Assert.Equal("Listen in Bemba", article.Audio!.Label);
        Assert.Equal(["nshima"], article.RelatedDishIds);
    }

    [Fact]
    public async Task GetCategories_ReturnsEightInOrder()
    {
        var cats = await _client.GetFromJsonAsync<List<CategoryDto>>(ApiRoutes.Categories.Collection);
        Assert.Equal(8, cats!.Count);
        Assert.Equal("Traditional Meals", cats[0].Name);
        Assert.Equal(7, cats[7].Order);
    }
}
