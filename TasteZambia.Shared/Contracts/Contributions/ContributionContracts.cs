using System.ComponentModel.DataAnnotations;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Shared.Contracts.Contributions;

public sealed record ContributionIngredientDto(
    string? IngredientKey,
    [Required, MaxLength(120)] string DisplayName,
    [MaxLength(120)] string DisplaySubtitle,
    [MaxLength(80)] string Quantity);

/// <summary>A draft leaving the phone. Arrives on the server already InReview.</summary>
public sealed record SubmitContributionRequest(
    [Required, MaxLength(120)] string LocalName,
    [Required, MaxLength(400)] string EnglishDescription,
    [Required, MaxLength(40)] string Province,
    [MaxLength(40)] string MealType,
    [MaxLength(40)] string Language,
    IReadOnlyList<ContributionIngredientDto> Ingredients,
    [MinLength(1)] IReadOnlyList<string> Steps,
    string Origin,
    string CulturalSignificance,
    string TraditionalMethod,
    string TaughtBy,
    string TaughtByOrigin,
    bool CreditTeacher);

public sealed record FlagAnswerDto(int FlagId, [Required] string Answer);
public sealed record ResubmitRequest(IReadOnlyList<FlagAnswerDto> Answers);

public sealed record ReviewEventDto(ReviewEventKind Kind, DateTimeOffset At, string? Actor, string? Note);
public sealed record FlaggedFieldDto(int Id, string Field, string Question, string CurrentValue, string? Answer);

public sealed record ContributionSummaryDto(
    Guid Id,
    string LocalName,
    string Province,
    ContributionStatus Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset UpdatedAt,
    string? PublishedDishId);

public sealed record ContributionDetailDto(
    Guid Id,
    string LocalName,
    string EnglishDescription,
    string Province,
    ContributionStatus Status,
    DateTimeOffset SubmittedAt,
    string? PublishedDishId,
    string ContributorName,
    string ContributorLocation,
    string TaughtBy,
    string TaughtByOrigin,
    bool CreditTeacher,
    IReadOnlyList<ReviewEventDto> Events,
    IReadOnlyList<FlaggedFieldDto> Flags);
