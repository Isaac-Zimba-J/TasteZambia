namespace TasteZambia.Core.Services;

/// <summary>
/// Keeps MAUI Shell out of the ViewModels so they stay unit-testable.
/// Implemented in TasteZambia.Mobile by ShellNavigationService.
/// </summary>
public interface INavigationService
{
    Task GoToAsync(string route);
    Task GoToAsync(string route, IDictionary<string, object> parameters);
    Task GoBackAsync();
}
