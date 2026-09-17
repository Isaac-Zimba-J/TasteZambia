using System.Text.Json;
using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

public sealed class PreferencesLocalStore : ILocalStore
{
    private const string Prefix = "tz.";

    public T? Get<T>(string key)
    {
        var json = Preferences.Default.Get<string?>(Prefix + key, null);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public void Set<T>(string key, T value) => Preferences.Default.Set(Prefix + key, JsonSerializer.Serialize(value));
    public void Remove(string key) => Preferences.Default.Remove(Prefix + key);
}
