using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Tabs;

public partial class RegionsView : LoadOnceView
{
    public RegionsView(RegionsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
