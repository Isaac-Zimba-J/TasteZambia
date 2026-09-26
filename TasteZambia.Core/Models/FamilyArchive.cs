using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Models;

public sealed record FamilyMember(Guid Id, string Name, string Role, string Status, string BadgeBgHex, string BadgeFgHex)
{
    private const string JoinedBg = "#EEF2EC", JoinedFg = "#2F6A4D";
    private const string InvitedBg = "#F7EEDA", InvitedFg = "#7A5A10";

    public static FamilyMember From(FamilyMemberDto dto) => dto.State switch
    {
        MemberState.Joined => new(dto.Id, dto.DisplayName, dto.Relation, "Joined", JoinedBg, JoinedFg),
        _ => new(dto.Id, dto.DisplayName, dto.Relation, "Invited", InvitedBg, InvitedFg),
    };
}

public sealed record FamilyNote(Guid Id, string Who, string When, string Body)
{
    public static FamilyNote From(FamilyNoteDto dto) => new(dto.Id, dto.AuthorName, dto.CreatedAt.ToString("d MMM"), dto.Body);
}

public sealed record DraftChecklistItem(string Label, string? Detail, bool IsDone)
{
    public bool HasDetail => !string.IsNullOrEmpty(Detail);
}

public sealed record ProvenanceStep(string Label, string Detail, string DotHex);

/// <summary>What the recording row shows. Duration is known only while this session holds
/// the file (the archive does not store one); once loaded from the account it is blank.</summary>
public sealed record AudioClip(string Speaker, string Duration, bool TranscriptApproved);

// The record's own data slot cannot be called Extras too - C# will not let a property and a
// static method share a name in one type - so the formatted line lives in ExtrasLine and the
// static formatter below keeps the name the design and the brief's test both call it by.
public sealed record PreservedRecipe(Guid Id, string Name, string TaughtBy, string Privacy,
                                     string BadgeBgHex, string BadgeFgHex, string ExtrasLine)
{
    private const string PublicBg = "#EEF2EC", PublicFg = "#2F6A4D";
    private const string FamilyBg = "#F7EEDA", FamilyFg = "#7A5A10";
    private const string PrivateBg = "#F0ECE4", PrivateFg = "#6B5C4A";

    public static PreservedRecipe From(FamilyRecipeSummaryDto dto)
    {
        var (label, bg, fg) = dto.Privacy switch
        {
            PrivacyLevel.PublicInArchive => ("Public", PublicBg, PublicFg),
            PrivacyLevel.SharedWithFamily => ("Family", FamilyBg, FamilyFg),
            _ => ("Private", PrivateBg, PrivateFg),
        };

        // The shelf has no recording length to show - only whether one exists - so an
        // audio row here always reads as "Audio" with no time attached.
        var extras = Extras(dto.HasAudio, "", dto.PhotoCount, dto.NoteCount, dto.PercentComplete);
        return new PreservedRecipe(dto.Id, dto.LocalName, dto.TaughtBy, label, bg, fg, extras);
    }

    /// <summary>The design's own wording: audio first when there is any, "no audio yet" trailing
    /// when there is not, each part omitted when its count is zero.</summary>
    public static string Extras(bool hasAudio, string audioLength, int photos, int notes, int percentComplete)
    {
        if (percentComplete < 100)
            return $"Draft · {percentComplete}% complete";

        var parts = new List<string>();
        if (hasAudio) parts.Add(audioLength.Length > 0 ? $"Audio {audioLength}" : "Audio");
        if (photos > 0) parts.Add(photos == 1 ? "1 photo" : $"{photos} photos");
        if (notes > 0) parts.Add(notes == 1 ? "1 family note" : $"{notes} family notes");
        if (!hasAudio) parts.Add("no audio yet");

        return string.Join(" · ", parts);
    }
}
