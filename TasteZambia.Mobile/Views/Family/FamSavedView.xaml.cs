using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Family;

public partial class FamSavedView : ContentView
{
    private readonly FamSavedViewModel _viewModel;
    private bool _loaded;

    public FamSavedView(FamSavedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await _viewModel.InitializeAsync();
    }
}
