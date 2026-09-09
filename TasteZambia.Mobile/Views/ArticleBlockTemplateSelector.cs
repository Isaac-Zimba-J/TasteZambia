using TasteZambia.Core.Models;

namespace TasteZambia.Mobile.Views;

/// <summary>
/// A story's lede, body paragraphs and pull-quote are typographically distinct -
/// serif lede, sans body, italic serif quote on cream - so each block kind gets
/// its own template rather than one template branching on flags.
/// </summary>
public sealed class ArticleBlockTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Lede { get; set; }
    public DataTemplate? Paragraph { get; set; }
    public DataTemplate? PullQuote { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        => item is ArticleBlock block
            ? block.Kind switch
            {
                ArticleBlockKind.Lede => Lede!,
                ArticleBlockKind.PullQuote => PullQuote!,
                _ => Paragraph!,
            }
            : Paragraph!;
}
