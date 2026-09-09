using TasteZambia.Mobile.Services;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var host = _services.GetRequiredService<MainShellPage>();

        // The navigation service drives the host directly; there is no Shell.
        _services.GetRequiredService<AppNavigationService>().Attach(host);

        return new Window(host);
    }
}
