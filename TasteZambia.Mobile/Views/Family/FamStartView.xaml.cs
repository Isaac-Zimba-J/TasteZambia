using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Family;

public partial class FamStartView : LoadOnceView
{
    public FamStartView(FamStartViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
