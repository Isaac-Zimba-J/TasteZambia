namespace TasteZambia.Shared.Contracts.Ingredients;

public sealed record LocalNameDto(string Language, string Name);

public sealed record IngredientDto
{
    public required string Key { get; init; }
    public required string LocalName { get; init; }
    public required string EnglishName { get; init; }
    public required string Description { get; init; }
    public required string WhereFound { get; init; }
    public required string TraditionalPreparation { get; init; }
    public required IReadOnlyList<LocalNameDto> LocalNames { get; init; }

    /// <summary>Languages whose name for this ingredient is not yet recorded.</summary>
    public required string PendingLanguages { get; init; }

    public required IReadOnlyList<string> UsedInDishIds { get; init; }
    public string? ImageAsset { get; init; }
}
