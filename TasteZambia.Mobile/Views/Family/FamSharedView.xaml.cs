using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Family;

public partial class FamSharedView : LoadOnceView
{
    public FamSharedView(FamSharedViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
