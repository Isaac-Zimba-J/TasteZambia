using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

public class RecipeViewModelTests
{
    private sealed class StubNavigation : INavigationService
    {
        public List<string> Routes { get; } = [];
        public int BackCount { get; private set; }
        public Task GoToAsync(string route) { Routes.Add(route); return Task.CompletedTask; }
        public Task GoToAsync(string route, IDictionary<string, object> p) { Routes.Add(route); return Task.CompletedTask; }
        public Task GoBackAsync() { BackCount++; return Task.CompletedTask; }
    }

    private static RecipeViewModel Sut(StubNavigation? nav = null,
                                       ICookingProgressService? progress = null) => new(
        new InMemoryDishRepository(),
        new InMemoryIngredientRepository(),
        new FavouritesService(),
        progress ?? new CookingProgressService(),
        new PreferenceService(),
        nav ?? new StubNavigation()) { DishId = "ifisashi" };

    [Fact]
    public async Task Initialize_LoadsTheIfisashiRecipe()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("Ifisashi", vm.Title);
        Assert.Equal("Traditional Zambian vegetable dish", vm.Subtitle);
        Assert.True(vm.IsVerified);
        Assert.Equal("20 min", vm.PrepTime);
        Assert.Equal("30 min", vm.CookTime);
        Assert.Equal(6, vm.Ingredients.Count);
        Assert.Equal("6 items", vm.IngredientCountLabel);
        Assert.Equal(4, vm.Steps.Count);
        Assert.Equal(4, vm.Variations.Count);
        Assert.Equal(3, vm.CulturalContext.Count);
    }

    [Fact]
    public async Task OnlyIngredientsInTheArchiveAreLinked()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.True(vm.Ingredients[0].IsLinked);   // Chibwabwa
        Assert.True(vm.Ingredients[1].IsLinked);   // Mbalala
        Assert.False(vm.Ingredients[2].IsLinked);  // Onion
        Assert.False(vm.Ingredients[4].IsLinked);  // Salt
    }

    [Fact]
    public async Task StepProgressLabel_CountsCompletedSteps()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("0 of 4 steps done", vm.StepProgressLabel);

        vm.Steps[1].ToggleCommand.Execute(null);

        Assert.True(vm.Steps[1].IsDone);
        Assert.Equal("1 of 4 steps done", vm.StepProgressLabel);
    }

    [Fact]
    public async Task MethodSwitcher_DefaultsToTraditionalAndSwaps()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.True(vm.IsTraditional);
        Assert.Equal("Over charcoal, in a clay pot", vm.MethodHeading);
        Assert.Equal(3, vm.MethodParagraphs.Count);

        vm.ShowModernCommand.Execute(null);

        Assert.False(vm.IsTraditional);
        Assert.True(vm.IsModern);
        Assert.Equal("In a flat you rent abroad", vm.MethodHeading);
        Assert.Equal(3, vm.MethodParagraphs.Count);
    }

    [Fact]
    public async Task TappingALinkedIngredient_OpensTheSheet()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        await vm.Ingredients[0].OpenCommand.ExecuteAsync(null);

        Assert.True(vm.IsSheetOpen);
        Assert.Equal("Chibwabwa", vm.Sheet!.Name);
        Assert.Equal("Pumpkin leaves", vm.Sheet.EnglishName);
        Assert.Single(vm.Sheet.LocalNames);
        Assert.Equal(3, vm.Sheet.UsedIn.Count);

        vm.CloseSheetCommand.Execute(null);
        Assert.False(vm.IsSheetOpen);
    }

    [Fact]
    public async Task TappingAPlainIngredient_DoesNotOpenTheSheet()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        await vm.Ingredients[2].OpenCommand.ExecuteAsync(null);   // Onion

        Assert.False(vm.IsSheetOpen);
    }

    [Fact]
    public async Task OpenFullIngredient_ClosesTheSheetAndNavigates()
    {
        var nav = new StubNavigation();
        var vm = Sut(nav);
        await vm.InitializeAsync();
        await vm.Ingredients[0].OpenCommand.ExecuteAsync(null);

        await vm.OpenFullIngredientCommand.ExecuteAsync(null);

        Assert.False(vm.IsSheetOpen);
        Assert.Equal("ingredient", nav.Routes.Single());
    }

    [Fact]
    public async Task Back_PopsTheNavigationStack()
    {
        var nav = new StubNavigation();
        var vm = Sut(nav);
        await vm.InitializeAsync();

        await vm.BackCommand.ExecuteAsync(null);

        Assert.Equal(1, nav.BackCount);
    }

    [Fact]
    public async Task CookProgress_IsSharedWithTheRestOfTheApp()
    {
        var progress = new CookingProgressService();
        progress.Toggle("ifisashi", 3);

        var vm = Sut(progress: progress);
        await vm.InitializeAsync();

        Assert.True(vm.Steps[2].IsDone);
        Assert.Equal("1 of 4 steps done", vm.StepProgressLabel);
    }
}
