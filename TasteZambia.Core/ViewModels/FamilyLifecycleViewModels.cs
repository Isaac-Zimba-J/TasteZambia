using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed record FamStep(string Number, string StepTitle, string Detail);

public sealed partial class FamStartViewModel(INavigationService navigation) : BaseViewModel(navigation)
{
    public IReadOnlyList<FamStep> Steps { get; } =
    [
        new("1", "The recipe",     "Name in your own language, region, photos."),
        new("2", "Who taught you", "Their name, where they learned it, and their voice if you can record it."),
        new("3", "The story",      "When it was cooked, what it meant, how they did it differently."),
        new("4", "Who can see it", "Private, your family, or the public archive. Changeable at any time."),
    ];

    [RelayCommand] private Task Begin() => Navigation.GoToAsync("family");
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class FamDraftViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<DraftChecklistItem> Checklist { get; } = [];

    public int PercentComplete => archive.PercentComplete;
    public double Fraction => archive.PercentComplete / 100.0;
    public AudioClip Recording => archive.Recording;
    public string RecipeName => "Ifisashi ya Banakulu";
    public string Meta => "Northern Province · Bemba · edited 2 hours ago";
    public string TranscriptionNote =>
        "Transcription pending. A Bemba speaker on the archive team will transcribe it, and you approve the text before it is attached.";

    [ObservableProperty] private string _progressLabel = "";

    public override Task InitializeAsync()
    {
        if (Checklist.Count > 0) return Task.CompletedTask;

        foreach (var item in archive.Checklist) Checklist.Add(item);

        var left = Checklist.Count(c => !c.IsDone);
        ProgressLabel = $"{PercentComplete}% complete · {left} thing{(left == 1 ? "" : "s")} left";
        return Task.CompletedTask;
    }

    [RelayCommand] private Task Finish() => Navigation.GoToAsync("famSaved");
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class FamSavedViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<FamilyMember> Members { get; } = [];

    public PrivacyLevel Privacy => archive.Privacy;
    public string PrivacyLabel => "Shared with family";
    public string PrivacyNote => "Anyone you invite can read it and add their own notes. It stays out of the public archive.";
    public string Headline => "Kept in your family archive";
    public string Body => "Ifisashi ya Banakulu is saved with her recording, her name, and the story behind it.";

    public override Task InitializeAsync()
    {
        if (Members.Count > 0) return Task.CompletedTask;
        foreach (var m in archive.Members) Members.Add(m);
        return Task.CompletedTask;
    }

    [RelayCommand] private Task OpenFamilyView() => Navigation.GoToAsync("famShared");
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class FamSharedViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<FamilyNote> Notes { get; } = [];

    public AudioClip Recording => archive.ApprovedRecording;
    public string AccessBadge => $"FAMILY ONLY · {archive.Members.Count} PEOPLE";
    public string RecipeName => "Ifisashi ya Banakulu";
    public string TaughtBy => "As taught by Banakulu Mwaba, Mungwi";

    [ObservableProperty] private string _notesCountLabel = "";

    public override Task InitializeAsync()
    {
        if (Notes.Count > 0) return Task.CompletedTask;
        foreach (var n in archive.Notes) Notes.Add(n);
        NotesCountLabel = $"{Notes.Count} notes";
        return Task.CompletedTask;
    }

    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class FamPublicViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<ProvenanceStep> Provenance { get; } = [];

    public string Headline => "Ifisashi ya Banakulu";
    public string Intro => "The family chose to open this recipe to everyone. It kept its name, its recording and its credit.";
    public string Credit => "Banakulu Mwaba of Mungwi, Northern Province. Recorded by her grandson, Chanda Mwaba.";
    public string CreditNote => "This credit cannot be removed by anyone but the family, and travels with the recipe wherever it is shown.";

    public override Task InitializeAsync()
    {
        if (Provenance.Count > 0) return Task.CompletedTask;
        foreach (var s in archive.Provenance) Provenance.Add(s);
        return Task.CompletedTask;
    }

    [RelayCommand] private void MakePrivate() => archive.SetPrivacy(PrivacyLevel.PrivateToMe);
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}
