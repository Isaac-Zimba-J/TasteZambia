using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

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
        if (Drafts.Count > 0) return Task.CompletedTask;
        foreach (var d in contributions.Drafts) Drafts.Add(d);
        return Task.CompletedTask;
    }

    [RelayCommand] private Task Continue() => Navigation.GoToAsync("share");
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class ShareReviewViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<ReviewStep> Timeline { get; } = [];

    public string DishName => "Chibwabwa na Mbalala";
    public string SubmittedMeta => "Northern Province · submitted 2 September 2026";
    public string ReviewerName => "Namakau Sitali";
    public string ReviewerScope => "Northern Province records";

    public override Task InitializeAsync()
    {
        if (Timeline.Count > 0) return Task.CompletedTask;
        foreach (var s in contributions.Timeline) Timeline.Add(s);
        return Task.CompletedTask;
    }

    [RelayCommand] private Task Withdraw() => Navigation.GoToAsync("shareChanges");
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class ShareChangesViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<FlaggedField> Flagged { get; } = [];

    public string ReviewerName => "Namakau Sitali";
    public string ReviewerNote =>
        "This is a good record and the method matches what we have for Mungwi. Two things I want to get right before it goes public.";

    public override Task InitializeAsync()
    {
        if (Flagged.Count > 0) return Task.CompletedTask;
        foreach (var q in contributions.FlaggedFields) Flagged.Add(q);
        return Task.CompletedTask;
    }

    [RelayCommand] private Task Resubmit() => Navigation.GoToAsync("sharePublished");
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class SharePublishedViewModel(INavigationService navigation)
    : BaseViewModel(navigation)
{
    public string DishName => "Chibwabwa na Mbalala";
    public string Subtitle => "Pumpkin leaves with pounded groundnuts";
    public string Credit => SeedData.PublishedCredit;
    public int OpenedCount => 318;
    public int SavedCount => 64;
    public int CookedCount => 11;

    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}
