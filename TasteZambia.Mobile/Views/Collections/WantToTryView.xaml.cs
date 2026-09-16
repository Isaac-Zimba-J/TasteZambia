using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Collections;

public partial class WantToTryView : ContentView
{
    private readonly WantToTryViewModel _viewModel;
    private bool _loaded;

    public WantToTryView(WantToTryViewModel viewModel)
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
