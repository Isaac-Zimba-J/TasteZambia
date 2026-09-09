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
