using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Collections;

public partial class DeleteAccountView : LoadOnceView
{
    public DeleteAccountView(DeleteAccountViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
