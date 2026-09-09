using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Sections;

public partial class RegionsView : ContentView
{
    private readonly RegionsViewModel _viewModel;
    private bool _loaded;

    public RegionsView(RegionsViewModel viewModel)
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
