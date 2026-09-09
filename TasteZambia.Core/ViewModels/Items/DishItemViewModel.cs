using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels.Items;

public sealed partial class DishItemViewModel : ObservableObject
{
    private readonly IFavouritesService _favourites;
    private readonly INavigationService _navigation;

    public string Id { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string Region { get; }
    public string MetaLabel { get; }
    public string TimeLabel { get; }
    public string Difficulty { get; }
    public string Description { get; }
    public string? ImageAsset { get; }
    public string PhotoCaption { get; }

    public DishItemViewModel(Dish dish, IFavouritesService favourites,
                             IPreferenceService preferences, INavigationService navigation)
    {
        _favourites = favourites;
        _navigation = navigation;

        var name = preferences.Resolve(dish.LocalName, dish.EnglishName);

        Id = dish.Id;
        Title = name.Title;
        Subtitle = name.Subtitle;
        Region = dish.Region;
        MetaLabel = dish.MetaLabel;
        TimeLabel = dish.TimeLabel;
        Difficulty = dish.Difficulty;
        Description = dish.Description;
        ImageAsset = dish.ImageAsset;
        PhotoCaption = dish.PhotoNeededCaption;

        _isSaved = favourites.IsSaved(dish.Id);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveGlyph))]
    [NotifyPropertyChangedFor(nameof(SaveColorHex))]
    [NotifyPropertyChangedFor(nameof(SaveSemanticLabel))]
    private bool _isSaved;

    /// <summary>Filled heart when saved, outline heart when not.</summary>
    public string SaveGlyph => IsSaved ? "♥" : "♡";

    public string SaveColorHex => IsSaved ? "#A3452A" : "#57493A";

    /// <summary>Screen readers otherwise announce the bare glyph.</summary>
    public string SaveSemanticLabel => IsSaved ? $"Remove {Title} from saved" : $"Save {Title}";

    [RelayCommand]
    private void ToggleSave()
    {
        _favourites.Toggle(Id);
        IsSaved = _favourites.IsSaved(Id);
    }

    [RelayCommand]
    private Task Open()
        => _navigation.GoToAsync("recipe", new Dictionary<string, object> { ["dishId"] = Id });
}
