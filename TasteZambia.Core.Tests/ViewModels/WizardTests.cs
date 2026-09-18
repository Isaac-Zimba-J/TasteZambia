using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.Tests.Fakes;
using TasteZambia.Core.ViewModels;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public int BackCount { get; private set; }
    public Task GoToAsync(string r) => Task.CompletedTask;
    public Task GoToAsync(string r, IDictionary<string, object> p) => Task.CompletedTask;
    public Task GoBackAsync() { BackCount++; return Task.CompletedTask; }
}

public class ShareViewModelTests
{
    private static ShareViewModel Sut() => new(TestServices.Contributions(), new Nav());

    [Fact]
    public async Task StartsOnStepOneWithNoBackButton()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(1, vm.Step);
        Assert.Equal("Recipe basics", vm.StepTitle);
        Assert.True(vm.IsStep1);
        Assert.False(vm.CanGoBack);
        Assert.Equal("Continue", vm.NextLabel);
        Assert.True(vm.IsPending);
    }

    [Fact]
    public async Task DraftIsPreFilledFromTheDesignWalkthrough()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal("Chibwabwa na Mbalala", vm.Draft.LocalName);
        Assert.Equal("Northern", vm.Draft.Province);
        Assert.Equal(3, vm.Draft.Ingredients.Count);
        Assert.Equal(2, vm.Draft.Steps.Count);
    }

    [Fact]
    public async Task StepFour_ShowsThePipelineAndTheSubmitLabel()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        for (var i = 0; i < 3; i++)
            await vm.NextCommand.ExecuteAsync(null);

        Assert.Equal(4, vm.Step);
        Assert.True(vm.IsStep4);
        Assert.Equal("Submit for review", vm.NextLabel);
        Assert.Equal(4, vm.Pipeline.Count);
        Assert.True(vm.Pipeline[0].IsComplete);
        Assert.False(vm.Pipeline[2].IsComplete);
    }

    [Fact]
    public async Task SubmittingFromStepFour_SwitchesToTheConfirmation()
    {
        var fake = new FakeContributionService();
        var vm = new ShareViewModel(fake, new Nav());
        await vm.InitializeAsync();

        for (var i = 0; i < 4; i++)
            await vm.NextCommand.ExecuteAsync(null);

        Assert.True(vm.IsSubmitted);
        Assert.False(vm.IsPending);
        Assert.False(vm.IsStep4);
        Assert.Equal("Chibwabwa na Mbalala", vm.SubmittedName);
        Assert.Equal("Northern Province · submitted just now", vm.SubmittedMeta);
        Assert.NotNull(vm.SubmittedId);
        Assert.Single(fake.Submitted);
    }

    [Fact]
    public async Task EveryStepForward_SavesTheDraftLocally()
    {
        var svc = TestServices.Contributions();
        var vm = new ShareViewModel(svc, new Nav());
        await vm.InitializeAsync();

        await vm.NextCommand.ExecuteAsync(null);

        var saved = Assert.Single(svc.Drafts);
        Assert.Equal("Chibwabwa na Mbalala", saved.Name);
        Assert.Equal(saved.Id, vm.DraftId);
    }

    [Fact]
    public async Task SubmittingOffline_KeepsTheDraftAndSaysSo()
    {
        var svc = TestServices.Contributions();   // no network
        var vm = new ShareViewModel(svc, new Nav());
        await vm.InitializeAsync();

        for (var i = 0; i < 4; i++)
            await vm.NextCommand.ExecuteAsync(null);

        Assert.False(vm.IsSubmitted);
        Assert.True(vm.IsStep4);
        Assert.Contains("draft is saved", vm.SubmitError);
        Assert.Single(svc.Drafts);
    }

    [Fact]
    public async Task ContinuingADraft_LoadsItInsteadOfTheWalkthrough()
    {
        var svc = TestServices.Contributions();
        var local = svc.SaveDraft(new ContributionDraft { LocalName = "Munkoyo", Province = "Central" });
        var vm = new ShareViewModel(svc, new Nav()) { DraftId = local.Id };

        await vm.InitializeAsync();

        Assert.Equal("Munkoyo", vm.Draft.LocalName);
    }

    [Fact]
    public async Task Reset_ReturnsToStepOne()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        for (var i = 0; i < 4; i++)
            await vm.NextCommand.ExecuteAsync(null);

        vm.ResetCommand.Execute(null);

        Assert.False(vm.IsSubmitted);
        Assert.Equal(1, vm.Step);
    }

    [Fact]
    public async Task Back_NeverGoesBelowStepOne()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        vm.BackCommand.Execute(null);

        Assert.Equal(1, vm.Step);
    }
}

public class FamilyViewModelTests
{
    private static FamilyViewModel Sut() => new(TestServices.Contributions(), new Nav());

    [Fact]
    public async Task StartsOnTheRecipeStepWithThePreFilledDraft()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        Assert.Equal(1, vm.Step);
        Assert.Equal("The recipe", vm.StepTitle);
        Assert.Equal("Ifisashi ya Banakulu", vm.Draft.LocalName);
        Assert.Equal("Bemba", vm.Draft.Language);
        Assert.False(vm.CanGoBack);
    }

    [Fact]
    public async Task StepTitlesFollowTheDesign()
    {
        var vm = Sut();
        await vm.InitializeAsync();

        var titles = new List<string> { vm.StepTitle };
        for (var i = 0; i < 3; i++)
        {
            vm.NextCommand.Execute(null);
            titles.Add(vm.StepTitle);
        }

        Assert.Equal(["The recipe", "Who taught you", "The story", "Who can see it"], titles);
    }

    [Fact]
    public async Task FinalStepOffersThreePrivacyLevelsWithFamilySelected()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        for (var i = 0; i < 3; i++) vm.NextCommand.Execute(null);

        Assert.True(vm.IsStep4);
        Assert.Equal("Save to the archive", vm.NextLabel);
        Assert.Equal(3, vm.PrivacyOptions.Count);
        Assert.True(vm.PrivacyOptions[1].IsSelected);
        Assert.Equal(PrivacyLevel.SharedWithFamily, vm.Draft.Privacy);
    }

    [Fact]
    public async Task ChoosingAPrivacyLevel_UpdatesTheDraftAndMovesTheSelection()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        for (var i = 0; i < 3; i++) vm.NextCommand.Execute(null);

        vm.PrivacyOptions[2].SelectCommand.Execute(null);

        Assert.True(vm.PrivacyOptions[2].IsSelected);
        Assert.False(vm.PrivacyOptions[1].IsSelected);
        Assert.Equal(PrivacyLevel.PublicInArchive, vm.Draft.Privacy);
    }

    [Fact]
    public async Task NextOnTheFinalStep_DoesNotAdvancePastFour()
    {
        var vm = Sut();
        await vm.InitializeAsync();
        for (var i = 0; i < 6; i++) vm.NextCommand.Execute(null);

        Assert.Equal(4, vm.Step);
    }
}
