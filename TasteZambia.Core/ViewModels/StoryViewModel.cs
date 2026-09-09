using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels.Items;

namespace TasteZambia.Core.ViewModels;

public sealed partial class StoryViewModel(
    IArticleRepository articles,
    ICatalogService catalog,
    IFavouritesService favourites,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    /// <summary>Set from the navigation parameters. Defaults to the one fully written essay.</summary>
    public string ArticleId { get; set; } = "nshima";

    public ObservableCollection<ArticleBlock> Blocks { get; } = [];
    public ObservableCollection<DishItemViewModel> RelatedDishes { get; } = [];

    [ObservableProperty] private string _kicker = "";
    [ObservableProperty] private string _titleText = "";
    [ObservableProperty] private string _author = "";
    [ObservableProperty] private string _readMeta = "";
    [ObservableProperty] private string _heroCaption = "";
    [ObservableProperty] private bool _hasAudio;
    [ObservableProperty] private string _audioLabel = "";
    [ObservableProperty] private string _audioDuration = "";
    [ObservableProperty] private double _audioProgress;
    [ObservableProperty] private bool _hasRelatedDishes;

    public override async Task InitializeAsync()
    {
        if (Blocks.Count > 0) return;

        var article = await articles.GetByIdAsync(ArticleId);
        if (article is null) return;

        // "8 min read · Pan-Zambian" -> the kicker wants the region half.
        var region = article.Meta.Contains('·')
            ? article.Meta[(article.Meta.IndexOf('·') + 1)..].Trim()
            : article.Meta;

        Kicker = $"{article.Kicker} · {region}";
        TitleText = article.Title;
        Author = article.Author;
        ReadMeta = article.Meta;
        HeroCaption = article.PhotoNeededCaption;

        if (article.Audio is { } audio)
        {
            HasAudio = true;
            AudioLabel = audio.Label;
            AudioDuration = audio.Duration;
            AudioProgress = audio.Progress;
        }

        foreach (var block in article.Body)
            Blocks.Add(block);

        foreach (var dish in await catalog.GetDishesByIdsAsync(article.RelatedDishIds))
            RelatedDishes.Add(new DishItemViewModel(dish, favourites, preferences, Navigation));

        HasRelatedDishes = RelatedDishes.Count > 0;
    }

    [RelayCommand]
    private Task Back() => Navigation.GoBackAsync();
}
