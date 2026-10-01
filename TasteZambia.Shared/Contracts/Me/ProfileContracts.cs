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

/// <summary>
/// Deleting an account cannot be undone, so it takes the account's own device id as
/// confirmation. A stray DELETE - a retried request, a mistapped screen - does not
/// carry it, and is refused.
/// </summary>
public sealed record DeleteAccountRequest(
    [System.ComponentModel.DataAnnotations.Required] string ConfirmDeviceId);

/// <summary>
/// What the deletion took away. The reader is told rather than just obeyed - in particular
/// that a published recipe stayed in the archive with the credit withdrawn, which is not
/// what "delete everything" sounds like it would do.
/// </summary>
public sealed record DeleteAccountResultDto(
    int DraftsDeleted,
    int PublishedRecipesAnonymised,
    int FamilyRecipesDeleted,
    int FamilyRecipesLeft,
    int NotesAnonymised,
    int FilesDeleted);
