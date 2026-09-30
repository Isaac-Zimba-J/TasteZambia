namespace TasteZambia.Core.Services;

/// <summary>
/// A brief confirmation that something the reader did took effect. Used where an action
/// succeeds quietly - a note added, an invite sent, a name saved - and the screen alone
/// does not make it obvious that the archive heard them.
///
/// Failures do not go here. A message that disappears on its own is no place for something
/// the reader has to act on; those stay on the screen until they are resolved.
/// </summary>
public interface IToastService
{
    void Show(string message);
}

/// <summary>Records what would have been shown. The host replaces it with a real one.</summary>
public sealed class NullToastService : IToastService
{
    public void Show(string message) { }
}
