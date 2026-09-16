namespace TasteZambia.Core.Models;

public sealed record SavedEntry(string DishId, string When, string Note);
public sealed record WishlistEntry(string DishId, string Why);
public sealed record CookedEntry(string DishId, string Times, string Last, string Note);
public sealed record LanguageStatus(string Name, string Note, bool IsCurrent);
public sealed record SettingToggle(string Label, string Note, bool IsOn);
