using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Details;

public partial class RecipeView : LoadOnceView
{
    private readonly RecipeViewModel _viewModel;

    public RecipeView(RecipeViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        _viewModel.PropertyChanged += async (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(RecipeViewModel.IsSheetOpen) when _viewModel.IsSheetOpen:
                    await SlideSheetInAsync();
                    break;

                // Ticking a step changes state the sighted user sees in the counter;
                // a screen-reader user only hears it if we say it.
                case nameof(RecipeViewModel.StepProgressLabel):
                    SemanticScreenReader.Announce(_viewModel.StepProgressLabel);
                    break;
            }
        };
    }

    /// <summary>TranslateToAsync, not TranslateTo - the sync-named forms are gone in .NET 10.</summary>
    private async Task SlideSheetInAsync()
    {
        Sheet.TranslationY = 420;
        await Sheet.TranslateToAsync(0, 0, 260, Easing.CubicOut);
    }
}
