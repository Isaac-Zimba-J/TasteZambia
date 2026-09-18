using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Models;

public sealed record UserProfile
{
    public required string Name { get; init; }
    public required string Location { get; init; }
    public required string Languages { get; init; }
    public required string AvatarAsset { get; init; }
    public required int CookedCount { get; init; }
    public required int FavouriteCount { get; init; }
    public required int ContributedCount { get; init; }
    public required int PreservedCount { get; init; }
}

/// <summary><paramref name="Tint"/> is the hex swatch on the collection's left edge.</summary>
public sealed record RecipeCollection(string Label, string CountLabel, string Tint);

public sealed record Contribution(Guid Id, string Name, ContributionStatus Status, string Meta, string? PublishedDishId = null);
