namespace TasteZambia.API.Data.Entities;

public class UserProfile
{
    public required string UserId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "";
    public string Languages { get; set; } = "";
    public string? AvatarAsset { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class OnboardingChoices
{
    public required string UserId { get; set; }
    public string Language { get; set; } = "English";
    public string Who { get; set; } = "I grew up here";
    /// <summary>Comma-joined taste keys, e.g. "traditional,veg".</summary>
    public string Tastes { get; set; } = "traditional,veg";
    public bool OfflineEnabled { get; set; } = true;
    public bool StoryNotifications { get; set; }
    public bool IsComplete { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Soft flag, never deleted: "unsave" is IsSaved=false with a newer UpdatedAt.</summary>
public class SavedDish
{
    public required string UserId { get; set; }
    public required string DishId { get; set; }
    public bool IsSaved { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class CookProgress
{
    public required string UserId { get; set; }
    public required string DishId { get; set; }
    public int StepNumber { get; set; }
    public bool IsDone { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
