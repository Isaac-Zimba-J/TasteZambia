using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class RecordingNavigation : INavigationService
{
    public List<string> Routes { get; } = [];
    public IDictionary<string, object>? LastParameters { get; private set; }
    public Task GoToAsync(string route) { Routes.Add(route); return Task.CompletedTask; }
    public Task GoToAsync(string route, IDictionary<string, object> p) { Routes.Add(route); LastParameters = p; return Task.CompletedTask; }
    public Task GoBackAsync() => Task.CompletedTask;
}

public class HomeViewModelTests
{
    private static HomeViewModel Sut(INavigationService? nav = null, IProfileRepository? profiles = null)
        => TestServices.Home(nav ?? new RecordingNavigation(), profiles: profiles);

    [Fact]
    public async Task Initialize_LoadsCategoriesDishesAndStories()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(8, vm.Categories.Count);
        Assert.Equal(8, vm.Dishes.Count);
        Assert.Equal(3, vm.Stories.Count);
    }

    [Fact]
    public async Task DishItems_PutTheLocalNameInTheTitle()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("Ifisashi", vm.Dishes[0].Title);
        Assert.Equal("Groundnut and greens relish", vm.Dishes[0].Subtitle);
    }

    [Fact]
    public async Task DishesStartUnsaved_AndToggleFlipsTheState()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        var ifisashi = vm.Dishes[0];

        Assert.False(ifisashi.IsSaved);
        Assert.Equal("#57493A", ifisashi.SaveColorHex);

        ifisashi.ToggleSaveCommand.Execute(null);

        Assert.True(ifisashi.IsSaved);
        Assert.Equal("#A3452A", ifisashi.SaveColorHex);
        Assert.Contains("Remove Ifisashi", ifisashi.SaveSemanticLabel);
    }

    [Fact]
    public async Task OpeningADish_NavigatesToTheRecipeRoute()
    {
        var nav = new RecordingNavigation();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        vm.Dishes[0].OpenCommand.Execute(null);

        Assert.Equal("recipe", nav.Routes.Single());
    }

    // ---- The header used to show a stock photograph of a stranger ----

    [Fact]
    public async Task Header_ShowsNeitherAPhotographNorInitials_UntilTheReaderHasAName()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.False(vm.HasProfileName);
        Assert.False(vm.HasAvatar);
        Assert.Empty(vm.Initials);   // the view falls back to a Person icon, not someone else's face
    }

    [Fact]
    public async Task Header_ShowsTheReadersOwnInitials_OnceTheyHaveAName()
    {
        var profiles = new InMemoryProfileRepository();
        await profiles.UpdateAsync("Mwansa Chanda", "Kasama", "Bemba");

        var vm = Sut(profiles: profiles);
        await vm.InitializeAsync();

        Assert.True(vm.HasProfileName);
        Assert.Equal("MC", vm.Initials);
    }

    [Fact]
    public async Task Header_ShowsOneLetter_ForAReaderWithOneName()
    {
        var profiles = new InMemoryProfileRepository();
        await profiles.UpdateAsync("mwansa", "", "");

        var vm = Sut(profiles: profiles);
        await vm.InitializeAsync();

        Assert.Equal("M", vm.Initials);
    }

    [Fact]
    public async Task Header_PicksUpANameSavedSinceTheLastVisit()
    {
        var profiles = new InMemoryProfileRepository();
        var vm = Sut(profiles: profiles);

        await vm.InitializeAsync();
        Assert.Empty(vm.Initials);

        await profiles.UpdateAsync("Bwalya Mulenga", "", "");

        // The rest of Home returns early on a second visit; the reader's own details must not.
        await vm.InitializeAsync();

        Assert.Equal("BM", vm.Initials);
    }

    [Fact]
    public async Task TappingTheAvatar_GoesToTheEditor_WhileThereIsNoName()
    {
        var nav = new RecordingNavigation();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        await vm.OpenProfileCommand.ExecuteAsync(null);

        Assert.Equal("profileEdit", nav.Routes[^1]);   // asking for a name is the useful thing to do
    }

    [Fact]
    public async Task TappingTheAvatar_GoesToTheProfile_OnceThereIsAName()
    {
        var profiles = new InMemoryProfileRepository();
        await profiles.UpdateAsync("Mwansa Chanda", "", "");
        var nav = new RecordingNavigation();
        var vm = Sut(nav, profiles);
        await vm.InitializeAsync();

        await vm.OpenProfileCommand.ExecuteAsync(null);

        Assert.Equal("//profile", nav.Routes[^1]);
    }

    // ---- Search, from the first screen, across the whole archive ----

    [Fact]
    public async Task Typing_SearchesTheWholeArchive_AndTakesTheScreenOver()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Query = "kapenta";
        await vm.WaitForSearchAsync();

        Assert.True(vm.IsSearching);
        Assert.False(vm.HasNoResults);
        Assert.Contains(vm.Results, h => h.Kind == SearchHitKind.Dish);
        Assert.Contains(vm.Results, h => h.Kind == SearchHitKind.Ingredient);
    }

    [Fact]
    public async Task ASingleLetter_IsNotYetASearch()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Query = "k";
        await vm.WaitForSearchAsync();

        Assert.False(vm.IsSearching);   // Home stays on screen
        Assert.Empty(vm.Results);
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public async Task AQueryTheArchiveDoesNotHold_SaysSoRatherThanShowingAnEmptyScreen()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Query = "tiramisu";
        await vm.WaitForSearchAsync();

        Assert.Empty(vm.Results);
        Assert.True(vm.HasNoResults);
    }

    [Fact]
    public async Task OpeningAResult_UsesThatKindsOwnRoute_AndLeavesTheSearch()
    {
        var nav = new RecordingNavigation();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        vm.Query = "Pumpkin leaves";
        await vm.WaitForSearchAsync();
        var ingredient = vm.Results.First(h => h.Kind == SearchHitKind.Ingredient);

        await vm.OpenResultCommand.ExecuteAsync(ingredient);

        Assert.Equal("ingredient", nav.Routes[^1]);
        Assert.Equal("chibwabwa", nav.LastParameters!["key"]);
        Assert.Empty(vm.Query);          // the results are not left up behind the reader
        Assert.False(vm.IsSearching);
    }

    [Fact]
    public async Task OpeningAProvinceResult_GoesToTheTab_WhichTakesNoParameter()
    {
        var nav = new RecordingNavigation();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        vm.Query = "Luapula";
        await vm.WaitForSearchAsync();
        var province = vm.Results.First(h => h.Kind == SearchHitKind.Province);

        await vm.OpenResultCommand.ExecuteAsync(province);

        Assert.Equal("//regions", nav.Routes[^1]);
    }

    [Fact]
    public async Task ClearingTheSearch_PutsHomeBack()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Query = "nshima";
        await vm.WaitForSearchAsync();
        Assert.NotEmpty(vm.Results);

        vm.ClearSearchCommand.Execute(null);
        await vm.WaitForSearchAsync();

        Assert.Empty(vm.Results);
        Assert.False(vm.IsSearching);
        Assert.False(vm.HasNoResults);
        Assert.Equal(8, vm.Dishes.Count);   // the archive Home never went away
    }

    [Fact]
    public async Task ASlowerEarlierQuery_DoesNotOverwriteANewerOnesResults()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Query = "kapenta";
        var stale = vm.WaitForSearchAsync();
        vm.Query = "chikanda";
        await vm.WaitForSearchAsync();
        await stale;

        Assert.All(vm.Results, h => Assert.Contains("hikanda", h.Title + h.Subtitle));
    }
}
