using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Collections;

public partial class CookedView : LoadOnceView
{
    public CookedView(CookedViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
