using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Family;

public partial class FamilyView : LoadOnceView
{
    public FamilyView(FamilyViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
