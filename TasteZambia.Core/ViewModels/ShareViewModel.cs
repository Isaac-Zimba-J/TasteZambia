using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class ShareViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ContributionDraft Draft { get; private set; } = new();
    public ObservableCollection<ReviewStage> Pipeline { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(IsStep4))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(NextLabel))]
    private int _step = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending))]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(IsStep4))]
    private bool _isSubmitted;

    [ObservableProperty] private string _submittedName = "";
    [ObservableProperty] private string _submittedMeta = "";
    [ObservableProperty] private string _submitError = "";

    /// <summary>Route parameter: continue an existing local draft instead of starting the walkthrough one.</summary>
    public Guid? DraftId { get; set; }

    /// <summary>The contribution's id once submitted; the Review screen opens on it.</summary>
    public Guid? SubmittedId { get; private set; }

    public string StepTitle => SeedData.ShareSteps[Step - 1];
    public bool IsPending => !IsSubmitted;
    public bool IsStep1 => IsPending && Step == 1;
    public bool IsStep2 => IsPending && Step == 2;
    public bool IsStep3 => IsPending && Step == 3;
    public bool IsStep4 => IsPending && Step == 4;
    public bool CanGoBack => Step > 1;
    public string NextLabel => Step == 4 ? "Submit for review" : "Continue";

    public override Task InitializeAsync()
    {
        if (Pipeline.Count > 0) return Task.CompletedTask;

        Draft = DraftId is { } id && contributions.FindDraft(id) is { } local
            ? local.Draft
            : contributions.StartShareDraft();

        foreach (var stage in contributions.ReviewPipeline)
            Pipeline.Add(stage);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Next()
    {
        // Every step forward lands the draft locally first, so nothing typed is ever lost.
        DraftId = contributions.SaveDraft(Draft, DraftId).Id;

        if (Step < 4)
        {
            Step++;
            return;
        }

        try
        {
            var contribution = await contributions.SubmitAsync(Draft);
            SubmittedId = contribution.Id;
            SubmittedName = contribution.Name;
            SubmittedMeta = contribution.Meta;
            SubmitError = "";
            IsSubmitted = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            SubmitError = "Could not reach the archive. Your draft is saved; try again when you are online.";
        }
    }

    [RelayCommand]
    private Task ViewStatus()
        => SubmittedId is { } id
            ? Navigation.GoToAsync("shareReview", new Dictionary<string, object> { ["id"] = id })
            : Task.CompletedTask;

    [RelayCommand] private void Back() => Step = Math.Max(1, Step - 1);

    [RelayCommand]
    private void Reset()
    {
        IsSubmitted = false;
        Step = 1;
    }

    [RelayCommand] private Task Close() => Navigation.GoBackAsync();
}
