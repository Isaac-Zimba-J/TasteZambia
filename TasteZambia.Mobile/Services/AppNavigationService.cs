using TasteZambia.Core.Services;
using TasteZambia.Mobile.Views;

namespace TasteZambia.Mobile.Services;

/// <summary>
/// Drives whichever host is on screen. Section changes are a content swap inside
/// one page, so they carry no page transition of any kind.
///
/// Onboarding is a separate host because it has no nav bar and runs before the app
/// proper; finishing it swaps the window's root page rather than navigating.
/// </summary>
public sealed class AppNavigationService : INavigationService
{
    private MainShellPage? _main;
    private OnboardingPage? _onboarding;

    /// <summary>Set by App: swaps the window root once onboarding completes.</summary>
    public Action? OnboardingCompleted { get; set; }

    public void Attach(MainShellPage host) => _main = host;
    public void AttachOnboarding(OnboardingPage host) => _onboarding = host;

    /// <summary>Onboarding is finished once its host is detached.</summary>
    public void DetachOnboarding() => _onboarding = null;

    public Task GoToAsync(string route)
    {
        if (_onboarding is not null)
        {
            // Leaving onboarding for the app proper is a root swap, not a navigation.
            if (route.StartsWith("//"))
            {
                OnboardingCompleted?.Invoke();
                return Task.CompletedTask;
            }

            if (_onboarding.Handles(route))
            {
                _onboarding.Navigate(route);
                return Task.CompletedTask;
            }
        }

        _main?.Navigate(route, null);
        return Task.CompletedTask;
    }

    public Task GoToAsync(string route, IDictionary<string, object> parameters)
    {
        _main?.Navigate(route, parameters);
        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        if (_onboarding is not null)
        {
            _onboarding.GoBack();
            return Task.CompletedTask;
        }

        _main?.GoBack();
        return Task.CompletedTask;
    }
}
