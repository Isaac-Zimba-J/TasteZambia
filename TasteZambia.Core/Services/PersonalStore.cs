using TasteZambia.Shared.Contracts.Me;

namespace TasteZambia.Core.Services;

public sealed record SavedFlag(bool IsSaved, DateTimeOffset At);
public sealed record ProgressFlag(bool IsDone, DateTimeOffset At);

public sealed class PersonalState
{
    public Dictionary<string, SavedFlag> Saved { get; set; } = [];
    /// <summary>Keyed "{dishId}:{step}".</summary>
    public Dictionary<string, ProgressFlag> Progress { get; set; } = [];
    public List<SyncChangeDto> Outbox { get; set; } = [];
}

/// <summary>
/// The device's copy of the account's personal data, plus an outbox of changes not yet
/// acknowledged by the server. Every write lands here first and is stamped with the
/// clock at the moment of the tap; that stamp is what the server compares on sync.
/// </summary>
public sealed class PersonalStore
{
    private const string Key = "personal";
    private readonly ILocalStore _local;
    private readonly TimeProvider _clock;
    private readonly object _sync = new();
    private PersonalState _state;

    public event EventHandler<string>? Changed;

    public PersonalStore(ILocalStore local, TimeProvider clock)
    {
        _local = local;
        _clock = clock;
        _state = local.Get<PersonalState>(Key) ?? Seed();
    }

    // The design's starting state: ifisashi saved. Stamped at epoch so any real tap outranks it.
    private static PersonalState Seed() => new()
    {
        Saved = { ["ifisashi"] = new SavedFlag(true, DateTimeOffset.UnixEpoch) },
    };

    private static string StepKey(string dishId, int step) => $"{dishId}:{step}";

    public bool IsSaved(string dishId)
    {
        lock (_sync) return _state.Saved.TryGetValue(dishId, out var e) && e.IsSaved;
    }

    public bool IsDone(string dishId, int step)
    {
        lock (_sync) return _state.Progress.TryGetValue(StepKey(dishId, step), out var e) && e.IsDone;
    }

    public void SetSaved(string dishId, bool isSaved)
    {
        var at = _clock.GetUtcNow();
        lock (_sync)
        {
            _state.Saved[dishId] = new SavedFlag(isSaved, at);
            _state.Outbox.Add(new SyncChangeDto(SyncChangeDto.Saved, dishId, null, isSaved, at));
            _local.Set(Key, _state);
        }
        Changed?.Invoke(this, dishId);
    }

    public void SetDone(string dishId, int step, bool isDone)
    {
        var at = _clock.GetUtcNow();
        lock (_sync)
        {
            _state.Progress[StepKey(dishId, step)] = new ProgressFlag(isDone, at);
            _state.Outbox.Add(new SyncChangeDto(SyncChangeDto.Progress, dishId, step, isDone, at));
            _local.Set(Key, _state);
        }
        Changed?.Invoke(this, dishId);
    }

    /// <summary>Takes everything queued. The caller puts it back with <see cref="Requeue"/> if the send fails.</summary>
    public IReadOnlyList<SyncChangeDto> DrainOutbox()
    {
        lock (_sync)
        {
            var batch = _state.Outbox.ToList();
            _state.Outbox.Clear();
            _local.Set(Key, _state);
            return batch;
        }
    }

    /// <summary>Puts a failed batch back ahead of anything queued since, preserving order.</summary>
    public void Requeue(IReadOnlyList<SyncChangeDto> batch)
    {
        lock (_sync)
        {
            _state.Outbox.InsertRange(0, batch);
            _local.Set(Key, _state);
        }
    }

    /// <summary>
    /// Adopts the server's merged state. Anything the user did AFTER the drain is still
    /// in the outbox and is re-applied on top, so an in-flight tap is never lost.
    /// </summary>
    public void Apply(SyncResponse server)
    {
        lock (_sync)
        {
            var pending = _state.Outbox.ToList();

            _state.Saved = server.Saved.ToDictionary(s => s.DishId, s => new SavedFlag(s.IsSaved, s.UpdatedAt));
            _state.Progress = server.Progress.ToDictionary(p => StepKey(p.DishId, p.StepNumber), p => new ProgressFlag(p.IsDone, p.UpdatedAt));

            foreach (var c in pending)
            {
                if (c.Kind == SyncChangeDto.Saved)
                    _state.Saved[c.DishId] = new SavedFlag(c.Value, c.At);
                else if (c.Kind == SyncChangeDto.Progress && c.Step is { } step)
                    _state.Progress[StepKey(c.DishId, step)] = new ProgressFlag(c.Value, c.At);
            }

            _local.Set(Key, _state);
        }
        Changed?.Invoke(this, "");
    }
}
