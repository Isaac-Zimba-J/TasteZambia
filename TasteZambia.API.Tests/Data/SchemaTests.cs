using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Tests.Data;

public class SchemaTests
{
    private static TasteZambiaDbContext InMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TasteZambiaDbContext>()
            .UseInMemoryDatabase($"schema-{Guid.NewGuid()}")
            .Options;
        return new TasteZambiaDbContext(options);
    }

    [Fact]
    public void Context_ExposesEverySetTheArchiveNeeds()
    {
        using var db = InMemoryContext();

        Assert.NotNull(db.Dishes);
        Assert.NotNull(db.Recipes);
        Assert.NotNull(db.Ingredients);
        Assert.NotNull(db.Provinces);
        Assert.NotNull(db.Categories);
        Assert.NotNull(db.Articles);
    }

    [Fact]
    public void SavingAnEntity_StampsUpdatedAt()
    {
        using var db = InMemoryContext();

        db.Dishes.Add(new Dish
        {
            Id = "ifisashi", LocalName = "Ifisashi", EnglishName = "Groundnut and greens relish",
            Region = "Pan-Zambian", TimeLabel = "45 min", Difficulty = "Easy",
            Description = "Leafy greens simmered in pounded groundnuts.",
        });
        db.SaveChanges();

        var saved = db.Dishes.Single();
        Assert.True(saved.UpdatedAt > DateTimeOffset.MinValue);
    }
}
