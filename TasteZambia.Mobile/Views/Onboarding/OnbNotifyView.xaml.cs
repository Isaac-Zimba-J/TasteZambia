using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Onboarding;

public partial class OnbNotifyView : ContentView
{
    public OnbNotifyView(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
