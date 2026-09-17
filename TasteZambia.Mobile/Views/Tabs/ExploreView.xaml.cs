using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Tabs;

public partial class ExploreView : LoadOnceView
{
    public ExploreView(ExploreViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
