namespace TasteZambia.Core.Models;

public sealed record LocalName(string Language, string Name);

public sealed record Ingredient
{
    public required string Key { get; init; }
    public required string LocalName { get; init; }
    public required string EnglishName { get; init; }
    public required string Description { get; init; }
    public required string WhereFound { get; init; }
    public required string TraditionalPreparation { get; init; }
    public required IReadOnlyList<LocalName> LocalNames { get; init; }

    /// <summary>Languages whose name for this ingredient is not yet recorded, comma separated.</summary>
    public required string PendingLanguages { get; init; }

    /// <summary>Dish ids this ingredient is used in.</summary>
    public required IReadOnlyList<string> UsedInDishIds { get; init; }

    public string? ImageAsset { get; init; }
    public bool HasPhoto => !string.IsNullOrEmpty(ImageAsset);
}

/// <summary>One line of a recipe's ingredient list. Linked entries open the ingredient sheet.</summary>
public sealed record RecipeIngredient
{
    public string? IngredientKey { get; init; }
    public required string DisplayName { get; init; }
    public required string DisplaySubtitle { get; init; }
    public required string Quantity { get; init; }
    public bool IsLinked => !string.IsNullOrEmpty(IngredientKey);
}
