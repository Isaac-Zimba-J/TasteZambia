using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.ViewModels;

public sealed partial class RecipeIngredientItemViewModel(
    RecipeIngredient ingredient, Action<string> openSheet) : ObservableObject
{
    public string Name { get; } = ingredient.DisplayName;
    public string Subtitle { get; } = ingredient.DisplaySubtitle;
    public string Quantity { get; } = ingredient.Quantity;
    public bool IsLinked { get; } = ingredient.IsLinked;
    public bool IsPlain => !IsLinked;

    [RelayCommand]
    private void Open()
    {
        if (ingredient.IngredientKey is { } key)
            openSheet(key);
    }
}

public sealed partial class CookingStepItemViewModel : ObservableObject
{
    private readonly ICookingProgressService _progress;
    private readonly string _dishId;
    private readonly Action _notifyParent;

    public int Number { get; }
    public string NumberLabel { get; }
    public string StepTitle { get; }
    public string Body { get; }

    public CookingStepItemViewModel(CookingStep step, string dishId,
                                    ICookingProgressService progress, Action notifyParent)
    {
        _progress = progress;
        _dishId = dishId;
        _notifyParent = notifyParent;

        Number = step.Number;
        NumberLabel = step.Number.ToString();
        StepTitle = step.Title;
        Body = step.Body;
        _isDone = progress.IsDone(dishId, step.Number);
    }

    [ObservableProperty]
    private bool _isDone;

    [RelayCommand]
    private void Toggle()
    {
        _progress.Toggle(_dishId, Number);
        IsDone = _progress.IsDone(_dishId, Number);
        _notifyParent();
    }
}

public sealed record LocalNameRow(string Language, string Name);
public sealed record UsedInRow(string Name, string Subtitle);

public sealed class IngredientSheetViewModel(Ingredient ingredient, IReadOnlyList<UsedInRow> usedIn)
{
    public string Key { get; } = ingredient.Key;
    public string Name { get; } = ingredient.LocalName;
    public string EnglishName { get; } = ingredient.EnglishName;
    public string Description { get; } = ingredient.Description;
    public string WhereFound { get; } = ingredient.WhereFound;
    public string TraditionalPreparation { get; } = ingredient.TraditionalPreparation;
    public IReadOnlyList<LocalNameRow> LocalNames { get; } =
        ingredient.LocalNames.Select(l => new LocalNameRow(l.Language, l.Name)).ToList();
    public IReadOnlyList<UsedInRow> UsedIn { get; } = usedIn;
}

public sealed partial class RecipeViewModel(
    IDishRepository dishes,
    IIngredientRepository ingredients,
    IFavouritesService favourites,
    ICookingProgressService progress,
    IPreferenceService preferences,
    INavigationService navigation) : BaseViewModel(navigation)
{
    private RecipeDetail? _recipe;

    /// <summary>Set from the navigation parameters. Defaults to the one seeded recipe.</summary>
    public string DishId { get; set; } = "ifisashi";

    public ObservableCollection<string> CulturalContext { get; } = [];
    public ObservableCollection<RecipeIngredientItemViewModel> Ingredients { get; } = [];
    public ObservableCollection<CookingStepItemViewModel> Steps { get; } = [];
    public ObservableCollection<RegionalVariation> Variations { get; } = [];
    public ObservableCollection<string> MethodParagraphs { get; } = [];

    [ObservableProperty] private string _subtitle = "";
    [ObservableProperty] private bool _isVerified;
    [ObservableProperty] private string _prepTime = "";
    [ObservableProperty] private string _cookTime = "";
    [ObservableProperty] private string _difficulty = "";
    [ObservableProperty] private string _region = "";
    [ObservableProperty] private string? _heroAsset;
    [ObservableProperty] private string _heroCaption = "";
    [ObservableProperty] private string _stepProgressLabel = "";
    [ObservableProperty] private string _methodHeading = "";
    [ObservableProperty] private string _contributorName = "";
    [ObservableProperty] private string _contributorAvatar = "";
    [ObservableProperty] private string _ingredientCountLabel = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModern))]
    private bool _isTraditional = true;

    public bool IsModern => !IsTraditional;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveColorHex))]
    [NotifyPropertyChangedFor(nameof(SaveSemanticLabel))]
    private bool _isSaved;

    public string SaveColorHex => IsSaved ? "#A3452A" : "#4A3D2E";
    public string SaveSemanticLabel => IsSaved ? $"Remove {Title} from saved" : $"Save {Title}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSheetOpen))]
    private IngredientSheetViewModel? _sheet;

    public bool IsSheetOpen => Sheet is not null;

    public override async Task InitializeAsync()
    {
        if (_recipe is not null) return;

        _recipe = await dishes.GetRecipeAsync(DishId);
        if (_recipe is null) return;

        var dish = _recipe.Dish;
        var name = preferences.Resolve(dish.LocalName, dish.EnglishName);

        Title = name.Title;
        Subtitle = _recipe.Subtitle;
        IsVerified = _recipe.IsVerified && preferences.ShowVerificationBadge;
        PrepTime = dish.PrepTime ?? "—";
        CookTime = dish.CookTime ?? "—";
        Difficulty = dish.Difficulty;
        Region = dish.Region;
        HeroAsset = dish.ImageAsset;
        HeroCaption = dish.PhotoNeededCaption;
        IsSaved = favourites.IsSaved(dish.Id);
        ContributorName = $"{_recipe.Contributor.Name}, {_recipe.Contributor.Location}";
        ContributorAvatar = _recipe.Contributor.AvatarAsset ?? "";

        foreach (var paragraph in _recipe.CulturalContext)
            CulturalContext.Add(paragraph);

        foreach (var ingredient in _recipe.Ingredients)
            Ingredients.Add(new RecipeIngredientItemViewModel(ingredient, OpenSheet));

        IngredientCountLabel = $"{Ingredients.Count} items";

        foreach (var step in _recipe.Steps)
            Steps.Add(new CookingStepItemViewModel(step, dish.Id, progress, RefreshStepProgress));

        foreach (var variation in _recipe.Variations)
            Variations.Add(variation);

        RefreshStepProgress();
        ApplyMethod();
    }

    private void RefreshStepProgress()
    {
        var done = progress.CompletedCount(DishId, Steps.Select(s => s.Number));
        StepProgressLabel = $"{done} of {Steps.Count} steps done";
    }

    private void ApplyMethod()
    {
        if (_recipe is null) return;

        var method = IsTraditional ? _recipe.TraditionalMethod : _recipe.ModernMethod;
        MethodHeading = method.Heading;

        MethodParagraphs.Clear();
        foreach (var paragraph in method.Paragraphs)
            MethodParagraphs.Add(paragraph);
    }

    private async void OpenSheet(string ingredientKey)
    {
        var ingredient = await ingredients.GetByKeyAsync(ingredientKey);
        if (ingredient is null) return;

        var rows = new List<UsedInRow>();
        foreach (var id in ingredient.UsedInDishIds)
        {
            if (await dishes.GetByIdAsync(id) is { } dish)
                rows.Add(new UsedInRow(dish.LocalName, dish.EnglishName));
        }

        Sheet = new IngredientSheetViewModel(ingredient, rows);
    }

    [RelayCommand]
    private void ToggleSave()
    {
        favourites.Toggle(DishId);
        IsSaved = favourites.IsSaved(DishId);
    }

    [RelayCommand] private void ShowTraditional() { IsTraditional = true; ApplyMethod(); }
    [RelayCommand] private void ShowModern() { IsTraditional = false; ApplyMethod(); }
    [RelayCommand] private void CloseSheet() => Sheet = null;

    [RelayCommand]
    private async Task OpenFullIngredient()
    {
        var key = Sheet?.Key;
        Sheet = null;

        if (key is not null)
            await Navigation.GoToAsync("ingredient", new Dictionary<string, object> { ["key"] = key });
    }

    [RelayCommand]
    private Task Back() => Navigation.GoBackAsync();
}
