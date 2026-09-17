using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Data.Entities;

/// <summary>
/// A recipe submitted for the public archive. Arrives as InReview (drafts live on the
/// phone), moves through the state machine in ContributionService, and on Published
/// becomes a Dish + Recipe with Provenance.Community.
/// </summary>
public class Contribution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public ContributionStatus Status { get; set; }

    public required string LocalName { get; set; }
    public required string EnglishDescription { get; set; }
    public required string Province { get; set; }
    public string MealType { get; set; } = "";
    public string Language { get; set; } = "";
    public string Origin { get; set; } = "";
    public string CulturalSignificance { get; set; } = "";
    public string TraditionalMethod { get; set; } = "";
    public string TaughtBy { get; set; } = "";
    public string TaughtByOrigin { get; set; } = "";
    public bool CreditTeacher { get; set; } = true;

    // Snapshot of the profile at submission: the credit line must not change if the profile does.
    public required string ContributorName { get; set; }
    public required string ContributorLocation { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? PublishedDishId { get; set; }

    public List<ContributionIngredient> Ingredients { get; set; } = [];
    public List<ContributionStep> Steps { get; set; } = [];
    public List<ReviewEvent> Events { get; set; } = [];
    public List<FlaggedField> Flags { get; set; } = [];
}

public class ContributionIngredient
{
    public int Id { get; set; }
    public Guid ContributionId { get; set; }
    public int SortOrder { get; set; }
    public string? IngredientKey { get; set; }
    public required string DisplayName { get; set; }
    public required string DisplaySubtitle { get; set; }
    public required string Quantity { get; set; }
}

public class ContributionStep
{
    public int Id { get; set; }
    public Guid ContributionId { get; set; }
    public int SortOrder { get; set; }
    public required string Text { get; set; }
}

public class ReviewEvent
{
    public int Id { get; set; }
    public Guid ContributionId { get; set; }
    public ReviewEventKind Kind { get; set; }
    public DateTimeOffset At { get; set; }
    /// <summary>The reviewer's display name for review events; null for contributor actions.</summary>
    public string? Actor { get; set; }
    public string? Note { get; set; }
}

/// <summary>A question from the reviewer about one field. Answered by the contributor on resubmit.</summary>
public class FlaggedField
{
    public int Id { get; set; }
    public Guid ContributionId { get; set; }
    public required string Field { get; set; }
    public required string Question { get; set; }
    public required string CurrentValue { get; set; }
    public string? Answer { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
}
