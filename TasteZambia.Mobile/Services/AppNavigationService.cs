using TasteZambia.Core.Services;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Services;

/// <summary>
/// Drives the single host page instead of Shell. Section changes are a content
/// swap inside one page, so they carry no page transition of any kind.
/// </summary>
public sealed class AppNavigationService : INavigationService
{
    private MainShellPage? _host;

    public void Attach(MainShellPage host) => _host = host;

    public Task GoToAsync(string route)
    {
        _host?.Navigate(route, null);
        return Task.CompletedTask;
    }

    public Task GoToAsync(string route, IDictionary<string, object> parameters)
    {
        _host?.Navigate(route, parameters);
        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        _host?.GoBack();
        return Task.CompletedTask;
    }
}
