using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Share;

public partial class SharePublishedView : LoadOnceView
{
    public SharePublishedView(SharePublishedViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
