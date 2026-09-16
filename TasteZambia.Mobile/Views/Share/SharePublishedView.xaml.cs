using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Share;

public partial class SharePublishedView : ContentView
{
    private readonly SharePublishedViewModel _viewModel;
    private bool _loaded;

    public SharePublishedView(SharePublishedViewModel viewModel)
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
