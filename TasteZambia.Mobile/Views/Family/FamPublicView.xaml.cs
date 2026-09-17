using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Family;

public partial class FamPublicView : LoadOnceView
{
    public FamPublicView(FamPublicViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
