using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Onboarding;

public partial class OnbTasteView : ContentView
{
    public OnbTasteView(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
