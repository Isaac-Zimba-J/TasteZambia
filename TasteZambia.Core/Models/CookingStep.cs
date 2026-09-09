namespace TasteZambia.Core.Models;

public sealed record CookingStep(int Number, string Title, string Body);

public sealed record RegionalVariation(string Place, string Description);

public sealed record MethodNarrative(string Heading, IReadOnlyList<string> Paragraphs);

public enum PreparationMethod
{
    Traditional,
    Modern
}
