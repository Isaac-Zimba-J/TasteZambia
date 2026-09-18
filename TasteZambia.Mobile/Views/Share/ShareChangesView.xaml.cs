using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Share;

public partial class ShareChangesView : LoadOnceView
{
    private readonly ShareChangesViewModel _viewModel;

    public ShareChangesView(ShareChangesViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
    }

    // Answer is a plain property on the flagged field; the ViewModel re-evaluates
    // whether every question has a reply each time one changes.
    private void OnAnswerChanged(object? sender, TextChangedEventArgs e) => _viewModel.AnswersChanged();
}
