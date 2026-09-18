using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Models;

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

/// <summary>A draft on this phone. Drafts never leave the device until they are submitted.</summary>
public sealed class LocalDraft
{
    public Guid Id { get; set; }
    public DateTimeOffset EditedAt { get; set; }
    public ContributionDraft Draft { get; set; } = new();
}

/// <summary>The Drafts screen's card: how far along a local draft is, and what it still needs.</summary>
public sealed record RecipeDraft(string Name, int PercentComplete, string Missing, string When, string TintHex)
{
    public Guid Id { get; init; }
    public string PercentLabel => $"{PercentComplete}%";
    public double Fraction => PercentComplete / 100.0;

    private static readonly (Func<ContributionDraft, bool> Filled, string Label)[] Checks =
    [
        (d => d.LocalName.Length > 0, "a local name"),
        (d => d.EnglishDescription.Length > 0, "a short English description"),
        (d => d.Province.Length > 0, "a province"),
        (d => d.MealType.Length > 0, "a meal type"),
        (d => d.Ingredients.Count > 0, "at least one ingredient"),
        (d => d.Steps.Count >= 2, "one more cooking step"),
        (d => d.Origin.Length > 0, "where it is cooked"),
        (d => d.CulturalSignificance.Length > 0, "what it means"),
        (d => d.TraditionalMethod.Length > 0, "the traditional method"),
    ];

    public static RecipeDraft From(LocalDraft local, DateTimeOffset now)
    {
        var d = local.Draft;
        var filled = Checks.Count(c => c.Filled(d));
        var percent = (int)Math.Round(filled * 100.0 / Checks.Length);
        var missing = Checks.FirstOrDefault(c => !c.Filled(d)).Label;
        return new RecipeDraft(
            d.LocalName.Length > 0 ? d.LocalName : "Untitled recipe",
            percent,
            missing is null ? "Ready to submit" : $"Needs {missing}",
            $"Edited {Relative(now - local.EditedAt)}",
            percent >= 80 ? "#2F6A4D" : percent >= 40 ? "#C07F1E" : "#A3452A")
        { Id = local.Id };
    }

    public static string Relative(TimeSpan ago)
    {
        if (ago < TimeSpan.FromMinutes(1)) return "just now";
        if (ago < TimeSpan.FromHours(1)) return Plural((int)ago.TotalMinutes, "minute");
        if (ago < TimeSpan.FromDays(1)) return Plural((int)ago.TotalHours, "hour");
        if (ago < TimeSpan.FromDays(7)) return Plural((int)ago.TotalDays, "day");
        if (ago < TimeSpan.FromDays(30)) return Plural((int)(ago.TotalDays / 7), "week");
        return Plural((int)(ago.TotalDays / 30), "month");
    }

    private static string Plural(int n, string unit) => $"{n} {unit}{(n == 1 ? "" : "s")} ago";
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

/// <summary>A reviewer's question about one field. <see cref="Answer"/> is what the contributor types back.</summary>
public sealed record FlaggedField(string Field, string Question, string CurrentValue)
{
    public int Id { get; init; }
    public string Answer { get; set; } = "";
}
