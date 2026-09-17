using System.Reflection;

namespace TasteZambia.Mobile.Services;

/// <summary>
/// Where the app finds the archive API during development. Each platform reaches the
/// developer's machine differently: the Android emulator through 10.0.2.2, the iOS
/// simulator through localhost, and a physical phone through the Mac's LAN address.
///
/// The LAN address is NOT hardcoded - it changes with every Wi-Fi network. The deploy
/// script injects it at build time (`-p:ArchiveApiHost=...`), which lands here as
/// assembly metadata. Production supplies a real host; this is the dev default only.
/// </summary>
public static class ArchiveApiOptions
{
    public const int Port = 5080;

    /// <summary>The dev machine's LAN address, injected at build time; null when not supplied.</summary>
    public static string? InjectedHost { get; } = typeof(ArchiveApiOptions).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "ArchiveApiHost" && !string.IsNullOrWhiteSpace(a.Value))?.Value;

    public static string BaseUrl
    {
        get
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                if (DeviceInfo.DeviceType == DeviceType.Virtual)
                    return $"http://10.0.2.2:{Port}";

                // A physical phone needs the Mac's LAN address. Without one injected there
                // is nothing sensible to fall back to; fail loudly rather than time out
                // silently for 100 seconds.
                return InjectedHost is { } host
                    ? $"http://{host}:{Port}"
                    : throw new InvalidOperationException(
                        "No API host for a physical Android device. Deploy with scripts/android.sh, " +
                        "which injects the Mac's LAN address, or pass -p:ArchiveApiHost=<ip>.");
            }

            return $"http://localhost:{Port}";
        }
    }
}
