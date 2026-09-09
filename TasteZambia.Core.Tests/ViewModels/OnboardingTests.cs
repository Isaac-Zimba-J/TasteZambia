using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public List<string> Routes { get; } = [];
    public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoBackAsync() => Task.CompletedTask;
}

public class OnboardingServiceTests
{
    [Fact]
    public void SixReadingLanguages_EnglishFirst()
    {
        var sut = new OnboardingService();
        Assert.Equal(6, sut.Languages.Count);
        Assert.Equal("English", sut.Languages[0].Name);
        Assert.Equal("Full interface", sut.Languages[0].Note);
        Assert.Equal("Icibemba", sut.Languages[1].Note);
    }

    [Fact]
    public void FourVisitorKindsAndSixTastes()
    {
        var sut = new OnboardingService();
        Assert.Equal(4, sut.VisitorKinds.Count);
        Assert.Equal("I grew up here", sut.VisitorKinds[0].Key);
        Assert.Equal("I am learning to cook", sut.VisitorKinds[3].Key);
        Assert.Equal(6, sut.Tastes.Count);
    }

    [Fact]
    public void Defaults_MatchTheDesignsInitialState()
    {
        var c = new OnboardingService().Current;
        Assert.Equal("English", c.Language);
        Assert.Equal("I grew up here", c.Who);
        Assert.Equal(["traditional", "veg"], c.Tastes.OrderBy(x => x));
        Assert.True(c.OfflineEnabled);
        Assert.False(c.StoryNotifications);
    }

    [Fact]
    public void IsComplete_FlipsOnlyAfterComplete()
    {
        var sut = new OnboardingService();
        Assert.False(sut.IsComplete);
        sut.Complete(sut.Current);
        Assert.True(sut.IsComplete);
    }
}

public class OnboardingViewModelTests
{
    private static OnboardingViewModel Sut(INavigationService? nav = null, IOnboardingService? svc = null)
        => new(svc ?? new OnboardingService(), nav ?? new Nav());

    [Fact]
    public async Task EnglishAndGrewUpHereAreSelectedInitially()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.True(vm.Languages.Single(l => l.Name == "English").IsSelected);
        Assert.True(vm.VisitorKinds[0].IsSelected);
        Assert.Equal("English", vm.PickedLanguage);
        Assert.Equal("I grew up here", vm.PickedWho);
    }

    [Fact]
    public async Task PickingALanguage_MovesTheSelectionAndTheSummary()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.Languages[2].SelectCommand.Execute(null);

        Assert.True(vm.Languages[2].IsSelected);
        Assert.False(vm.Languages[0].IsSelected);
        Assert.Equal("Nyanja", vm.PickedLanguage);
    }

    [Fact]
    public async Task TasteChips_AreMultiSelectAndCount()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("2 selected", vm.TasteCountLabel);

        vm.TasteChips.Single(t => t.Key == "quick").ToggleCommand.Execute(null);
        Assert.Equal("3 selected", vm.TasteCountLabel);

        vm.TasteChips.Single(t => t.Key == "traditional").ToggleCommand.Execute(null);
        Assert.Equal("2 selected", vm.TasteCountLabel);
    }

    [Fact]
    public async Task Finish_MarksOnboardingCompleteAndPersistsChoices()
    {
        var svc = new OnboardingService();
        var nav = new Nav();
        var vm = Sut(nav, svc);
        await vm.InitializeAsync();
        vm.Languages[1].SelectCommand.Execute(null);

        await vm.FinishCommand.ExecuteAsync(null);

        Assert.True(svc.IsComplete);
        Assert.Equal("Bemba", svc.Current.Language);
        Assert.Equal("//home", nav.Routes.Single());
    }

    [Fact]
    public async Task Skip_AlsoCompletesOnboardingWithTheDefaults()
    {
        var svc = new OnboardingService();
        var vm = Sut(svc: svc);
        await vm.InitializeAsync();

        await vm.SkipCommand.ExecuteAsync(null);

        Assert.True(svc.IsComplete);
        Assert.Equal("English", svc.Current.Language);
    }
}
