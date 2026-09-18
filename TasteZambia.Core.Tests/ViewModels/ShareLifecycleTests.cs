using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.Tests.Fakes;
using TasteZambia.Core.ViewModels;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.ViewModels;

file sealed class Nav : INavigationService
{
    public List<string> Routes { get; } = [];
    public bool WentBack { get; private set; }
    public Task GoToAsync(string r) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoToAsync(string r, IDictionary<string, object> p) { Routes.Add(r); return Task.CompletedTask; }
    public Task GoBackAsync() { WentBack = true; return Task.CompletedTask; }
}

public class ShareLifecycleTests
{
    [Fact]
    public async Task ShareStart_ShowsBothRoutesAndTheRecordCounts()
    {
        var nav = new Nav();
        var vm = new ShareStartViewModel(TestServices.Contributions(), nav);
        await vm.InitializeAsync();

        Assert.Equal(1, vm.PublishedCount);
        Assert.Equal(1, vm.InReviewCount);
        Assert.Equal(4, vm.PreservedCount);
        Assert.Equal("0 drafts in progress", vm.DraftsLabel);

        vm.GoShareCommand.Execute(null);
        vm.GoPreserveCommand.Execute(null);
        vm.GoDraftsCommand.Execute(null);

        Assert.Equal(["share", "famStart", "shareDraft"], nav.Routes);
    }

    [Fact]
    public async Task Drafts_AreTheLocalDraftsNewestFirstWithTheirTints()
    {
        var svc = TestServices.Contributions();
        svc.SaveDraft(new ContributionDraft { LocalName = "Ubwali bwa Tute", Province = "Luapula" });
        svc.SaveDraft(new ContributionDraft { LocalName = "Munkoyo", Province = "Central", EnglishDescription = "A fermented maize drink", MealType = "Drink" });
        svc.SaveDraft(svc.StartShareDraft());
        var nav = new Nav();
        var vm = new DraftsViewModel(svc, nav);
        await vm.InitializeAsync();

        Assert.Equal(3, vm.Drafts.Count);
        Assert.Equal("Chibwabwa na Mbalala", vm.Drafts[0].Name);   // saved last, so edited most recently
        Assert.Equal(100, vm.Drafts[0].PercentComplete);
        Assert.Equal("100%", vm.Drafts[0].PercentLabel);
        Assert.Equal(1.0, vm.Drafts[0].Fraction, 3);
        Assert.Equal("#2F6A4D", vm.Drafts[0].TintHex);
        Assert.Equal("#C07F1E", vm.Drafts[1].TintHex);
        Assert.Equal("#A3452A", vm.Drafts[2].TintHex);

        await vm.ContinueCommand.ExecuteAsync(vm.Drafts[2]);
        Assert.Equal(["share"], nav.Routes);
    }

    [Fact]
    public void TimelineFor_MapsEventsOntoTheFourDesignStages()
    {
        var svc = TestServices.Contributions();
        var detail = Detail(ContributionStatus.InReview, [Submitted(), Read("Namakau Sitali")]);

        var steps = svc.TimelineFor(detail);

        Assert.Equal(4, steps.Count);
        Assert.Equal(("Submitted", "2 Sep 2026", ReviewState.Done), (steps[0].Label, steps[0].When, steps[0].State));
        Assert.Equal(("Read by the archive team", "3 Sep 2026", ReviewState.Done), (steps[1].Label, steps[1].When, steps[1].State));
        Assert.Equal("Reviewed by Namakau Sitali.", steps[1].Note);
        Assert.Equal(("Checked against regional sources", "In progress", ReviewState.InProgress), (steps[2].Label, steps[2].When, steps[2].State));
        Assert.Equal(("Published and credited", "Pending", ReviewState.Pending), (steps[3].Label, steps[3].When, steps[3].State));
        Assert.True(steps[2].IsNotLast);
        Assert.False(steps[3].IsNotLast);
        Assert.Equal("#C07F1E", steps[2].DotHex);
        Assert.Equal("#D8CDB9", steps[3].DotHex);
        Assert.Equal("#7A6B59", steps[3].LabelHex);
    }

    [Fact]
    public void TimelineFor_JustSubmitted_HasReadInProgress()
    {
        var steps = TestServices.Contributions().TimelineFor(Detail(ContributionStatus.InReview, [Submitted()]));
        Assert.Equal(ReviewState.InProgress, steps[1].State);
        Assert.Equal(ReviewState.Pending, steps[2].State);
    }

    [Fact]
    public void TimelineFor_PublishedContribution_IsAllDone()
    {
        var steps = TestServices.Contributions().TimelineFor(
            Detail(ContributionStatus.Published, [Submitted(), Read("Namakau Sitali"), Published("Namakau Sitali")]));
        Assert.All(steps, s => Assert.Equal(ReviewState.Done, s.State));
        Assert.Equal("12 Sep 2026", steps[3].When);
    }

    [Fact]
    public void TimelineFor_Withdrawn_LeavesTheRestMarkedWithdrawn()
    {
        var steps = TestServices.Contributions().TimelineFor(Detail(ContributionStatus.Withdrawn, [Submitted()]));
        Assert.Equal(ReviewState.Done, steps[0].State);
        Assert.Equal(("Withdrawn", ReviewState.Pending), (steps[1].When, steps[1].State));
    }

    [Fact]
    public void FlagsFor_ReturnsOnlyOpenQuestionsWithTheirIds()
    {
        var detail = Detail(ContributionStatus.ChangesRequested, [Submitted()],
        [
            new(7, "Local name", "Is this dish called Chibwabwa na Mbalala in Mungwi specifically?", "Chibwabwa na Mbalala", null),
            new(8, "Cooking step 2", "Roughly how long does that take by hand?", "Pound the groundnuts until the oil starts to show.", "Ten minutes."),
        ]);

        var flags = TestServices.Contributions().FlagsFor(detail);

        var only = Assert.Single(flags);
        Assert.Equal(7, only.Id);
        Assert.Equal("Local name", only.Field);
        Assert.Contains("Mungwi", only.Question);
        Assert.Equal("", only.Answer);
    }

    private static readonly DateTimeOffset T = new(2026, 9, 2, 9, 0, 0, TimeSpan.Zero);
    internal static ReviewEventDto Submitted() => new(ReviewEventKind.Submitted, T, null, null);
    internal static ReviewEventDto Read(string actor) => new(ReviewEventKind.Read, T.AddDays(1), actor, null);
    internal static ReviewEventDto ChangesRequested(string actor, string note) => new(ReviewEventKind.ChangesRequested, T.AddDays(2), actor, note);
    internal static ReviewEventDto Published(string actor) => new(ReviewEventKind.Published, T.AddDays(10), actor, null);

    internal static ContributionDetailDto Detail(ContributionStatus status, IReadOnlyList<ReviewEventDto> events, IReadOnlyList<FlaggedFieldDto>? flags = null)
        => new(Guid.NewGuid(), "Chibwabwa na Mbalala", "Pumpkin leaves with pounded groundnuts", "Northern", status, events[0].At, null,
            "Chanda Mwaba", "Kitwe", "", "", true, events, flags ?? []);

    [Fact]
    public async Task Review_LoadsTheContributionByIdAndBuildsTheTimeline()
    {
        var fake = new FakeContributionService();
        var id = fake.Add(Detail(ContributionStatus.InReview, [Submitted(), Read("Namakau Sitali")]));
        var vm = new ShareReviewViewModel(fake, new Nav()) { Id = id };

        await vm.InitializeAsync();

        Assert.Equal("Chibwabwa na Mbalala", vm.DishName);
        Assert.Equal("Northern Province · submitted 2 September 2026", vm.SubmittedMeta);
        Assert.Equal("Namakau Sitali", vm.ReviewerName);
        Assert.Equal("Northern Province records", vm.ReviewerScope);
        Assert.Equal(4, vm.Timeline.Count);
        Assert.True(vm.CanWithdraw);
        Assert.False(vm.HasChangesRequested);
    }

    [Fact]
    public async Task Review_Withdraw_CallsTheServiceAndGoesBack()
    {
        var fake = new FakeContributionService();
        var nav = new Nav();
        var id = fake.Add(Detail(ContributionStatus.InReview, [Submitted()]));
        var vm = new ShareReviewViewModel(fake, nav) { Id = id };
        await vm.InitializeAsync();

        await vm.WithdrawCommand.ExecuteAsync(null);

        Assert.Equal(id, fake.Withdrawn.Single());
        Assert.True(nav.WentBack);
    }

    [Fact]
    public async Task Review_OfAPublishedContribution_CannotWithdraw()
    {
        var fake = new FakeContributionService();
        var id = fake.Add(Detail(ContributionStatus.Published, [Submitted(), Read("R"), Published("R")]));
        var vm = new ShareReviewViewModel(fake, new Nav()) { Id = id };
        await vm.InitializeAsync();
        Assert.False(vm.CanWithdraw);
        Assert.False(vm.WithdrawCommand.CanExecute(null));
    }

    [Fact]
    public async Task Changes_ResubmitIsDisabledUntilEveryQuestionIsAnswered()
    {
        var fake = new FakeContributionService();
        var nav = new Nav();
        var id = fake.Add(Detail(ContributionStatus.ChangesRequested,
            [Submitted(), Read("Namakau Sitali"), ChangesRequested("Namakau Sitali", "Two things.")],
            [
                new(1, "Local name", "Mungwi or Kasama?", "Chibwabwa na Mbalala", null),
                new(2, "Cooking step 2", "How long?", "Pound the groundnuts until the oil starts to show.", null),
            ]));
        var vm = new ShareChangesViewModel(fake, nav) { Id = id };
        await vm.InitializeAsync();

        Assert.Equal("Namakau Sitali", vm.ReviewerName);
        Assert.Equal("Two things.", vm.ReviewerNote);
        Assert.Equal(2, vm.Flagged.Count);
        Assert.False(vm.ResubmitCommand.CanExecute(null));

        vm.Flagged[0].Answer = "Mungwi specifically.";
        vm.Flagged[1].Answer = "About ten minutes by hand.";
        vm.AnswersChanged();
        Assert.True(vm.ResubmitCommand.CanExecute(null));

        await vm.ResubmitCommand.ExecuteAsync(null);
        Assert.Equal([1, 2], fake.Resubmitted[id].Select(a => a.FlagId));
        Assert.Equal(["shareReview"], nav.Routes);
    }

    [Fact]
    public async Task Published_BuildsTheCreditLine()
    {
        var fake = new FakeContributionService();
        var id = fake.Add(Detail(ContributionStatus.Published, [Submitted(), Read("Namakau Sitali"), Published("Namakau Sitali")])
            with { ContributorName = "Chanda Mwaba", ContributorLocation = "Kitwe", TaughtBy = "Banakulu Mwaba", TaughtByOrigin = "Mungwi, Northern Province", PublishedDishId = "chibwabwa-na-mbalala" });
        var nav = new Nav();
        var vm = new SharePublishedViewModel(fake, nav) { Id = id };
        await vm.InitializeAsync();

        Assert.Equal("Chibwabwa na Mbalala", vm.DishName);
        Assert.Equal("Recorded by Chanda Mwaba, Kitwe. As taught by Banakulu Mwaba of Mungwi, Northern Province. Verified against provincial records, September 2026.", vm.Credit);
        Assert.Equal(318, vm.OpenedCount);

        await vm.OpenInArchiveCommand.ExecuteAsync(null);
        Assert.Equal(["recipe"], nav.Routes);
    }

    [Fact]
    public async Task Published_WithoutATeacher_OmitsTheTaughtBySentence()
    {
        var fake = new FakeContributionService();
        var id = fake.Add(Detail(ContributionStatus.Published, [Submitted(), Published("R")])
            with { ContributorName = "Chanda Mwaba", ContributorLocation = "", CreditTeacher = false });
        var vm = new SharePublishedViewModel(fake, new Nav()) { Id = id };
        await vm.InitializeAsync();
        Assert.Equal("Recorded by Chanda Mwaba. Verified against provincial records, September 2026.", vm.Credit);
    }
}
