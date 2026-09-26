using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.ViewModels;

public sealed record FamStep(string Number, string StepTitle, string Detail);

public sealed partial class FamStartViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public IReadOnlyList<FamStep> Steps { get; } =
    [
        new("1", "The recipe",     "Name in your own language, region, photos."),
        new("2", "Who taught you", "Their name, where they learned it, and their voice if you can record it."),
        new("3", "The story",      "When it was cooked, what it meant, how they did it differently."),
        new("4", "Who can see it", "Private, your family, or the public archive. Changeable at any time."),
    ];

    public ObservableCollection<PreservedRecipe> Recipes { get; } = [];

    [ObservableProperty] private bool _isEmpty;

    protected override bool HasContent => Recipes.Count > 0;

    public override async Task InitializeAsync()
    {
        Recipes.Clear();
        foreach (var r in await archive.GetShelfAsync()) Recipes.Add(r);
        IsEmpty = Recipes.Count == 0;
    }

    [RelayCommand] private Task Begin() => Navigation.GoToAsync("family");

    [RelayCommand]
    private Task Open(PreservedRecipe? recipe)
        => recipe is null ? Task.CompletedTask : Navigation.GoToAsync("famSaved", new Dictionary<string, object> { ["id"] = recipe.Id });

    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

/// <summary>
/// A specific family recipe, mid-creation: what has been captured, what has not, and the
/// place to add a photograph or her recording before moving on to <c>famSaved</c>.
/// </summary>
public sealed partial class FamDraftViewModel(
    IFamilyArchiveService archive, IPhotoPicker photoPicker, IMediaUploader mediaUploader,
    IVoiceRecorder recorder, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<DraftChecklistItem> Checklist { get; } = [];

    /// <summary>Route parameter: which family recipe this draft is.</summary>
    public Guid Id { get; set; }

    [ObservableProperty] private int _percentComplete;
    [ObservableProperty] private double _fraction;
    [ObservableProperty] private string _recipeName = "";
    [ObservableProperty] private string _meta = "";
    [ObservableProperty] private string _progressLabel = "";
    [ObservableProperty] private string _transcriptionNote = "";
    [ObservableProperty] private bool _hasRecording;
    [ObservableProperty] private AudioClip _recording = new("", "", false);
    [ObservableProperty] private bool _isRecording;

    /// <summary>Why the last photo or recording did not attach. Empty when there is nothing to report.</summary>
    [ObservableProperty] private string _mediaError = "";

    protected override bool HasContent => RecipeName.Length > 0;

    public override async Task InitializeAsync()
    {
        var dto = await archive.GetAsync(Id);
        if (dto is null) return;
        Apply(dto);
    }

    private void Apply(FamilyRecipeDto dto)
    {
        RecipeName = dto.LocalName;
        Meta = $"{dto.Province} Province · {dto.Language} · edited {dto.UpdatedAt:d MMM yyyy}";
        PercentComplete = dto.PercentComplete;
        Fraction = dto.PercentComplete / 100.0;

        var hasPhoto = dto.Media.Any(m => m.Kind == MediaKind.Photo);
        HasRecording = dto.Media.Any(m => m.Kind == MediaKind.Audio);
        Recording = new AudioClip(dto.TaughtBy.Length > 0 ? dto.TaughtBy : "Her recording", "", dto.Transcript == TranscriptState.Approved);

        Checklist.Clear();
        Checklist.Add(new("Recipe name, region and photos",
            hasPhoto ? null : "A photo brings it to life, but is not required.",
            dto.LocalName.Length > 0 && dto.Province.Length > 0));
        Checklist.Add(new("Who taught you, and the story", null, dto.TaughtBy.Length > 0 && dto.Story.Length > 0));
        Checklist.Add(new("Her method, and a recording",
            HasRecording ? null : "Record her telling it, in any language.",
            dto.TraditionalMethod.Length > 0 && HasRecording));
        Checklist.Add(new("Who can see it", null, true));

        var left = Checklist.Count(c => !c.IsDone);
        ProgressLabel = $"{PercentComplete}% complete · {left} thing{(left == 1 ? "" : "s")} left";
        TranscriptionNote = $"Transcription pending. A {(dto.Language.Length > 0 ? dto.Language : "family")} speaker on the archive team " +
                             "will transcribe it, and you approve the text before it is attached.";
    }

    [RelayCommand]
    private async Task AddPhoto()
    {
        MediaError = "";
        var picked = await photoPicker.PickPhotoAsync();
        if (picked is not null) await UploadAndAttachAsync(picked, MediaKind.Photo);
    }

    [RelayCommand]
    private async Task ToggleRecording()
    {
        MediaError = "";
        if (recorder.IsRecording)
        {
            var file = await recorder.StopAsync();
            IsRecording = false;
            if (file is not null) await UploadAndAttachAsync(file, MediaKind.Audio);
            return;
        }

        try
        {
            await recorder.StartAsync();
            IsRecording = true;
        }
        catch (Exception)
        {
            // Permission refused, or no microphone on this device - either way, not a crash.
            MediaError = "Could not start recording. Check the microphone permission.";
        }
    }

    private async Task UploadAndAttachAsync(PickedFile file, MediaKind kind)
    {
        var mediaId = await mediaUploader.UploadAsync(file, kind);
        if (mediaId is null)
        {
            // MediaUploader already queued it for the next time the archive is reachable.
            MediaError = "Saved on your phone; it will attach once you are back online.";
            return;
        }

        try
        {
            await archive.AttachMediaAsync(Id, mediaId.Value);
            var dto = await archive.GetAsync(Id);
            if (dto is not null) Apply(dto);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            MediaError = OfflineMessage;
        }
    }

    [RelayCommand] private Task Finish() => Navigation.GoToAsync("famSaved", new Dictionary<string, object> { ["id"] = Id });
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class FamSavedViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<FamilyMember> Members { get; } = [];

    /// <summary>Route parameter: which family recipe was just saved.</summary>
    public Guid Id { get; set; }

    [ObservableProperty] private PrivacyLevel _privacy;
    [ObservableProperty] private string _privacyLabel = "";
    [ObservableProperty] private string _privacyNote = "";
    [ObservableProperty] private string _headline = "Kept in your family archive";
    [ObservableProperty] private string _body = "";

    protected override bool HasContent => Body.Length > 0;

    public override async Task InitializeAsync()
    {
        var dto = await archive.GetAsync(Id);
        if (dto is null) return;

        Privacy = dto.Privacy;
        (PrivacyLabel, PrivacyNote) = dto.Privacy switch
        {
            PrivacyLevel.PublicInArchive => ("Public in the archive", "Anyone can read it. It keeps its credit to your family wherever it is shown."),
            PrivacyLevel.SharedWithFamily => ("Shared with family", "Anyone you invite can read it and add their own notes. It stays out of the public archive."),
            _ => ("Private to you", "Only you can open it. Nothing is reviewed or published."),
        };

        var withRecording = dto.Media.Any(m => m.Kind == MediaKind.Audio) ? " with her recording, its name, and the story behind it." : " with its name and the story behind it.";
        Body = dto.LocalName + " is saved" + withRecording;

        Members.Clear();
        foreach (var m in dto.Members.Where(m => m.State != MemberState.Removed)) Members.Add(FamilyMember.From(m));
    }

    [RelayCommand] private Task OpenFamilyView() => Navigation.GoToAsync("famShared", new Dictionary<string, object> { ["id"] = Id });
    [RelayCommand] private Task ChangePrivacy() => Navigation.GoToAsync("famPublic", new Dictionary<string, object> { ["id"] = Id });
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class FamSharedViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<FamilyMember> Members { get; } = [];
    public ObservableCollection<FamilyNote> Notes { get; } = [];

    /// <summary>Route parameter: which family recipe this is.</summary>
    public Guid Id { get; set; }

    [ObservableProperty] private string _recipeName = "";
    [ObservableProperty] private string _taughtBy = "";
    [ObservableProperty] private string _accessBadge = "";
    [ObservableProperty] private string _notesCountLabel = "";
    [ObservableProperty] private bool _hasRecording;
    [ObservableProperty] private AudioClip _recording = new("", "", false);

    [ObservableProperty] private string _newMemberName = "";
    [ObservableProperty] private string _newMemberRelation = "";
    [ObservableProperty] private string _inviteCode = "";

    /// <summary>Why the last invite or removal did not take. Empty when there is nothing to report.</summary>
    [ObservableProperty] private string _actionError = "";

    protected override bool HasContent => RecipeName.Length > 0;

    public override async Task InitializeAsync()
    {
        var dto = await archive.GetAsync(Id);
        if (dto is null) return;

        RecipeName = dto.LocalName;
        TaughtBy = dto.TaughtBy.Length > 0 ? $"As taught by {dto.TaughtBy}" : "";

        var active = dto.Members.Count(m => m.State != MemberState.Removed);
        // +1: the owner reading this screen is never in the member list, but is always one of the people who can see it.
        AccessBadge = $"FAMILY ONLY · {active + 1} PEOPLE";

        HasRecording = dto.Media.Any(m => m.Kind == MediaKind.Audio);
        Recording = new AudioClip(dto.TaughtBy, "", dto.Transcript == TranscriptState.Approved);

        Members.Clear();
        foreach (var m in dto.Members.Where(m => m.State != MemberState.Removed)) Members.Add(FamilyMember.From(m));

        Notes.Clear();
        foreach (var n in dto.Notes) Notes.Add(FamilyNote.From(n));
        NotesCountLabel = Notes.Count == 1 ? "1 note" : $"{Notes.Count} notes";
    }

    [RelayCommand]
    private async Task Invite()
    {
        if (NewMemberName.Trim().Length == 0) return;
        try
        {
            var invite = await archive.InviteAsync(Id, NewMemberName.Trim(), NewMemberRelation.Trim());
            if (invite is null) { ActionError = "Could not send that invite."; return; }

            InviteCode = invite.Code;
            ActionError = "";
            NewMemberName = "";
            NewMemberRelation = "";
            await InitializeAsync();   // the person just invited now shows up as Invited
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ActionError = OfflineMessage;
        }
    }

    [RelayCommand]
    private async Task RemoveMember(FamilyMember? member)
    {
        if (member is null) return;
        try
        {
            await archive.RemoveMemberAsync(Id, member.Id);
            Members.Remove(member);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ActionError = OfflineMessage;
        }
    }

    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

public sealed partial class FamPublicViewModel(
    IFamilyArchiveService archive, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<ProvenanceStep> Provenance { get; } = [];

    /// <summary>Route parameter: which family recipe this is.</summary>
    public Guid Id { get; set; }

    [ObservableProperty] private string _headline = "";
    [ObservableProperty] private string _intro = "";
    [ObservableProperty] private string _credit = "";
    [ObservableProperty] private string _creditNote = "This credit cannot be removed by anyone but the family, and travels with the recipe wherever it is shown.";
    [ObservableProperty] private string _privacyNote = "";
    [ObservableProperty] private bool _isPublic;

    private FamilyRecipeDto? _dto;

    protected override bool HasContent => Headline.Length > 0;

    public override async Task InitializeAsync()
    {
        _dto = await archive.GetAsync(Id);
        if (_dto is not null) Apply(_dto);
    }

    private void Apply(FamilyRecipeDto dto)
    {
        _dto = dto;
        Headline = dto.LocalName;
        IsPublic = dto.Privacy == PrivacyLevel.PublicInArchive;
        Intro = IsPublic
            ? "The family chose to open this recipe to everyone. It kept its name, its recording and its credit."
            : "Still just for the family. Publishing sends it to the public archive, with the same name, recording and credit.";
        PrivacyNote = IsPublic
            ? "Now visible to everyone in the public archive."
            : "Only your family can see it right now. Publishing shows it to everyone.";

        Credit = dto.TaughtBy.Length > 0
            ? $"{dto.TaughtBy}{(dto.TaughtByOrigin.Length > 0 ? $" of {dto.TaughtByOrigin}" : "")}."
            : "";

        var active = dto.Members.Count(m => m.State != MemberState.Removed);

        Provenance.Clear();
        Provenance.Add(new ProvenanceStep("Preserved privately", $"Written down on {dto.UpdatedAt:d MMMM yyyy}.", "#2F6A4D"));
        Provenance.Add(active > 0
            ? new ProvenanceStep("Shared with family", $"{active} member{(active == 1 ? "" : "s")} have access.", "#2F6A4D")
            : new ProvenanceStep("Not yet shared", "No one has been invited yet.", "#D8CDB9"));
        Provenance.Add(IsPublic
            ? new ProvenanceStep("Published and credited", "Listed in the public archive, credited to your family.", "#C07F1E")
            : new ProvenanceStep("Not yet published", "Still private to your family.", "#D8CDB9"));
    }

    [RelayCommand]
    private async Task SetPublic()
    {
        try
        {
            await archive.SetPrivacyAsync(Id, PrivacyLevel.PublicInArchive);
            if (_dto is not null) Apply(_dto with { Privacy = PrivacyLevel.PublicInArchive });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            PrivacyNote = OfflineMessage;
        }
    }

    [RelayCommand]
    private async Task MakePrivate()
    {
        try
        {
            await archive.SetPrivacyAsync(Id, PrivacyLevel.SharedWithFamily);
            if (_dto is not null) Apply(_dto with { Privacy = PrivacyLevel.SharedWithFamily });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            PrivacyNote = OfflineMessage;
        }
    }

    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}
