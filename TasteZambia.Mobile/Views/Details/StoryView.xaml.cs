using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Details;

public partial class StoryView : LoadOnceView
{
    public StoryView(StoryViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
