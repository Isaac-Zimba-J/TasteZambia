using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Share;

public partial class ShareChangesView : LoadOnceView
{
    public ShareChangesView(ShareChangesViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
