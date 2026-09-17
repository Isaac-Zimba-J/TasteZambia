using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Share;

public partial class ShareView : LoadOnceView
{
    public ShareView(ShareViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
