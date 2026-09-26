using TasteZambia.API.Data.Entities;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Contracts.Media;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Mapping;

public static class FamilyMappings
{
    // IsOwner and Members/Notes/Media are only meaningful once the reader is known, so
    // the caller's id comes in as a parameter rather than living on the entity.
    public static FamilyRecipeDto ToDto(this FamilyRecipe r, string userId) => new(
        r.Id, r.LocalName, r.Description, r.Province, r.Language,
        r.TaughtBy, r.TaughtByOrigin, r.Story, r.TraditionalMethod,
        r.Privacy, r.Transcript, r.PublishedDishId,
        IFamilyService.PercentComplete(r), r.OwnerId == userId, r.UpdatedAt,
        // Removed stays visible with that state rather than vanishing - the point of
        // the enum value is to say what happened, not to hide it.
        [.. r.Members.Select(m => m.ToDto())],
        [.. r.Notes.Select(n => n.ToDto())],
        [.. r.Media.Select(a => a.ToDto())]);

    public static FamilyRecipeSummaryDto ToSummary(this FamilyRecipe r) => new(
        r.Id, r.LocalName, r.TaughtBy, r.Privacy,
        r.Media.Count(m => m.Kind == MediaKind.Photo),
        r.Notes.Count,
        r.Media.Any(m => m.Kind == MediaKind.Audio),
        IFamilyService.PercentComplete(r), r.UpdatedAt);

    public static FamilyMemberDto ToDto(this FamilyMember m) => new(m.Id, m.DisplayName, m.Relation, m.State);

    public static FamilyNoteDto ToDto(this FamilyNote n) => new(n.Id, n.AuthorName, n.Body, n.CreatedAt);

    public static InviteDto ToDto(this FamilyInvite i) => new(i.MemberId, i.Code, i.ExpiresAt);

    public static MediaAssetDto ToDto(this MediaAsset a) => new(a.Id, a.Kind, a.ContentType, a.Length, a.Caption, a.CreatedAt);
}
