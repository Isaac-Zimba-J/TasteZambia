using TasteZambia.Core.Services;
using TasteZambia.Mobile.Services;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private Window? _window;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var navigation = _services.GetRequiredService<AppNavigationService>();
        var onboarding = _services.GetRequiredService<IOnboardingService>();

        navigation.OnboardingCompleted = ShowMainShell;

        if (onboarding.IsComplete)
        {
            _window = new Window(BuildMainShell(navigation));
            return _window;
        }

        var host = _services.GetRequiredService<OnboardingPage>();
        navigation.AttachOnboarding(host);

        _window = new Window(host);
        return _window;
    }

    private MainShellPage BuildMainShell(AppNavigationService navigation)
    {
        var shell = _services.GetRequiredService<MainShellPage>();
        navigation.Attach(shell);
        return shell;
    }

    private void ShowMainShell()
    {
        var navigation = _services.GetRequiredService<AppNavigationService>();
        navigation.DetachOnboarding();

        if (_window is not null)
            _window.Page = BuildMainShell(navigation);
    }
}
