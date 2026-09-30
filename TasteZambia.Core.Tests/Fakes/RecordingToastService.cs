using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Fakes;

/// <summary>Keeps every confirmation, so a test can assert what the reader was told.</summary>
public sealed class RecordingToastService : IToastService
{
    public List<string> Shown { get; } = [];
    public string Last => Shown.Count == 0 ? "" : Shown[^1];

    public void Show(string message) => Shown.Add(message);
}
