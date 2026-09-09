using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Onboarding;

public partial class OnbReadyView : ContentView
{
    public OnbReadyView(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
