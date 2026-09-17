using System.Text.Json;

namespace TasteZambia.Core.Services;

/// <summary>
/// Small JSON documents by key. Backed by Preferences on device. If it ever needs to
/// hold more than a few hundred rows, the replacement is SQLite behind this same interface.
/// </summary>
public interface ILocalStore
{
    T? Get<T>(string key);
    void Set<T>(string key, T value);
    void Remove(string key);
}

/// <summary>Process-lifetime store for tests and previews. Serialises like the real one so round-trip bugs surface here.</summary>
public sealed class InMemoryLocalStore : ILocalStore
{
    private readonly Dictionary<string, string> _docs = [];

    public T? Get<T>(string key)
        => _docs.TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json) : default;

    public void Set<T>(string key, T value) => _docs[key] = JsonSerializer.Serialize(value);
    public void Remove(string key) => _docs.Remove(key);
}
