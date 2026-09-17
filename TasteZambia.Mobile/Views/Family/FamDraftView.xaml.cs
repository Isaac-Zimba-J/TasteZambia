using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Family;

public partial class FamDraftView : LoadOnceView
{
    public FamDraftView(FamDraftViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
