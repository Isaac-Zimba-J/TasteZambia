using TasteZambia.Core.ViewModels;

namespace TasteZambia.Mobile.Views.Onboarding;

public partial class OnbLanguageView : ContentView
{
    public OnbLanguageView(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
