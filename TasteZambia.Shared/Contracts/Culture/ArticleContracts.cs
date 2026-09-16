namespace TasteZambia.Shared.Contracts.Culture;

public enum ArticleBlockKind { Lede = 0, Paragraph = 1, PullQuote = 2 }

public sealed record ArticleBlockDto(ArticleBlockKind Kind, string Text);

public sealed record AudioNarrationDto(string Label, string Duration, double Progress);

public sealed record ArticleDto
{
    public required string Id { get; init; }
    public required string Kicker { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required string Meta { get; init; }
    public string? Lede { get; init; }
    public bool IsLead { get; init; }
    public string? ImageAsset { get; init; }
    public string PhotoNeededCaption { get; init; } = "photo needed";
    public IReadOnlyList<ArticleBlockDto> Body { get; init; } = [];
    public IReadOnlyList<string> RelatedDishIds { get; init; } = [];
    public AudioNarrationDto? Audio { get; init; }
}
