using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Collections;

public partial class CookedView : ContentView
{
    private readonly CookedViewModel _viewModel;
    private bool _loaded;

    public CookedView(CookedViewModel viewModel)
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
