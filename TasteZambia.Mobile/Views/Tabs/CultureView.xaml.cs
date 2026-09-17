using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Tabs;

public partial class CultureView : LoadOnceView
{
    public CultureView(CultureViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
