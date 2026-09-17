using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Details;

public partial class IngredientView : LoadOnceView
{
    public IngredientView(IngredientViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
