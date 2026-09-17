using System.ComponentModel.DataAnnotations;

namespace TasteZambia.Shared.Contracts.Me;

public sealed record ProfileDto(string DisplayName, string Location, string Languages, string? AvatarAsset);

public sealed record UpdateProfileRequest(
    [MaxLength(120)] string DisplayName,
    [MaxLength(120)] string Location,
    [MaxLength(200)] string Languages);

public sealed record OnboardingChoicesDto(
    string Language,
    string Who,
    IReadOnlyList<string> Tastes,
    bool OfflineEnabled,
    bool StoryNotifications,
    bool IsComplete);
