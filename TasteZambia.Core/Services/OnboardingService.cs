using TasteZambia.Core.Models;

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
/// In-memory for this phase, matching the repository rule: when persistence lands,
/// back IsComplete and Current with Preferences or the API and change nothing else.
/// </summary>
public sealed class OnboardingService : IOnboardingService
{
    public bool IsComplete { get; private set; }
    public OnboardingChoices Current { get; private set; } = new();

    public void Complete(OnboardingChoices choices)
    {
        Current = choices;
        IsComplete = true;
    }

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
