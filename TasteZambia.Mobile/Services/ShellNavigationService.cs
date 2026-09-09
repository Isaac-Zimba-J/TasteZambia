using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

public sealed class ShellNavigationService : INavigationService
{
    /// <summary>
    /// Absolute routes ("//home") are tab switches and must not animate - the
    /// cross-fade reads as a flash. Relative routes are pushes onto the current
    /// tab's stack and keep their slide, which is the expected affordance.
    /// </summary>
    private static bool ShouldAnimate(string route) => !route.StartsWith("//");

    public Task GoToAsync(string route)
        => Shell.Current.GoToAsync(route, ShouldAnimate(route));

    public Task GoToAsync(string route, IDictionary<string, object> parameters)
        => Shell.Current.GoToAsync(route, ShouldAnimate(route), parameters);

    public Task GoBackAsync()
        => Shell.Current.GoToAsync("..", animate: true);
}
