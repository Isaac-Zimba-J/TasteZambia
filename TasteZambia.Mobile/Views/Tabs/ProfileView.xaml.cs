using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Tabs;

public partial class ProfileView : LoadOnceView
{
    public ProfileView(ProfileViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
