using TasteZambia.Core.Data;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public class CatalogServiceTests
{
    private static CatalogService Sut() => new(new InMemoryDishRepository());

    [Fact]
    public async Task EmptyQuery_ReturnsEveryDish()
        => Assert.Equal(8, (await Sut().SearchAsync("", "All")).Count);

    [Fact]
    public async Task Query_MatchesLocalName_CaseInsensitively()
    {
        var results = await Sut().SearchAsync("IFISASHI", "All");
        Assert.Single(results);
        Assert.Equal("ifisashi", results[0].Id);
    }

    [Fact]
    public async Task Query_MatchesEnglishNameRegionAndDescription()
    {
        Assert.Equal("chikanda", (await Sut().SearchAsync("orchid", "All"))[0].Id);
        Assert.Equal("kapenta", (await Sut().SearchAsync("luapula", "All"))[0].Id);
        Assert.Equal("delele", (await Sut().SearchAsync("bicarbonate", "All"))[0].Id);
    }

    [Fact]
    public async Task Query_SurroundedByWhitespace_IsTrimmed()
    {
        var sut = Sut();
        var padded = await sut.SearchAsync("   nshima   ", "All");
        var exact = await sut.SearchAsync("nshima", "All");

        // "nshima" legitimately matches two dishes: Nshima itself, and Ifisashi,
        // whose description reads "Eaten with nshima across the country."
        // Trimming means the padded query behaves identically to the exact one.
        Assert.Equal(exact.Select(d => d.Id), padded.Select(d => d.Id));
        Assert.Equal(2, padded.Count);
    }

    [Fact]
    public async Task Query_WithNoMatch_ReturnsEmpty()
        => Assert.Empty(await Sut().SearchAsync("sushi", "All"));

    [Fact]
    public async Task GetDishesByIds_PreservesTheRequestedOrder()
    {
        var results = await Sut().GetDishesByIdsAsync(["delele", "ifisashi"]);
        Assert.Equal(["delele", "ifisashi"], results.Select(d => d.Id));
    }
}

public class FavouritesServiceTests
{
    [Fact]
    public void IfisashiIsSavedByDefault_ChikandaIsNot()
    {
        var sut = TestServices.Favourites();
        Assert.True(sut.IsSaved("ifisashi"));
        Assert.False(sut.IsSaved("chikanda"));
    }

    [Fact]
    public void Toggle_FlipsStateAndRaisesChangedWithTheDishId()
    {
        var sut = TestServices.Favourites();
        string? raised = null;
        sut.Changed += (_, id) => raised = id;

        sut.Toggle("chikanda");

        Assert.True(sut.IsSaved("chikanda"));
        Assert.Equal("chikanda", raised);
    }

    [Fact]
    public void Toggle_Twice_ReturnsToTheOriginalState()
    {
        var sut = TestServices.Favourites();
        sut.Toggle("nshima");
        sut.Toggle("nshima");
        Assert.False(sut.IsSaved("nshima"));
    }
}

public class CookingProgressServiceTests
{
    [Fact]
    public void NoStepsDoneInitially()
    {
        var sut = TestServices.Progress();
        Assert.False(sut.IsDone("ifisashi", 1));
        Assert.Equal(0, sut.CompletedCount("ifisashi", [1, 2, 3, 4]));
    }

    [Fact]
    public void Toggle_MarksOneStepDoneForOneDishOnly()
    {
        var sut = TestServices.Progress();
        sut.Toggle("ifisashi", 2);

        Assert.True(sut.IsDone("ifisashi", 2));
        Assert.False(sut.IsDone("ifisashi", 1));
        Assert.False(sut.IsDone("chikanda", 2));
        Assert.Equal(1, sut.CompletedCount("ifisashi", [1, 2, 3, 4]));
    }

    [Fact]
    public void Toggle_RaisesChangedWithTheDishId()
    {
        var sut = TestServices.Progress();
        string? raised = null;
        sut.Changed += (_, id) => raised = id;

        sut.Toggle("ifisashi", 1);

        Assert.Equal("ifisashi", raised);
    }
}

public class PreferenceServiceTests
{
    [Fact]
    public void DefaultsToLocalNameAsTitle()
    {
        var sut = new PreferenceService();
        Assert.Equal(TitleLanguage.LocalName, sut.TitleLanguage);

        var name = sut.Resolve("Ifisashi", "Groundnut and greens relish");
        Assert.Equal("Ifisashi", name.Title);
        Assert.Equal("Groundnut and greens relish", name.Subtitle);
    }

    [Fact]
    public void EnglishAsTitle_SwapsTitleAndSubtitle()
    {
        var sut = new PreferenceService { TitleLanguage = TitleLanguage.English };

        var name = sut.Resolve("Ifisashi", "Groundnut and greens relish");
        Assert.Equal("Groundnut and greens relish", name.Title);
        Assert.Equal("Ifisashi", name.Subtitle);
    }

    [Fact]
    public void ChangingTitleLanguage_RaisesChanged()
    {
        var sut = new PreferenceService();
        var raised = false;
        sut.Changed += (_, _) => raised = true;

        sut.TitleLanguage = TitleLanguage.English;

        Assert.True(raised);
    }

    [Fact]
    public void VerificationBadgeIsShownByDefault()
        => Assert.True(new PreferenceService().ShowVerificationBadge);
}
