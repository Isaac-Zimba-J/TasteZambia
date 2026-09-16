using TasteZambia.Core.Models;
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

public class ShareLifecycleTests
{
    [Fact]
    public async Task ShareStart_ShowsBothRoutesAndTheRecordCounts()
    {
        var nav = new Nav();
        var vm = new ShareStartViewModel(new ContributionService(), nav);
        await vm.InitializeAsync();

        Assert.Equal(1, vm.PublishedCount);
        Assert.Equal(1, vm.InReviewCount);
        Assert.Equal(4, vm.PreservedCount);
        Assert.Equal("3 drafts in progress", vm.DraftsLabel);

        vm.GoShareCommand.Execute(null);
        vm.GoPreserveCommand.Execute(null);
        vm.GoDraftsCommand.Execute(null);

        Assert.Equal(["share", "famStart", "shareDraft"], nav.Routes);
    }

    [Fact]
    public async Task Drafts_AreOrderedMostCompleteFirstWithTheirTints()
    {
        var vm = new DraftsViewModel(new ContributionService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(3, vm.Drafts.Count);
        Assert.Equal("Chibwabwa na Mbalala", vm.Drafts[0].Name);
        Assert.Equal(85, vm.Drafts[0].PercentComplete);
        Assert.Equal("85%", vm.Drafts[0].PercentLabel);
        Assert.Equal(0.85, vm.Drafts[0].Fraction, 3);
        Assert.Equal("#2F6A4D", vm.Drafts[0].TintHex);
        Assert.Equal("#A3452A", vm.Drafts[2].TintHex);
    }

    [Fact]
    public async Task ReviewTimeline_HasOneInProgressAndOnePending()
    {
        var vm = new ShareReviewViewModel(new ContributionService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(4, vm.Timeline.Count);
        Assert.Equal(ReviewState.Done, vm.Timeline[0].State);
        Assert.Equal(ReviewState.InProgress, vm.Timeline[2].State);
        Assert.Equal(ReviewState.Pending, vm.Timeline[3].State);
        Assert.True(vm.Timeline[2].IsNotLast);
        Assert.False(vm.Timeline[3].IsNotLast);
        Assert.Equal("#C07F1E", vm.Timeline[2].DotHex);
        Assert.Equal("#D8CDB9", vm.Timeline[3].DotHex);
        Assert.Equal("#7A6B59", vm.Timeline[3].LabelHex);
        Assert.Equal("Namakau Sitali", vm.ReviewerName);
    }

    [Fact]
    public async Task ChangesRequested_CarriesTwoFieldQuestions()
    {
        var vm = new ShareChangesViewModel(new ContributionService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(2, vm.Flagged.Count);
        Assert.Equal("Local name", vm.Flagged[0].Field);
        Assert.Contains("Mungwi", vm.Flagged[0].Question);
        Assert.Equal("Chibwabwa na Mbalala", vm.Flagged[0].CurrentValue);
        Assert.Equal("Cooking step 2", vm.Flagged[1].Field);
    }

    [Fact]
    public void Published_ShowsCreditAndReach()
    {
        var vm = new SharePublishedViewModel(new Nav());

        Assert.Equal("Chibwabwa na Mbalala", vm.DishName);
        Assert.Equal(318, vm.OpenedCount);
        Assert.Equal(64, vm.SavedCount);
        Assert.Equal(11, vm.CookedCount);
        Assert.Contains("Banakulu Mwaba", vm.Credit);
    }
}
