using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Family;

public partial class FamSavedView : LoadOnceView
{
    public FamSavedView(FamSavedViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
