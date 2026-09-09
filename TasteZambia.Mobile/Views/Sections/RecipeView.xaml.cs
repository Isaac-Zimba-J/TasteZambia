using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Sections;

public partial class RecipeView : ContentView
{
    private readonly RecipeViewModel _viewModel;
    private bool _loaded;

    public RecipeView(RecipeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        Loaded += OnLoaded;

        _viewModel.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(RecipeViewModel.IsSheetOpen) && _viewModel.IsSheetOpen)
                await SlideSheetInAsync();
        };
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await _viewModel.InitializeAsync();
    }

    /// <summary>TranslateToAsync, not TranslateTo - the sync-named forms are gone in .NET 10.</summary>
    private async Task SlideSheetInAsync()
    {
        Sheet.TranslationY = 420;
        await Sheet.TranslateToAsync(0, 0, 260, Easing.CubicOut);
    }
}
