namespace TasteZambia.Core.Models;

public sealed record ReadingLanguage(string Name, string Note);
public sealed record VisitorKind(string Key, string Note);
public sealed record TasteOption(string Key, string Label);

public sealed class OnboardingChoices
{
    public string Language { get; set; } = "English";
    public string Who { get; set; } = "I grew up here";
    public HashSet<string> Tastes { get; set; } = ["traditional", "veg"];
    public bool OfflineEnabled { get; set; } = true;
    public bool StoryNotifications { get; set; }
}
