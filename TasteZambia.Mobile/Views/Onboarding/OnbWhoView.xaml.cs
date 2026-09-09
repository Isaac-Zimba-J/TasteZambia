using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Onboarding;

public partial class OnbWhoView : ContentView
{
    public OnbWhoView(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
