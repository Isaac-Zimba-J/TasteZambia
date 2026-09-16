using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Share;

public partial class ShareDraftsView : ContentView
{
    private readonly DraftsViewModel _viewModel;
    private bool _loaded;

    public ShareDraftsView(DraftsViewModel viewModel)
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
