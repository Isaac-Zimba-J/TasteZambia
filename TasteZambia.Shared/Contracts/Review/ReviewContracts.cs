using System.ComponentModel.DataAnnotations;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Shared.Contracts.Review;

public sealed record FlagRequestDto([Required, MaxLength(80)] string Field, [Required] string Question, string CurrentValue);

public sealed record RequestChangesRequest([Required] string Note, IReadOnlyList<FlagRequestDto> Flags);

public sealed record ReviewQueueItemDto(
    Guid Id,
    string LocalName,
    string Province,
    string ContributorName,
    ContributionStatus Status,
    DateTimeOffset SubmittedAt,
    int OpenFlags);

public sealed record PublishResultDto(Guid ContributionId, string DishId);
