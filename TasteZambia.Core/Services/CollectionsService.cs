using TasteZambia.Core.Data;
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Services;

public interface ICollectionsService
{
    IReadOnlyList<SavedEntry> Saved { get; }
    IReadOnlyList<WishlistEntry> Wishlist { get; }
    IReadOnlyList<CookedEntry> Cooked { get; }
    IReadOnlyList<LanguageStatus> Languages { get; }
    IReadOnlyList<SettingToggle> Toggles { get; }
}

public sealed class CollectionsService : ICollectionsService
{
    public IReadOnlyList<SavedEntry> Saved => SeedData.Saved;
    public IReadOnlyList<WishlistEntry> Wishlist => SeedData.Wishlist;
    public IReadOnlyList<CookedEntry> Cooked => SeedData.Cooked;
    public IReadOnlyList<LanguageStatus> Languages => SeedData.LanguageStatuses;
    public IReadOnlyList<SettingToggle> Toggles => SeedData.SettingToggles;
}
