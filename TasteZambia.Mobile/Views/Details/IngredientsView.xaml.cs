using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Details;

public partial class IngredientsView : LoadOnceView
{
    public IngredientsView(IngredientsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
