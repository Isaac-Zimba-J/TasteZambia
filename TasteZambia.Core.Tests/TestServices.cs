using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests;

/// <summary>Real services over throwaway local state and a client that can never reach a server.</summary>
public static class TestServices
{
    public static HttpClient NoNetwork() => new() { BaseAddress = new Uri("http://localhost:1"), Timeout = TimeSpan.FromMilliseconds(200) };

    public static OnboardingService Onboarding() => new(new InMemoryLocalStore(), NoNetwork());
}
