using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels.Items;

namespace TasteZambia.Core.ViewModels;

public sealed record StoryTeaser(string Title, string Meta);

public sealed partial class HomeViewModel(
    ICatalogService catalog,
    ICategoryRepository categories,
    IFavouritesService favourites,
    IPreferenceService preferences,
    IProfileRepository profiles,
    IArchiveSearchService search,
    INavigationService navigation) : BaseViewModel(navigation)
{
    /// <summary>Results across the whole archive, not only its recipes.</summary>
    public ObservableCollection<SearchHit> Results { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<DishItemViewModel> Dishes { get; } = [];
    public ObservableCollection<StoryTeaser> Stories { get; } = [];

    protected override bool HasContent => Dishes.Count > 0;

    // ---- Who is reading. The header used to show a stock photograph of somebody else. ----

    /// <summary>The reader's own photograph, when they have one. Empty until then.</summary>
    [ObservableProperty] private string _avatarAsset = "";

    /// <summary>Their initials, for the circle when there is no photograph - which is usually.</summary>
    [ObservableProperty] private string _initials = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAvatar))]
    private bool _hasProfileName;

    public bool HasAvatar => AvatarAsset.Length > 0;

    // ---- Search, from the first screen, across everything ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearching))]
    [NotifyPropertyChangedFor(nameof(HasNoResults))]
    private string _query = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoResults))]
    private bool _searched;

    /// <summary>While a query is being typed, the results take the screen over.</summary>
    public bool IsSearching => Query.Trim().Length >= ArchiveSearchService.MinimumQueryLength;

    public bool HasNoResults => IsSearching && Searched && Results.Count == 0;

    private Task _search = Task.CompletedTask;
    private CancellationTokenSource? _searchCts;

    /// <summary>Lets a test await the search a typed query kicked off.</summary>
    public Task WaitForSearchAsync() => _search;

    partial void OnQueryChanged(string value) => _search = RunSearchAsync();

    private async Task RunSearchAsync()
    {
        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();

        if (!IsSearching)
        {
            Results.Clear();
            Searched = false;
            OnPropertyChanged(nameof(HasNoResults));
            return;
        }

        try
        {
            var hits = await search.SearchAsync(Query, cts.Token);

            // A slower earlier query must not overwrite a newer one's results.
            if (cts.IsCancellationRequested) return;

            Results.Clear();
            foreach (var hit in hits) Results.Add(hit);
            Searched = true;
            OnPropertyChanged(nameof(HasNoResults));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Offline, or superseded. The archive's own screens report their own failures;
            // a search box that empties itself is enough here.
            Results.Clear();
            Searched = true;
            OnPropertyChanged(nameof(HasNoResults));
        }
    }

    [RelayCommand]
    private Task OpenResult(SearchHit? hit)
    {
        if (hit is null) return Task.CompletedTask;

        Query = "";   // leaving the results up behind the reader is disorienting
        return hit.ParameterName.Length == 0
            ? Navigation.GoToAsync($"//{hit.Route}")
            : Navigation.GoToAsync(hit.Route, new Dictionary<string, object> { [hit.ParameterName] = hit.ParameterValue });
    }

    [RelayCommand] private void ClearSearch() => Query = "";

    [RelayCommand] private Task OpenProfile() => Navigation.GoToAsync(HasProfileName ? "//profile" : "profileEdit");

    private async Task LoadProfileAsync()
    {
        try
        {
            var profile = await profiles.GetAsync();
            AvatarAsset = profile.AvatarAsset;
            HasProfileName = profile.Name != Data.Http.HttpProfileRepository.DefaultName && profile.Name.Length > 0;
            Initials = HasProfileName ? InitialsOf(profile.Name) : "";
            OnPropertyChanged(nameof(HasAvatar));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // The rest of Home is worth showing without it.
        }
    }

    /// <summary>At most two letters, from the first and last word of whatever they typed.</summary>
    private static string InitialsOf(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            0 => "",
            1 => words[0][..1].ToUpperInvariant(),
            _ => (words[0][..1] + words[^1][..1]).ToUpperInvariant(),
        };
    }

    protected override void ClearForReload()
    {
        Categories.Clear();
        Dishes.Clear();
        Stories.Clear();
    }

    public override async Task InitializeAsync()
    {
        // The reader's own details are cheap and change independently of the archive, so they
        // are refreshed on every visit rather than guarded with the rest.
        await LoadProfileAsync();

        if (Dishes.Count > 0) return;

        IsBusy = true;
        try
        {
            foreach (var category in await categories.GetAllAsync())
                Categories.Add(category);

            foreach (var dish in await catalog.SearchAsync("", "All"))
                Dishes.Add(new DishItemViewModel(dish, favourites, preferences, Navigation));

            foreach (var (title, meta) in SeedData.HomeStories)
                Stories.Add(new StoryTeaser(title, meta));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand] private Task OpenExplore() => Navigation.GoToAsync("//explore");
    [RelayCommand] private Task OpenCulture() => Navigation.GoToAsync("//culture");
}
