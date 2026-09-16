using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Share;

public partial class ShareReviewView : ContentView
{
    private readonly ShareReviewViewModel _viewModel;
    private bool _loaded;

    public ShareReviewView(ShareReviewViewModel viewModel)
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
