using System.ComponentModel.DataAnnotations;
using TasteZambia.Shared.Contracts.Media;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Shared.Contracts.Family;

public sealed record CreateFamilyRecipeRequest(
    [Required, MaxLength(120)] string LocalName,
    [MaxLength(400)] string Description,
    [MaxLength(40)] string Province,
    [MaxLength(40)] string Language,
    [MaxLength(200)] string TaughtBy,
    [MaxLength(200)] string TaughtByOrigin,
    string Story,
    string TraditionalMethod,
    PrivacyLevel Privacy);

public sealed record UpdateFamilyRecipeRequest(
    [Required, MaxLength(120)] string LocalName,
    [MaxLength(400)] string Description,
    [MaxLength(40)] string Province,
    [MaxLength(40)] string Language,
    [MaxLength(200)] string TaughtBy,
    [MaxLength(200)] string TaughtByOrigin,
    string Story,
    string TraditionalMethod);

public sealed record AddMemberRequest([Required, MaxLength(120)] string DisplayName, [MaxLength(120)] string Relation);
public sealed record AcceptInviteRequest([Required, MinLength(8), MaxLength(8)] string Code);
public sealed record AddNoteRequest([Required, MaxLength(2000)] string Body);
public sealed record SetPrivacyRequest(PrivacyLevel Privacy);

public sealed record FamilyMemberDto(Guid Id, string DisplayName, string Relation, MemberState State);
public sealed record FamilyNoteDto(Guid Id, string AuthorName, string Body, DateTimeOffset CreatedAt);
public sealed record InviteDto(Guid MemberId, string Code, DateTimeOffset ExpiresAt);

public sealed record FamilyRecipeSummaryDto(
    Guid Id, string LocalName, string TaughtBy, PrivacyLevel Privacy,
    int PhotoCount, int NoteCount, bool HasAudio, int PercentComplete, DateTimeOffset UpdatedAt);

public sealed record FamilyRecipeDto(
    Guid Id, string LocalName, string Description, string Province, string Language,
    string TaughtBy, string TaughtByOrigin, string Story, string TraditionalMethod,
    PrivacyLevel Privacy, TranscriptState Transcript, string? PublishedDishId,
    int PercentComplete, bool IsOwner, DateTimeOffset UpdatedAt,
    IReadOnlyList<FamilyMemberDto> Members,
    IReadOnlyList<FamilyNoteDto> Notes,
    IReadOnlyList<MediaAssetDto> Media);
