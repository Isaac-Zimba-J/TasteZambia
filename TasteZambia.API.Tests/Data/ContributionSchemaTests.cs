using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Tests.Data;

[Collection(nameof(DatabaseCollection))]
public class ContributionSchemaTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Contribution_RoundTripsWithChildren_AndDishProvenanceDefaultsToEditorial()
    {
        await using var db = fixture.NewContext();
        var user = new ArchiveUser { UserName = $"device-{Guid.NewGuid():N}" };
        db.Users.Add(user);

        var c = new Contribution
        {
            UserId = user.Id,
            Status = ContributionStatus.InReview,
            LocalName = "Chibwabwa na Mbalala",
            EnglishDescription = "Pumpkin leaves cooked with pounded groundnuts and nothing else",
            Province = "Northern",
            MealType = "Relish",
            ContributorName = "Chanda Mwaba",
            ContributorLocation = "Kitwe",
            SubmittedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Ingredients = { new() { SortOrder = 0, IngredientKey = "chibwabwa", DisplayName = "Chibwabwa", DisplaySubtitle = "Pumpkin leaves", Quantity = "2 bundles" } },
            Steps = { new() { SortOrder = 0, Text = "Shred the leaves fine and rinse them twice." } },
            Events = { new() { Kind = ReviewEventKind.Submitted, At = DateTimeOffset.UtcNow } },
        };
        db.Contributions.Add(c);
        await db.SaveChangesAsync();

        await using var read = fixture.NewContext();
        var back = await read.Contributions
            .Include(x => x.Ingredients).Include(x => x.Steps).Include(x => x.Events).Include(x => x.Flags)
            .SingleAsync(x => x.Id == c.Id);
        Assert.Single(back.Ingredients);
        Assert.Single(back.Steps);
        Assert.Single(back.Events);
        Assert.Empty(back.Flags);

        if (await read.Dishes.AnyAsync())
        {
            var dish = await read.Dishes.FirstAsync();
            Assert.Equal(Provenance.Editorial, dish.Provenance);
            Assert.Null(dish.ContributionId);
        }
    }
}
