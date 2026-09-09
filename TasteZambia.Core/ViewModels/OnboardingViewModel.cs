using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class LanguageOptionViewModel(
    ReadingLanguage language, Action<LanguageOptionViewModel> onSelect) : ObservableObject
{
    public string Name { get; } = language.Name;
    public string Note { get; } = language.Note;

    [ObservableProperty] private bool _isSelected;

    [RelayCommand] private void Select() => onSelect(this);
}

public sealed partial class VisitorKindViewModel(
    VisitorKind kind, Action<VisitorKindViewModel> onSelect) : ObservableObject
{
    public string Key { get; } = kind.Key;
    public string Note { get; } = kind.Note;

    [ObservableProperty] private bool _isSelected;

    [RelayCommand] private void Select() => onSelect(this);
}

public sealed partial class TasteChipViewModel(
    TasteOption option, Action onChanged) : ObservableObject
{
    public string Key { get; } = option.Key;
    public string Label { get; } = option.Label;

    [ObservableProperty] private bool _isSelected;

    [RelayCommand]
    private void Toggle()
    {
        IsSelected = !IsSelected;
        onChanged();
    }
}

/// <summary>
/// One instance shared by all seven onboarding screens, so choices survive the
/// walk from language through to the summary on the Ready screen.
/// </summary>
public sealed partial class OnboardingViewModel(
    IOnboardingService onboarding,
    INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<LanguageOptionViewModel> Languages { get; } = [];
    public ObservableCollection<VisitorKindViewModel> VisitorKinds { get; } = [];
    public ObservableCollection<TasteChipViewModel> TasteChips { get; } = [];

    [ObservableProperty] private string _pickedLanguage = "English";
    [ObservableProperty] private string _pickedWho = "I grew up here";
    [ObservableProperty] private string _tasteCountLabel = "";
    [ObservableProperty] private bool _offlineEnabled = true;
    [ObservableProperty] private bool _storyNotifications;

    public override Task InitializeAsync()
    {
        if (Languages.Count > 0) return Task.CompletedTask;

        var choices = onboarding.Current;

        foreach (var language in onboarding.Languages)
            Languages.Add(new LanguageOptionViewModel(language, SelectLanguage)
            { IsSelected = language.Name == choices.Language });

        foreach (var kind in onboarding.VisitorKinds)
            VisitorKinds.Add(new VisitorKindViewModel(kind, SelectWho)
            { IsSelected = kind.Key == choices.Who });

        foreach (var taste in onboarding.Tastes)
            TasteChips.Add(new TasteChipViewModel(taste, RefreshTasteCount)
            { IsSelected = choices.Tastes.Contains(taste.Key) });

        PickedLanguage = choices.Language;
        PickedWho = choices.Who;
        OfflineEnabled = choices.OfflineEnabled;
        StoryNotifications = choices.StoryNotifications;

        RefreshTasteCount();
        return Task.CompletedTask;
    }

    private void SelectLanguage(LanguageOptionViewModel picked)
    {
        foreach (var l in Languages) l.IsSelected = ReferenceEquals(l, picked);
        PickedLanguage = picked.Name;
    }

    private void SelectWho(VisitorKindViewModel picked)
    {
        foreach (var w in VisitorKinds) w.IsSelected = ReferenceEquals(w, picked);
        PickedWho = picked.Key;
    }

    private void RefreshTasteCount()
        => TasteCountLabel = $"{TasteChips.Count(t => t.IsSelected)} selected";

    private OnboardingChoices Snapshot() => new()
    {
        Language = PickedLanguage,
        Who = PickedWho,
        Tastes = [.. TasteChips.Where(t => t.IsSelected).Select(t => t.Key)],
        OfflineEnabled = OfflineEnabled,
        StoryNotifications = StoryNotifications,
    };

    [RelayCommand] private Task ToIntro() => Navigation.GoToAsync("intro");
    [RelayCommand] private Task ToLanguage() => Navigation.GoToAsync("onbLang");
    [RelayCommand] private Task ToWho() => Navigation.GoToAsync("onbWho");
    [RelayCommand] private Task ToTaste() => Navigation.GoToAsync("onbTaste");
    [RelayCommand] private Task ToNotify() => Navigation.GoToAsync("onbNotify");
    [RelayCommand] private Task ToReady() => Navigation.GoToAsync("onbReady");

    /// <summary>Finishing and skipping both record the choices; skipping just uses the defaults.</summary>
    [RelayCommand]
    private Task Finish()
    {
        onboarding.Complete(Snapshot());
        return Navigation.GoToAsync("//home");
    }

    [RelayCommand]
    private Task Skip()
    {
        onboarding.Complete(Snapshot());
        return Navigation.GoToAsync("//home");
    }
}
