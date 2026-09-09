namespace TasteZambia.Core.Services;

public interface IFavouritesService
{
    bool IsSaved(string dishId);
    void Toggle(string dishId);
    event EventHandler<string>? Changed;
}

public sealed class FavouritesService : IFavouritesService
{
    // Matches the design's initial state: ifisashi saved, chikanda not.
    private readonly HashSet<string> _saved = ["ifisashi"];

    public event EventHandler<string>? Changed;

    public bool IsSaved(string dishId) => _saved.Contains(dishId);

    public void Toggle(string dishId)
    {
        if (!_saved.Remove(dishId))
            _saved.Add(dishId);

        Changed?.Invoke(this, dishId);
    }
}
