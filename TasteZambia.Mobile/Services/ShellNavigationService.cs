using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route)
        => Shell.Current.GoToAsync(route);

    public Task GoToAsync(string route, IDictionary<string, object> parameters)
        => Shell.Current.GoToAsync(route, parameters);

    public Task GoBackAsync()
        => Shell.Current.GoToAsync("..");
}
