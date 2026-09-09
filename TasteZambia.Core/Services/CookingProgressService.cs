namespace TasteZambia.Core.Services;

public interface ICookingProgressService
{
    bool IsDone(string dishId, int stepNumber);
    void Toggle(string dishId, int stepNumber);
    int CompletedCount(string dishId, IEnumerable<int> stepNumbers);
    event EventHandler<string>? Changed;
}

public sealed class CookingProgressService : ICookingProgressService
{
    private readonly Dictionary<string, HashSet<int>> _done = [];

    public event EventHandler<string>? Changed;

    public bool IsDone(string dishId, int stepNumber)
        => _done.TryGetValue(dishId, out var steps) && steps.Contains(stepNumber);

    public void Toggle(string dishId, int stepNumber)
    {
        if (!_done.TryGetValue(dishId, out var steps))
            _done[dishId] = steps = [];

        if (!steps.Remove(stepNumber))
            steps.Add(stepNumber);

        Changed?.Invoke(this, dishId);
    }

    public int CompletedCount(string dishId, IEnumerable<int> stepNumbers)
        => _done.TryGetValue(dishId, out var steps)
            ? stepNumbers.Count(steps.Contains)
            : 0;
}
