using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class PrivacyOptionViewModel(
    PrivacyLevel level, string label, string note, Action<PrivacyOptionViewModel> onSelect)
    : ObservableObject
{
    public PrivacyLevel Level { get; } = level;
    public string Label { get; } = label;
    public string Note { get; } = note;

    [ObservableProperty]
    private bool _isSelected;

    [RelayCommand]
    private void Select() => onSelect(this);
}

public sealed partial class FamilyViewModel(
    IContributionService contributions,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ContributionDraft Draft { get; private set; } = new();
    public ObservableCollection<PrivacyOptionViewModel> PrivacyOptions { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(IsStep4))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(NextLabel))]
    private int _step = 1;

    public string StepTitle => SeedData.FamilySteps[Step - 1];
    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool IsStep3 => Step == 3;
    public bool IsStep4 => Step == 4;
    public bool CanGoBack => Step > 1;
    public string NextLabel => Step == 4 ? "Save to the archive" : "Continue";

    public override Task InitializeAsync()
    {
        if (PrivacyOptions.Count > 0) return Task.CompletedTask;

        Draft = contributions.StartFamilyDraft();

        PrivacyOptions.Add(new PrivacyOptionViewModel(PrivacyLevel.PrivateToMe, "Private to me",
            "Only you can open it. Nothing is reviewed or published.", SelectPrivacy));
        PrivacyOptions.Add(new PrivacyOptionViewModel(PrivacyLevel.SharedWithFamily, "Shared with family",
            "Anyone you invite by name can read and add to it.", SelectPrivacy));
        PrivacyOptions.Add(new PrivacyOptionViewModel(PrivacyLevel.PublicInArchive, "Public in the archive",
            "Goes to the archive team for verification before it appears publicly.", SelectPrivacy));

        foreach (var option in PrivacyOptions)
            option.IsSelected = option.Level == Draft.Privacy;

        return Task.CompletedTask;
    }

    private void SelectPrivacy(PrivacyOptionViewModel option)
    {
        foreach (var o in PrivacyOptions)
            o.IsSelected = ReferenceEquals(o, option);

        Draft.Privacy = option.Level;
    }

    [RelayCommand] private void Next() => Step = Math.Min(4, Step + 1);
    [RelayCommand] private void Back() => Step = Math.Max(1, Step - 1);
    [RelayCommand] private Task Close() => Navigation.GoBackAsync();
}
