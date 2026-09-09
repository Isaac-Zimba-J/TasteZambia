namespace TasteZambia.Core.Models;

public sealed record Dish
{
    public required string Id { get; init; }
    public required string LocalName { get; init; }
    public required string EnglishName { get; init; }
    public required string Region { get; init; }
    public required string TimeLabel { get; init; }
    public required string Difficulty { get; init; }
    public required string Description { get; init; }

    /// <summary>Resource image name, or null when photography is still missing.</summary>
    public string? ImageAsset { get; init; }

    /// <summary>Caption shown inside the striped placeholder when <see cref="ImageAsset"/> is null.</summary>
    public string PhotoNeededCaption { get; init; } = "photo needed";

    public string? PrepTime { get; init; }
    public string? CookTime { get; init; }

    public bool HasPhoto => !string.IsNullOrEmpty(ImageAsset);

    /// <summary>"45 min  ·  Easy" — two spaces either side of the interpunct, per the design.</summary>
    public string MetaLabel => $"{TimeLabel}  ·  {Difficulty}";
}

public sealed record RecipeDetail
{
    public required Dish Dish { get; init; }
    public required string Subtitle { get; init; }
    public bool IsVerified { get; init; } = true;
    public required IReadOnlyList<string> CulturalContext { get; init; }
    public required IReadOnlyList<RecipeIngredient> Ingredients { get; init; }
    public required IReadOnlyList<CookingStep> Steps { get; init; }
    public required MethodNarrative TraditionalMethod { get; init; }
    public required MethodNarrative ModernMethod { get; init; }
    public required IReadOnlyList<RegionalVariation> Variations { get; init; }
    public required Contributor Contributor { get; init; }
}

public sealed record Contributor(string Name, string Location, string? AvatarAsset);
