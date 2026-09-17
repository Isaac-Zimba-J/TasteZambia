using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Share;

public partial class ShareDraftsView : LoadOnceView
{
    public ShareDraftsView(DraftsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
