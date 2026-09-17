using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Share;

public partial class ShareStartView : LoadOnceView
{
    public ShareStartView(ShareStartViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
