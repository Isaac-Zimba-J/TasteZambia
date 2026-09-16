using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Family;

public partial class FamDraftView : ContentView
{
    private readonly FamDraftViewModel _viewModel;
    private bool _loaded;

    public FamDraftView(FamDraftViewModel viewModel)
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
