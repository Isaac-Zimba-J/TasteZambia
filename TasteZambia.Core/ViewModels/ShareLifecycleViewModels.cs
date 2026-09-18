using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.ViewModels;

public sealed partial class ShareStartViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    [ObservableProperty] private int _publishedCount = 1;
    [ObservableProperty] private int _inReviewCount = 1;
    [ObservableProperty] private int _preservedCount = 4;
    [ObservableProperty] private string _draftsLabel = "";

    public override Task InitializeAsync()
    {
        var n = contributions.Drafts.Count;
        DraftsLabel = $"{n} draft{(n == 1 ? "" : "s")} in progress";
        return Task.CompletedTask;
    }

    [RelayCommand] private Task GoShare() => Navigation.GoToAsync("share");
    [RelayCommand] private Task GoPreserve() => Navigation.GoToAsync("famStart");
    [RelayCommand] private Task GoDrafts() => Navigation.GoToAsync("shareDraft");
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class DraftsViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<RecipeDraft> Drafts { get; } = [];

    public override Task InitializeAsync()
    {
        Drafts.Clear();
        foreach (var d in contributions.Drafts) Drafts.Add(d);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Continue(RecipeDraft? draft)
        => draft is null
            ? Navigation.GoToAsync("share")
            : Navigation.GoToAsync("share", new Dictionary<string, object> { ["draftId"] = draft.Id });
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class ShareReviewViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    private const string DefaultReviewer = "The archive team";

    public ObservableCollection<ReviewStep> Timeline { get; } = [];

    /// <summary>Route parameter: which contribution to show.</summary>
    public Guid Id { get; set; }

    [ObservableProperty] private string _dishName = "";
    [ObservableProperty] private string _submittedMeta = "";
    [ObservableProperty] private string _reviewerName = DefaultReviewer;
    [ObservableProperty] private string _reviewerScope = "";
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(WithdrawCommand))] private bool _canWithdraw;
    [ObservableProperty] private bool _hasChangesRequested;

    public override async Task InitializeAsync()
    {
        var d = await contributions.GetDetailAsync(Id);
        if (d is null) return;

        DishName = d.LocalName;
        SubmittedMeta = $"{d.Province} Province · submitted {d.SubmittedAt.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}";
        ReviewerName = d.Events.FirstOrDefault(e => e.Kind == ReviewEventKind.Read)?.Actor ?? DefaultReviewer;
        ReviewerScope = $"{d.Province} Province records";
        CanWithdraw = d.Status is ContributionStatus.InReview or ContributionStatus.ChangesRequested;
        HasChangesRequested = d.Status == ContributionStatus.ChangesRequested;

        Timeline.Clear();
        foreach (var s in contributions.TimelineFor(d)) Timeline.Add(s);
    }

    [RelayCommand(CanExecute = nameof(CanWithdraw))]
    private async Task Withdraw()
    {
        await contributions.WithdrawAsync(Id);
        await Navigation.GoBackAsync();
    }

    [RelayCommand]
    private Task SeeQuestions() => Navigation.GoToAsync("shareChanges", new Dictionary<string, object> { ["id"] = Id });

    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class ShareChangesViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<FlaggedField> Flagged { get; } = [];

    /// <summary>Route parameter: which contribution's questions to answer.</summary>
    public Guid Id { get; set; }

    [ObservableProperty] private string _reviewerName = "The archive team";
    [ObservableProperty] private string _reviewerNote = "";

    public bool CanResubmit => Flagged.Count > 0 && Flagged.All(f => f.Answer.Trim().Length > 0);

    public override async Task InitializeAsync()
    {
        var d = await contributions.GetDetailAsync(Id);
        if (d is null) return;

        var request = d.Events.LastOrDefault(e => e.Kind == ReviewEventKind.ChangesRequested);
        ReviewerName = request?.Actor ?? "The archive team";
        ReviewerNote = request?.Note ?? "";

        Flagged.Clear();
        foreach (var q in contributions.FlagsFor(d)) Flagged.Add(q);
        AnswersChanged();
    }

    /// <summary>The view calls this as the user types; Answer is a plain property, so the command and the view must be told.</summary>
    public void AnswersChanged()
    {
        OnPropertyChanged(nameof(CanResubmit));
        ResubmitCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanResubmit))]
    private async Task Resubmit()
    {
        await contributions.ResubmitAsync(Id, [.. Flagged.Select(f => new FlagAnswerDto(f.Id, f.Answer.Trim()))]);
        await Navigation.GoToAsync("shareReview", new Dictionary<string, object> { ["id"] = Id });
    }

    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class SharePublishedViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    /// <summary>Route parameter: which published contribution to celebrate.</summary>
    public Guid Id { get; set; }

    [ObservableProperty] private string _dishName = "";
    [ObservableProperty] private string _subtitle = "";
    [ObservableProperty] private string _credit = "";
    [ObservableProperty] private string? _publishedDishId;

    // No analytics source yet; the design's reach numbers stand in until one exists.
    public int OpenedCount => 318;
    public int SavedCount => 64;
    public int CookedCount => 11;

    public override async Task InitializeAsync()
    {
        var d = await contributions.GetDetailAsync(Id);
        if (d is null) return;

        DishName = d.LocalName;
        Subtitle = d.EnglishDescription;
        PublishedDishId = d.PublishedDishId;

        var taught = d.CreditTeacher && d.TaughtBy.Length > 0
            ? $" As taught by {d.TaughtBy}{(d.TaughtByOrigin.Length > 0 ? $" of {d.TaughtByOrigin}" : "")}."
            : "";
        var location = d.ContributorLocation.Length > 0 ? $", {d.ContributorLocation}" : "";
        var verified = (d.Events.LastOrDefault(e => e.Kind == ReviewEventKind.Published)?.At ?? d.SubmittedAt)
            .ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        Credit = $"Recorded by {d.ContributorName}{location}.{taught} Verified against provincial records, {verified}.";
    }

    [RelayCommand]
    private Task OpenInArchive()
        => PublishedDishId is { } dishId
            ? Navigation.GoToAsync("recipe", new Dictionary<string, object> { ["dishId"] = dishId })
            : Task.CompletedTask;

    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}
