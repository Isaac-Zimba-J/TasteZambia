namespace TasteZambia.Shared.Contracts.Dishes;

public sealed record DishDto
{
    public required string Id { get; init; }
    public required string LocalName { get; init; }
    public required string EnglishName { get; init; }
    public required string Region { get; init; }
    public required string TimeLabel { get; init; }
    public required string Difficulty { get; init; }
    public required string Description { get; init; }

    /// <summary>Resource image name, or null while photography is still missing.</summary>
    public string? ImageAsset { get; init; }

    /// <summary>Caption for the striped placeholder shown when ImageAsset is null.</summary>
    public string PhotoNeededCaption { get; init; } = "photo needed";

    public string? PrepTime { get; init; }
    public string? CookTime { get; init; }
}

public sealed record ContributorDto(string Name, string Location, string? AvatarAsset);

public sealed record RecipeIngredientDto
{
    /// <summary>Set when this line links to an archived ingredient; null for plain items like Salt.</summary>
    public string? IngredientKey { get; init; }
    public required string DisplayName { get; init; }
    public required string DisplaySubtitle { get; init; }
    public required string Quantity { get; init; }
}

public sealed record CookingStepDto(int Number, string Title, string Body);

public sealed record MethodNarrativeDto(string Heading, IReadOnlyList<string> Paragraphs);

public sealed record RegionalVariationDto(string Place, string Description);

public sealed record RecipeDto
{
    public required DishDto Dish { get; init; }
    public required string Subtitle { get; init; }
    public bool IsVerified { get; init; }
    public required IReadOnlyList<string> CulturalContext { get; init; }
    public required IReadOnlyList<RecipeIngredientDto> Ingredients { get; init; }
    public required IReadOnlyList<CookingStepDto> Steps { get; init; }
    public required MethodNarrativeDto TraditionalMethod { get; init; }
    public required MethodNarrativeDto ModernMethod { get; init; }
    public required IReadOnlyList<RegionalVariationDto> Variations { get; init; }
    public required ContributorDto Contributor { get; init; }
}
