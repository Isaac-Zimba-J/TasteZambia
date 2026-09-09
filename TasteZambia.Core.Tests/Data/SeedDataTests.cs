using TasteZambia.Core.Data;

namespace TasteZambia.Core.Tests.Data;

public class SeedDataTests
{
    [Fact]
    public async Task DishRepository_ReturnsTheEightSeededDishes()
    {
        var repo = new InMemoryDishRepository();
        var dishes = await repo.GetAllAsync(CancellationToken.None);
        Assert.Equal(8, dishes.Count);
        Assert.Equal("ifisashi", dishes[0].Id);
    }

    [Fact]
    public async Task DishRepository_ReturnsFullRecipeForIfisashi()
    {
        var repo = new InMemoryDishRepository();
        var recipe = await repo.GetRecipeAsync("ifisashi", CancellationToken.None);

        Assert.NotNull(recipe);
        Assert.Equal(6, recipe!.Ingredients.Count);
        Assert.Equal(4, recipe.Steps.Count);
        Assert.Equal(4, recipe.Variations.Count);
        Assert.Equal(3, recipe.CulturalContext.Count);
        Assert.True(recipe.IsVerified);
    }

    [Fact]
    public async Task IngredientRepository_ChibwabwaLinksToThreeDishes()
    {
        var repo = new InMemoryIngredientRepository();
        var chibwabwa = await repo.GetByKeyAsync("chibwabwa", CancellationToken.None);

        Assert.NotNull(chibwabwa);
        Assert.Equal("Pumpkin leaves", chibwabwa!.EnglishName);
        Assert.Equal(3, chibwabwa.UsedInDishIds.Count);
        Assert.Contains("Tonga", chibwabwa.PendingLanguages);
    }

    [Fact]
    public async Task RegionRepository_ReturnsTenProvincesWithNorthernAtIndexSix()
    {
        var repo = new InMemoryRegionRepository();
        var provinces = await repo.GetAllAsync(CancellationToken.None);

        Assert.Equal(10, provinces.Count);
        Assert.Equal("Northern", provinces[6].Name);
        Assert.Equal("Kasama", provinces[6].Seat);
    }

    [Fact]
    public async Task DishesWithoutPhotography_CarryTheirOwnPlaceholderCaption()
    {
        var repo = new InMemoryDishRepository();
        var dishes = await repo.GetAllAsync(CancellationToken.None);
        var unphotographed = dishes.Where(d => !d.HasPhoto).ToList();

        Assert.Equal(5, unphotographed.Count);
        Assert.All(unphotographed, d => Assert.StartsWith("photo:", d.PhotoNeededCaption));
    }
}
