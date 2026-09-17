namespace TasteZambia.Core.Services;

public interface IFavouritesService
{
    bool IsSaved(string dishId);
    void Toggle(string dishId);
    event EventHandler<string>? Changed;
}

/// <summary>A thin face over <see cref="PersonalStore"/>: every toggle is local first, synced later.</summary>
public sealed class FavouritesService(PersonalStore store) : IFavouritesService
{
    public event EventHandler<string>? Changed
    {
        add => store.Changed += value;
        remove => store.Changed -= value;
    }

    public bool IsSaved(string dishId) => store.IsSaved(dishId);
    public void Toggle(string dishId) => store.SetSaved(dishId, !store.IsSaved(dishId));
}
