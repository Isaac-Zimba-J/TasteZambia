namespace TasteZambia.Core.Models;

public enum ArticleBlockKind { Lede, Paragraph, PullQuote }

public sealed record ArticleBlock(ArticleBlockKind Kind, string Text);

public sealed record Article
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
    public IReadOnlyList<ArticleBlock> Body { get; init; } = [];
    public IReadOnlyList<string> RelatedDishIds { get; init; } = [];
    public AudioNarration? Audio { get; init; }
    public bool HasPhoto => !string.IsNullOrEmpty(ImageAsset);
}

public sealed record AudioNarration(string Label, string Duration, double Progress);
