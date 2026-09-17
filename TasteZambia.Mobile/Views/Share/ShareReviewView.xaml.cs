using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Share;

public partial class ShareReviewView : LoadOnceView
{
    public ShareReviewView(ShareReviewViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
