using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

/// <summary>
/// Home's search box reaches the whole archive, not only its recipes. These pin what a reader
/// gets back for each kind of thing, and where tapping it goes.
/// </summary>
public class ArchiveSearchTests
{
    private static readonly ArchiveSearchService Sut = TestServices.Search();

    [Fact]
    public async Task ADishIsFoundByItsLocalName()
    {
        var hits = await Sut.SearchAsync("Chikanda");

        var dish = hits.Single(h => h.Kind == SearchHitKind.Dish && h.Title == "Chikanda");
        Assert.Equal("Wild orchid cake", dish.Subtitle);
        Assert.Equal("recipe", dish.Route);
        Assert.Equal("dishId", dish.ParameterName);   // the recipe route's parameter, not "id"
        Assert.Equal("chikanda", dish.ParameterValue);
    }

    [Fact]
    public async Task ADishIsFoundByItsEnglishName()
    {
        var hits = await Sut.SearchAsync("Okra relish");

        Assert.Contains(hits, h => h.Kind == SearchHitKind.Dish && h.Title == "Delele");
    }

    [Fact]
    public async Task AnIngredientIsFoundAndOpensTheIngredientScreen()
    {
        var hits = await Sut.SearchAsync("Pumpkin leaves");

        var ingredient = hits.Single(h => h.Kind == SearchHitKind.Ingredient && h.Title == "Chibwabwa");
        Assert.Equal("ingredient", ingredient.Route);
        Assert.Equal("key", ingredient.ParameterName);
        Assert.Equal("chibwabwa", ingredient.ParameterValue);
    }

    [Fact]
    public async Task AStoryIsFoundByItsTitle()
    {
        var hits = await Sut.SearchAsync("Why Groundnuts Anchor");

        var story = hits.Single(h => h.Kind == SearchHitKind.Story);
        Assert.Equal("story", story.Route);
        Assert.Equal("articleId", story.ParameterName);
        Assert.Equal("groundnuts", story.ParameterValue);
    }

    [Fact]
    public async Task AProvinceIsFoundByNameAndByItsSeat()
    {
        var byName = await Sut.SearchAsync("Luapula");
        var province = byName.Single(h => h.Kind == SearchHitKind.Province);
        Assert.Equal("Luapula Province", province.Title);
        Assert.Equal("regions", province.Route);
        Assert.Empty(province.ParameterName);   // the province tab takes no parameter

        var bySeat = await Sut.SearchAsync("Kabwe");
        Assert.Contains(bySeat, h => h.Kind == SearchHitKind.Province && h.Title == "Central Province");
    }

    [Fact]
    public async Task OneQueryCanReachSeveralKindsAtOnce()
    {
        // "Kapenta" is a dish, an ingredient, and part of a province's larder.
        var kinds = (await Sut.SearchAsync("kapenta")).Select(h => h.Kind).Distinct().ToList();

        Assert.Contains(SearchHitKind.Dish, kinds);
        Assert.Contains(SearchHitKind.Ingredient, kinds);
    }

    [Fact]
    public async Task TheMatchIgnoresCase()
    {
        Assert.Contains(await Sut.SearchAsync("NSHIMA"), h => h.Title == "Nshima");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("n")]
    public async Task AQueryTooShortToMeanAnythingReturnsNothing(string query)
        => Assert.Empty(await Sut.SearchAsync(query));

    [Fact]
    public async Task AQueryTheArchiveDoesNotHoldReturnsNothing()
        => Assert.Empty(await Sut.SearchAsync("tiramisu"));

    [Fact]
    public async Task EveryHitCarriesALabelTheListCanShow()
    {
        var hits = await Sut.SearchAsync("kapenta");

        Assert.All(hits, h => Assert.False(string.IsNullOrWhiteSpace(h.KindLabel)));
        Assert.Equal("Recipe", hits.First(h => h.Kind == SearchHitKind.Dish).KindLabel);
        Assert.Equal("Ingredient", hits.First(h => h.Kind == SearchHitKind.Ingredient).KindLabel);
    }
}
