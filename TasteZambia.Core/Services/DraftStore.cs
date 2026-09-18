using TasteZambia.Core.Models;

namespace TasteZambia.Core.Services;

/// <summary>The phone's drafts. JSON in the local store; nothing here talks to the network.</summary>
public sealed class DraftStore
{
    private const string Key = "drafts";
    private readonly ILocalStore _local;
    private readonly TimeProvider _clock;
    private readonly List<LocalDraft> _drafts;

    public event EventHandler? Changed;

    public DraftStore(ILocalStore local, TimeProvider clock)
    {
        _local = local;
        _clock = clock;
        _drafts = local.Get<List<LocalDraft>>(Key) ?? [];
    }

    public IReadOnlyList<LocalDraft> All => _drafts.OrderByDescending(d => d.EditedAt).ToList();

    public LocalDraft? Find(Guid id) => _drafts.FirstOrDefault(d => d.Id == id);

    public LocalDraft Create(ContributionDraft draft)
    {
        var local = new LocalDraft { Id = Guid.NewGuid(), EditedAt = _clock.GetUtcNow(), Draft = draft };
        _drafts.Add(local);
        Persist();
        return local;
    }

    public void Save(LocalDraft draft)
    {
        var index = _drafts.FindIndex(d => d.Id == draft.Id);
        draft.EditedAt = _clock.GetUtcNow();
        if (index < 0) _drafts.Add(draft);
        else _drafts[index] = draft;
        Persist();
    }

    public void Remove(Guid id)
    {
        if (_drafts.RemoveAll(d => d.Id == id) > 0) Persist();
    }

    private void Persist()
    {
        _local.Set(Key, _drafts);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
