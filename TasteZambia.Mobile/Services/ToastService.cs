using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

/// <summary>
/// Raises what the ViewModels ask to confirm. The shell page draws it, because the screen
/// that caused the confirmation is often the one being navigated away from - a banner owned
/// by that screen would leave with it before it had been read.
/// </summary>
public sealed class ToastService : IToastService
{
    public event Action<string>? Requested;

    public void Show(string message)
    {
        if (message.Length == 0) return;
        Requested?.Invoke(message);
    }
}
