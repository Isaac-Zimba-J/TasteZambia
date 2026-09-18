using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public List<string> Routes { get; } = [];
    public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoBackAsync() => Task.CompletedTask;
}

public class FamilyLifecycleTests
{
    [Fact]
    public void FamStart_ListsTheFourThingsYouWillBeAsked()
    {
        var nav = new Nav();
        var vm = new FamStartViewModel(nav);

        Assert.Equal(4, vm.Steps.Count);
        Assert.Equal("Who can see it", vm.Steps[3].StepTitle);

        vm.BeginCommand.Execute(null);
        Assert.Equal("family", nav.Routes.Single());
    }

    [Fact]
    public async Task Draft_IsSeventyTwoPercentWithTwoItemsOutstanding()
    {
        var vm = new FamDraftViewModel(new FamilyArchiveService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(72, vm.PercentComplete);
        Assert.Equal(0.72, vm.Fraction, 3);
        Assert.Equal("72% complete · 2 things left", vm.ProgressLabel);
        Assert.Equal(4, vm.Checklist.Count);
        Assert.True(vm.Checklist[0].IsDone);
        Assert.False(vm.Checklist[2].IsDone);
        Assert.Equal("Cooking steps", vm.Checklist[2].Label);
        Assert.True(vm.Checklist[2].HasDetail);
        Assert.False(vm.Checklist[0].HasDetail);
    }

    [Fact]
    public async Task Draft_CarriesAudioPendingTranscription()
    {
        var vm = new FamDraftViewModel(new FamilyArchiveService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal("Banakulu Mwaba, in Bemba", vm.Recording.Speaker);
        Assert.Equal("12:40", vm.Recording.Duration);
        Assert.False(vm.Recording.TranscriptApproved);
    }

    [Fact]
    public async Task Saved_ListsFourFamilyMembersWithOneStillInvited()
    {
        var vm = new FamSavedViewModel(new FamilyArchiveService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(4, vm.Members.Count);
        Assert.Equal("Owner", vm.Members[0].Status);
        Assert.Equal("Invited", vm.Members[3].Status);
        Assert.Equal("Kaunda Mwaba", vm.Members[3].Name);
        Assert.Equal("#7A5A10", vm.Members[3].BadgeFgHex);
        Assert.Equal(PrivacyLevel.SharedWithFamily, vm.Privacy);
    }

    [Fact]
    public async Task Shared_ShowsApprovedAudioAndTwoFamilyNotes()
    {
        var vm = new FamSharedViewModel(new FamilyArchiveService(), new Nav());
        await vm.InitializeAsync();

        Assert.True(vm.Recording.TranscriptApproved);
        Assert.Equal("FAMILY ONLY · 4 PEOPLE", vm.AccessBadge);
        Assert.Equal(2, vm.Notes.Count);
        Assert.Equal("2 notes", vm.NotesCountLabel);
        Assert.Equal("Aunt Bwalya", vm.Notes[0].Who);
        Assert.Contains("never used tomato", vm.Notes[0].Body);
    }

    [Fact]
    public async Task Public_HasFourProvenanceStepsEndingOnPublished()
    {
        var vm = new FamPublicViewModel(new FamilyArchiveService(), new Nav());
        await vm.InitializeAsync();

        Assert.Equal(4, vm.Provenance.Count);
        Assert.Equal("Preserved privately", vm.Provenance[0].Label);
        Assert.Equal("#2F6A4D", vm.Provenance[0].DotHex);
        Assert.Equal("Published and credited", vm.Provenance[3].Label);
        Assert.Equal("#C07F1E", vm.Provenance[3].DotHex);
        Assert.Contains("cannot be removed", vm.CreditNote);
    }

    [Fact]
    public async Task MakePrivate_WritesThroughToTheSharedService()
    {
        var archive = new FamilyArchiveService();
        var vm = new FamPublicViewModel(archive, new Nav());
        await vm.InitializeAsync();

        vm.MakePrivateCommand.Execute(null);

        Assert.Equal(PrivacyLevel.PrivateToMe, archive.Privacy);
    }
}
