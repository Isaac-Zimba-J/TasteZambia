namespace TasteZambia.Core.Services;

public interface ICookingProgressService
{
    bool IsDone(string dishId, int stepNumber);
    void Toggle(string dishId, int stepNumber);
    int CompletedCount(string dishId, IEnumerable<int> stepNumbers);
    event EventHandler<string>? Changed;
}

/// <summary>A thin face over <see cref="PersonalStore"/>: every tick is local first, synced later.</summary>
public sealed class CookingProgressService(PersonalStore store) : ICookingProgressService
{
    public event EventHandler<string>? Changed
    {
        add => store.Changed += value;
        remove => store.Changed -= value;
    }

    public bool IsDone(string dishId, int stepNumber) => store.IsDone(dishId, stepNumber);
    public void Toggle(string dishId, int stepNumber) => store.SetDone(dishId, stepNumber, !store.IsDone(dishId, stepNumber));
    public int CompletedCount(string dishId, IEnumerable<int> stepNumbers) => stepNumbers.Count(s => store.IsDone(dishId, s));
}
