using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Share;

public partial class ShareStartView : ContentView
{
    private readonly ShareStartViewModel _viewModel;
    private bool _loaded;

    public ShareStartView(ShareStartViewModel viewModel)
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
