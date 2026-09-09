using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route)
        => Shell.Current.GoToAsync(route, animate: !route.StartsWith("//"));

    public Task GoToAsync(string route, IDictionary<string, object> parameters)
        => Shell.Current.GoToAsync(route, animate: !route.StartsWith("//"), parameters);

    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
