using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class ArticleRowViewModel(Article article, INavigationService navigation)
{
    public string Id { get; } = article.Id;
    public string Kicker { get; } = article.Kicker;
    public string TitleText { get; } = article.Title;
    public string Byline { get; } = $"{article.Author} · {article.Meta}";
    public string PhotoCaption { get; } = article.PhotoNeededCaption;
    public string? ImageAsset { get; } = article.ImageAsset;

    [RelayCommand]
    private Task Open()
        => navigation.GoToAsync("story", new Dictionary<string, object> { ["articleId"] = Id });
}

public sealed partial class CultureViewModel(
    IArticleRepository articles,
    INavigationService navigation) : BaseViewModel(navigation)
{
    private string _leadId = "";

    public ObservableCollection<ArticleRowViewModel> Articles { get; } = [];

    [ObservableProperty] private string _leadKicker = "";
    [ObservableProperty] private string _leadTitle = "";
    [ObservableProperty] private string _leadLede = "";
    [ObservableProperty] private string _leadByline = "";
    [ObservableProperty] private string _leadCaption = "";

    protected override bool HasContent => Articles.Count > 0;

    protected override void ClearForReload() => Articles.Clear();

    public override async Task InitializeAsync()
    {
        if (Articles.Count > 0) return;

        var all = await articles.GetAllAsync();
        var lead = all.First(a => a.IsLead);

        _leadId = lead.Id;
        LeadKicker = lead.Kicker;
        LeadTitle = lead.Title;
        LeadLede = lead.Lede ?? "";
        LeadByline = $"{lead.Author} · {lead.Meta}";
        LeadCaption = lead.PhotoNeededCaption;

        foreach (var article in all.Where(a => !a.IsLead))
            Articles.Add(new ArticleRowViewModel(article, Navigation));
    }

    [RelayCommand]
    private Task OpenLead()
        => Navigation.GoToAsync("story", new Dictionary<string, object> { ["articleId"] = _leadId });
}
