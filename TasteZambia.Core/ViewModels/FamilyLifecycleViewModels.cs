using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Validation;

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

    /// <summary>What this phone can manage, said before the reader starts talking.</summary>
    [ObservableProperty] private string _recordingLimitNote = "";
    [ObservableProperty] private string _recordingElapsedLabel = "";

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

        // The same four facts the percentage is built from, so the list and the number
        // cannot tell the reader different things.
        var steps = FamilyDraftProgress.Steps(
            hasNameAndRegion: dto.LocalName.Length > 0 && dto.Province.Length > 0,
            hasTeacherAndStory: dto.TaughtBy.Length > 0 && dto.Story.Length > 0,
            hasMethod: dto.TraditionalMethod.Length > 0,
            privacyChosen: dto.Privacy != PrivacyLevel.PrivateToMe);

        // A photograph and a recording are invitations, not requirements - which is why they
        // are notes under their steps rather than conditions on them.
        var notes = new Dictionary<string, string>
        {
            ["Recipe name, region and photos"] = hasPhoto ? "" : "A photo brings it to life, but is not required.",
            ["Her method, in her words"] = HasRecording ? "" : "Record her telling it, in any language.",
        };

        Checklist.Clear();
        foreach (var step in steps)
            Checklist.Add(new(step.Label,
                notes.TryGetValue(step.Label, out var note) && note.Length > 0 ? note : null,
                step.IsDone));

        var left = Checklist.Count(c => !c.IsDone);
        ProgressLabel = left == 0
            ? $"{PercentComplete}% complete"
            : $"{PercentComplete}% complete · {left} thing{(left == 1 ? "" : "s")} left";
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
            await FinishRecordingAsync();
            return;
        }

        try
        {
            await recorder.StartAsync();
            IsRecording = true;
            RecordingLimitNote = $"Up to {Minutes(recorder.MaxDuration)} on this phone.";
            WatchTheClock();
        }
        catch (Exception)
        {
            // Permission refused, or no microphone on this device - either way, not a crash.
            MediaError = "Could not start recording. Check the microphone permission.";
        }
    }

    /// <summary>
    /// Stops on its own at the point the file would grow past what the archive accepts.
    /// Letting it run and fail on upload would lose a recording someone sat down to make.
    /// </summary>
    private void WatchTheClock()
    {
        var limit = recorder.MaxDuration;
        _ = Task.Run(async () =>
        {
            while (recorder.IsRecording)
            {
                if (recorder.Elapsed >= limit)
                {
                    await FinishRecordingAsync();
                    MediaError = $"Recording stopped at {Minutes(limit)} - the longest the archive can take from this phone.";
                    return;
                }

                RecordingElapsedLabel = Clock(recorder.Elapsed);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        });
    }

    private async Task FinishRecordingAsync()
    {
        var file = await recorder.StopAsync();
        IsRecording = false;
        RecordingElapsedLabel = "";
        if (file is null) return;

        await UploadAndAttachAsync(file, MediaKind.Audio);

        // The uploader holds its own copy now, so the recorder's file is litter. Nothing
        // else deletes it, and an uncompressed recording is not small.
        if (file.LocalPath is { } path && File.Exists(path)) File.Delete(path);
    }

    private static string Minutes(TimeSpan span)
    {
        var whole = (int)span.TotalMinutes;
        return whole == 1 ? "1 minute" : $"{whole} minutes";
    }

    private static string Clock(TimeSpan span) => $"{(int)span.TotalMinutes}:{span.Seconds:00}";

    private async Task UploadAndAttachAsync(PickedFile file, MediaKind kind)
    {
        // The uploader attaches it to this recipe itself - on the spot when the archive is
        // reachable, and on the next drain when it is not. This screen only reports.
        var outcome = await mediaUploader.UploadAsync(file, kind, UploadTarget.FamilyRecipe(Id));

        switch (outcome)
        {
            case UploadOutcome.Refused refused:
                MediaError = refused.Reason;
                return;

            case UploadOutcome.Queued:
                MediaError = "Saved on your phone. It will be added once you are back online.";
                return;

            case UploadOutcome.Sent:
                MediaError = "";
                break;
        }

        try
        {
            var dto = await archive.GetAsync(Id);
            if (dto is not null) Apply(dto);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // It is in the archive; only this screen's refresh failed.
            MediaError = "Added, but this screen could not refresh. Pull down to see it.";
        }
    }

    [RelayCommand] private Task Finish() => Navigation.GoToAsync("famSaved", new Dictionary<string, object> { ["id"] = Id });
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}

/// <summary>Wording for "how many people can see this", counting the owner honestly.</summary>
internal static class Access
{
    public static string People(int total) => total switch
    {
        <= 1 => "JUST YOU",
        2 => "YOU AND 1 OTHER",
        _ => $"YOU AND {total - 1} OTHERS",
    };
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

        // The owner is already one of these rows, so counting them again overstated it -
        // a recipe nobody had been invited to read "2 PEOPLE".
        AccessBadge = "FAMILY ONLY · " + Access.People(dto.Members.Count(m => m.State != MemberState.Removed));

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
            if (await archive.RemoveMemberAsync(Id, member.Id)) Members.Remove(member);
            else ActionError = NotAllowedMessage;
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

        // Only people other than the owner count as having been let in; the owner is always
        // on the list, so counting them made "not yet shared" unreachable.
        var invited = dto.Members.Count(m => m.State != MemberState.Removed && !m.IsOwner);

        Provenance.Clear();
        Provenance.Add(new ProvenanceStep("Preserved privately", $"Written down on {dto.UpdatedAt:d MMMM yyyy}.", "#2F6A4D"));
        Provenance.Add(invited > 0
            ? new ProvenanceStep("Shared with family", $"{invited} other {(invited == 1 ? "person has" : "people have")} access.", "#2F6A4D")
            : new ProvenanceStep("Not yet shared", "No one has been invited yet.", "#D8CDB9"));

        // The design's "Family agreed to publish" and "Verified against Northern records" steps
        // described a consent-and-review workflow this archive does not track yet - shown here,
        // truthfully, as still pending rather than dropped outright or filled with invented data.
        Provenance.Add(new ProvenanceStep("Family agreed to publish", "Not tracked by the archive yet.", "#D8CDB9"));
        Provenance.Add(new ProvenanceStep("Verified against Northern records", "Not tracked by the archive yet.", "#D8CDB9"));

        Provenance.Add(IsPublic
            ? new ProvenanceStep("Published and credited", "Listed in the public archive, credited to your family.", "#C07F1E")
            : new ProvenanceStep("Not yet published", "Still private to your family.", "#D8CDB9"));
    }

    [RelayCommand]
    private async Task SetPublic()
    {
        try
        {
            if (await archive.SetPrivacyAsync(Id, PrivacyLevel.PublicInArchive))
            {
                if (_dto is not null) Apply(_dto with { Privacy = PrivacyLevel.PublicInArchive });
            }
            else PrivacyNote = NotAllowedMessage;
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
            if (await archive.SetPrivacyAsync(Id, PrivacyLevel.SharedWithFamily))
            {
                if (_dto is not null) Apply(_dto with { Privacy = PrivacyLevel.SharedWithFamily });
            }
            else PrivacyNote = NotAllowedMessage;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            PrivacyNote = OfflineMessage;
        }
    }

    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}
