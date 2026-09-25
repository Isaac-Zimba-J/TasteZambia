using TasteZambia.Core.ViewModels;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Views.Details;

public partial class ProfileEditView : LoadOnceView
{
    public ProfileEditView(ProfileEditViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
