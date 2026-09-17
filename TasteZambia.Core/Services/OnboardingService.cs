using System.Net.Http.Json;
using TasteZambia.Core.Models;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Services;

public interface IOnboardingService
{
    bool IsComplete { get; }
    OnboardingChoices Current { get; }
    void Complete(OnboardingChoices choices);
    IReadOnlyList<ReadingLanguage> Languages { get; }
    IReadOnlyList<VisitorKind> VisitorKinds { get; }
    IReadOnlyList<TasteOption> Tastes { get; }
}

/// <summary>
/// Local first, always: the choices land in the local store before anything else,
/// so onboarding never repeats and never waits on the network. The account copy
/// (PUT /me/onboarding) is best-effort and never blocks.
/// </summary>
public sealed class OnboardingService : IOnboardingService
{
    private const string Key = "onboarding";
    private readonly ILocalStore _local;
    private readonly HttpClient _api;

    public OnboardingService(ILocalStore local, HttpClient api)
    {
        _local = local;
        _api = api;
        var stored = local.Get<StoredOnboarding>(Key);
        IsComplete = stored?.IsComplete ?? false;
        Current = stored?.Choices ?? new OnboardingChoices();
    }

    public bool IsComplete { get; private set; }
    public OnboardingChoices Current { get; private set; }

    public void Complete(OnboardingChoices choices)
    {
        Current = choices;
        IsComplete = true;
        _local.Set(Key, new StoredOnboarding(true, choices));

        _ = PushAsync(choices);
    }

    private async Task PushAsync(OnboardingChoices c)
    {
        try
        {
            await _api.PutAsJsonAsync(ApiRoutes.Me.Onboarding,
                new OnboardingChoicesDto(c.Language, c.Who, [.. c.Tastes], c.OfflineEnabled, c.StoryNotifications, true));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Offline. The local copy is authoritative for this device.
        }
    }

    private sealed record StoredOnboarding(bool IsComplete, OnboardingChoices Choices);

    public IReadOnlyList<ReadingLanguage> Languages { get; } =
    [
        new("English", "Full interface"),
        new("Bemba", "Icibemba"),
        new("Nyanja", "Chinyanja"),
        new("Tonga", "Chitonga"),
        new("Lozi", "Silozi"),
        new("Kaonde", "Kikaonde"),
    ];

    public IReadOnlyList<VisitorKind> VisitorKinds { get; } =
    [
        new("I grew up here", "Show me dishes from my province first, and the ones I might not know."),
        new("I live abroad", "Show me what I can cook with what is available where I am."),
        new("I am visiting Zambia", "Explain the dishes and what to expect when I eat them."),
        new("I am learning to cook", "Start me on the staples, with more detail in the steps."),
    ];

    public IReadOnlyList<TasteOption> Tastes { get; } =
    [
        new("traditional", "Traditional dishes"),
        new("quick", "Quick weekday meals"),
        new("veg", "Vegetarian relishes"),
        new("family", "Family recipes"),
        new("drinks", "Traditional drinks"),
        new("snacks", "Snacks and street food"),
    ];
}
