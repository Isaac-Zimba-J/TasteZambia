using TasteZambia.Core.Models;

namespace TasteZambia.Core.Tests.Models;

public class DishTests
{
    [Fact]
    public void Dish_WithoutPhoto_ExposesPlaceholderCaption()
    {
        var dish = new Dish
        {
            Id = "kapenta",
            LocalName = "Kapenta",
            EnglishName = "Dried lake sardines",
            Region = "Luapula",
            TimeLabel = "25 min",
            Difficulty = "Easy",
            ImageAsset = null,
            PhotoNeededCaption = "photo: fried kapenta with tomato",
            Description = "Small dried fish."
        };

        Assert.False(dish.HasPhoto);
        Assert.Equal("photo: fried kapenta with tomato", dish.PhotoNeededCaption);
    }

    [Fact]
    public void Dish_WithPhoto_ReportsHasPhoto()
    {
        var dish = new Dish
        {
            Id = "ifisashi",
            LocalName = "Ifisashi",
            EnglishName = "Groundnut and greens relish",
            Region = "Pan-Zambian",
            TimeLabel = "45 min",
            Difficulty = "Easy",
            ImageAsset = "ifisashi.png",
            Description = "Leafy greens simmered in pounded groundnuts."
        };

        Assert.True(dish.HasPhoto);
    }

    [Fact]
    public void MetaLabel_JoinsTimeAndDifficultyWithDoubleSpacedInterpunct()
    {
        var dish = new Dish
        {
            Id = "chikanda", LocalName = "Chikanda", EnglishName = "Wild orchid cake",
            Region = "Northern and Muchinga", TimeLabel = "1 hr 30", Difficulty = "Medium",
            ImageAsset = "chikanda.png", Description = "Ground orchid tubers."
        };

        Assert.Equal("1 hr 30  ·  Medium", dish.MetaLabel);
    }
}
