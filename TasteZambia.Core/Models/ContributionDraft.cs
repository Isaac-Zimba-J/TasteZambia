namespace TasteZambia.Core.Models;

public enum PrivacyLevel { PrivateToMe, SharedWithFamily, PublicInArchive }

public sealed record ReviewStage(string Label, string Note, bool IsComplete);

/// <summary>Mutable working state for the Share and Preserve wizards.</summary>
public sealed class ContributionDraft
{
    public string LocalName { get; set; } = "";
    public string EnglishDescription { get; set; } = "";
    public string Province { get; set; } = "";
    public string MealType { get; set; } = "";
    public string Language { get; set; } = "";
    public List<RecipeIngredient> Ingredients { get; set; } = [];
    public List<string> Steps { get; set; } = [];
    public string Origin { get; set; } = "";
    public string CulturalSignificance { get; set; } = "";
    public string TraditionalMethod { get; set; } = "";
    public string TaughtBy { get; set; } = "";
    public string TaughtByOrigin { get; set; } = "";
    public string Story { get; set; } = "";
    public bool CreditTeacher { get; set; } = true;
    public bool AddToFoodStories { get; set; } = true;
    public PrivacyLevel Privacy { get; set; } = PrivacyLevel.SharedWithFamily;
    public List<string> PhotoPaths { get; set; } = [];
}

public enum ReviewState { Done, InProgress, Pending }

public sealed record RecipeDraft(string Name, int PercentComplete, string Missing, string When, string TintHex)
{
    public string PercentLabel => $"{PercentComplete}%";
    public double Fraction => PercentComplete / 100.0;
}

public sealed record ReviewStep(string Label, string When, string Note, ReviewState State, bool IsNotLast)
{
    /// <summary>Green when done, gold decoration in progress, the pale idle tone when not yet reached.</summary>
    public string DotHex => State switch
    {
        ReviewState.Done => "#2F6A4D",
        ReviewState.InProgress => "#C07F1E",
        _ => "#D8CDB9",
    };

    public string LabelHex => State == ReviewState.Pending ? "#7A6B59" : "#221A12";
}

public sealed record FlaggedField(string Field, string Question, string CurrentValue);
