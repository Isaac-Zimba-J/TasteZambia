using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels.Items;

namespace TasteZambia.Core.ViewModels;

public sealed partial class FilterChipViewModel(string label, Action<FilterChipViewModel> onSelect)
    : ObservableObject
{
    public string Label { get; } = label;

    [ObservableProperty]
    private bool _isSelected;

    [RelayCommand]
    private void Select() => onSelect(this);
}

public sealed partial class ExploreViewModel(
    ICatalogService catalog,
    IFavouritesService favourites,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    private Task _search = Task.CompletedTask;
    private string _filter = "All";

    public ObservableCollection<FilterChipViewModel> Chips { get; } = [];
    public ObservableCollection<DishItemViewModel> Results { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQuery))]
    private string _query = "";

    [ObservableProperty]
    private string _resultCountLabel = "";

    [ObservableProperty]
    private bool _noResults;

    public bool HasQuery => Query.Trim().Length > 0;

    public override async Task InitializeAsync()
    {
        if (Chips.Count == 0)
        {
            foreach (var label in SeedData.Filters)
                Chips.Add(new FilterChipViewModel(label, SelectChip));

            Chips[0].IsSelected = true;
        }

        await RunSearchAsync();
    }

    partial void OnQueryChanged(string value) => _search = RunSearchAsync();

    /// <summary>Lets tests await the search kicked off by setting <see cref="Query"/>.</summary>
    public Task WaitForSearchAsync() => _search;

    private void SelectChip(FilterChipViewModel chip)
    {
        foreach (var c in Chips)
            c.IsSelected = ReferenceEquals(c, chip);

        _filter = chip.Label;
        _search = RunSearchAsync();
    }

    private async Task RunSearchAsync()
    {
        var dishes = await catalog.SearchAsync(Query, _filter);

        Results.Clear();
        foreach (var dish in dishes)
            Results.Add(new DishItemViewModel(dish, favourites, preferences, Navigation));

        NoResults = Results.Count == 0;
        ResultCountLabel = $"{Results.Count} {(Results.Count == 1 ? "recipe" : "recipes")}";
    }

    [RelayCommand]
    private void ClearQuery() => Query = "";
}
