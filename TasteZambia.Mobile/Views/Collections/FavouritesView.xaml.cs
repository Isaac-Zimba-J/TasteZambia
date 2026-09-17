using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Collections;

public partial class FavouritesView : LoadOnceView
{
    public FavouritesView(FavouritesViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
