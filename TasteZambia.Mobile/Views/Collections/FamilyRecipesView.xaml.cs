using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Collections;

public partial class FamilyRecipesView : LoadOnceView
{
    public FamilyRecipesView(FamilyRecipesViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
