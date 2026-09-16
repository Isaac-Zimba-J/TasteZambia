namespace TasteZambia.Shared.Contracts.Regions;

public sealed record ProvinceDto
{
    public required string Name { get; init; }
    public required string Seat { get; init; }
    public required string Blurb { get; init; }
    public required string CookingTradition { get; init; }
    public required IReadOnlyList<string> SignatureFoods { get; init; }
    public required IReadOnlyList<string> CommonIngredients { get; init; }
}
