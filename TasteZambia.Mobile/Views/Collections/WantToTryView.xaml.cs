using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Collections;

public partial class WantToTryView : LoadOnceView
{
    public WantToTryView(WantToTryViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
