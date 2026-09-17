using System.Text.Json;
using TasteZambia.Shared.Contracts.Dishes;

namespace TasteZambia.API.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DishDto_RoundTripsAsCamelCase()
    {
        var dish = new DishDto
        {
            Id = "ifisashi",
            LocalName = "Ifisashi",
            EnglishName = "Groundnut and greens relish",
            Region = "Pan-Zambian",
            TimeLabel = "45 min",
            Difficulty = "Easy",
            Description = "Leafy greens simmered in pounded groundnuts.",
            ImageAsset = "ifisashi.png",
            PhotoNeededCaption = "photo needed",
        };

        var json = JsonSerializer.Serialize(dish, Web);

        Assert.Contains("\"localName\":\"Ifisashi\"", json);
        Assert.Contains("\"englishName\"", json);

        var back = JsonSerializer.Deserialize<DishDto>(json, Web)!;
        Assert.Equal("ifisashi", back.Id);
        Assert.Equal("Pan-Zambian", back.Region);
    }

    [Fact]
    public void DishDto_WithoutPhoto_SerialisesNullImageAsset()
    {
        var dish = new DishDto
        {
            Id = "kapenta", LocalName = "Kapenta", EnglishName = "Dried lake sardines",
            Region = "Luapula", TimeLabel = "25 min", Difficulty = "Easy",
            Description = "Small dried fish.", ImageAsset = null,
            PhotoNeededCaption = "photo: fried kapenta with tomato",
        };

        var back = JsonSerializer.Deserialize<DishDto>(JsonSerializer.Serialize(dish, Web), Web)!;

        Assert.Null(back.ImageAsset);
        Assert.Equal("photo: fried kapenta with tomato", back.PhotoNeededCaption);
    }

    [Fact]
    public void RecipeDto_NestsEveryCollection()
    {
        var recipe = new RecipeDto
        {
            Dish = new DishDto { Id = "ifisashi", LocalName = "Ifisashi", EnglishName = "x", Region = "x",
                                 TimeLabel = "x", Difficulty = "x", Description = "x" },
            Subtitle = "Traditional Zambian vegetable dish",
            IsVerified = true,
            CulturalContext = ["a", "b"],
            Ingredients = [new() { DisplayName = "Salt", DisplaySubtitle = "Mucele", Quantity = "To taste" }],
            Steps = [new(1, "Prepare", "Strip the leaves.")],
            TraditionalMethod = new("Over charcoal", ["p1"]),
            ModernMethod = new("In a flat", ["p1"]),
            Variations = [new("Copperbelt", "More tomato.")],
            Contributor = new("Chanda M.", "Kitwe", null),
        };

        var back = JsonSerializer.Deserialize<RecipeDto>(JsonSerializer.Serialize(recipe, Web), Web)!;

        Assert.Equal(2, back.CulturalContext.Count);
        Assert.Single(back.Ingredients);
        Assert.Null(back.Ingredients[0].IngredientKey);
        Assert.Equal(1, back.Steps[0].Number);
        Assert.Equal("Over charcoal", back.TraditionalMethod.Heading);
        Assert.Equal("Chanda M.", back.Contributor.Name);
    }
}
