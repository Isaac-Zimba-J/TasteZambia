using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public List<string> Routes { get; } = [];
    public int BackCount { get; private set; }
    public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoBackAsync() { BackCount++; return Task.CompletedTask; }
}

public class CultureViewModelTests
{
    [Fact]
    public async Task LeadIsNshima_AndTheOtherFiveBecomeRows()
    {
        var vm = new CultureViewModel(new InMemoryArticleRepository(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("The History of Nshima", vm.LeadTitle);
        Assert.Equal("The dish at the centre of every Zambian meal is younger than most people assume.", vm.LeadLede);
        Assert.Equal("Dr. Mutale Chileshe · 8 min read · Pan-Zambian", vm.LeadByline);
        Assert.Equal(5, vm.Articles.Count);
        Assert.Equal("groundnuts", vm.Articles[0].Id);
    }

    [Fact]
    public async Task OpeningAnArticle_NavigatesToTheStoryRoute()
    {
        var nav = new Nav();
        var vm = new CultureViewModel(new InMemoryArticleRepository(), nav);
        await vm.InitializeAsync();

        vm.Articles[0].OpenCommand.Execute(null);

        Assert.Equal("story", nav.Routes.Single());
    }

    [Fact]
    public async Task OpeningTheLead_AlsoNavigatesToTheStoryRoute()
    {
        var nav = new Nav();
        var vm = new CultureViewModel(new InMemoryArticleRepository(), nav);
        await vm.InitializeAsync();

        vm.OpenLeadCommand.Execute(null);

        Assert.Equal("story", nav.Routes.Single());
    }
}

public class StoryViewModelTests
{
    private static StoryViewModel Sut(INavigationService nav, string id = "nshima") => new(
        new InMemoryArticleRepository(),
        new CatalogService(new InMemoryDishRepository()),
        TestServices.Favourites(), new PreferenceService(), nav) { ArticleId = id };

    [Fact]
    public async Task Initialize_LoadsTheNshimaEssay()
    {
        var vm = Sut(new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Archive essay · Pan-Zambian", vm.Kicker);
        Assert.Equal("The History of Nshima", vm.TitleText);
        Assert.Equal("Dr. Mutale Chileshe", vm.Author);
        Assert.Equal(6, vm.Blocks.Count);
        Assert.Equal(ArticleBlockKind.Lede, vm.Blocks[0].Kind);
        Assert.Equal(ArticleBlockKind.PullQuote, vm.Blocks[3].Kind);
    }

    [Fact]
    public async Task AudioNarrationIsExposedWithAnEighteenPercentTrack()
    {
        var vm = Sut(new Nav());
        await vm.InitializeAsync();

        Assert.True(vm.HasAudio);
        Assert.Equal("Listen in Bemba", vm.AudioLabel);
        Assert.Equal("12:40", vm.AudioDuration);
        Assert.Equal(0.18, vm.AudioProgress, 3);
    }

    [Fact]
    public async Task RelatedDishesResolveFromTheArticle()
    {
        var vm = Sut(new Nav());
        await vm.InitializeAsync();

        Assert.True(vm.HasRelatedDishes);
        Assert.Single(vm.RelatedDishes);
        Assert.Equal("Nshima", vm.RelatedDishes[0].Title);
    }

    [Fact]
    public async Task AnArticleWithNoBody_LoadsItsMetadataWithoutAudioOrDishes()
    {
        var vm = Sut(new Nav(), "groundnuts");
        await vm.InitializeAsync();

        Assert.Equal("Why Groundnuts Anchor Zambian Cooking", vm.TitleText);
        Assert.Empty(vm.Blocks);
        Assert.False(vm.HasAudio);
        Assert.False(vm.HasRelatedDishes);
    }

    [Fact]
    public async Task Back_PopsTheStack()
    {
        var nav = new Nav();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        await vm.BackCommand.ExecuteAsync(null);

        Assert.Equal(1, nav.BackCount);
    }
}
