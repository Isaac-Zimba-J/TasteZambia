using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Tabs;

public partial class HomeView : LoadOnceView
{
    public HomeView(HomeViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
