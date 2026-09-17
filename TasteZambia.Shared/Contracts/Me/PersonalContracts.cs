namespace TasteZambia.Shared.Contracts.Me;

/// <summary>One local change, stamped with the client's clock at the moment of the tap.</summary>
public sealed record SyncChangeDto(string Kind, string DishId, int? Step, bool Value, DateTimeOffset At)
{
    public const string Saved = "saved";
    public const string Progress = "progress";
}

public sealed record SyncRequest(IReadOnlyList<SyncChangeDto> Changes);

public sealed record SavedDishDto(string DishId, bool IsSaved, DateTimeOffset UpdatedAt);
public sealed record CookProgressDto(string DishId, int StepNumber, bool IsDone, DateTimeOffset UpdatedAt);

/// <summary>The account's full current state after applying the request. The client replaces its local copy with this.</summary>
public sealed record SyncResponse(IReadOnlyList<SavedDishDto> Saved, IReadOnlyList<CookProgressDto> Progress, DateTimeOffset ServerTime);
