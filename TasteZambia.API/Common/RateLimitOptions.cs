namespace TasteZambia.API.Common;

/// <summary>Policy names, so a controller cannot misspell one and silently go unlimited.</summary>
public static class RateLimitPolicies
{
    /// <summary>Registering or signing in a device. The expensive, mintable one.</summary>
    public const string DeviceAuth = "auth-device";

    /// <summary>Exchanging a refresh token. Cheap, but a valid token should not be a firehose.</summary>
    public const string Refresh = "auth-refresh";

    /// <summary>Anything that writes to the archive, partitioned per account rather than per address.</summary>
    public const string Writes = "writes";
}

/// <summary>
/// How much of each thing one caller may do. Deliberately configurable: the right numbers
/// depend on who is using the archive and from where, and a limit that locks out a real
/// contributor is a worse failure than one that is slightly generous to an attacker.
///
/// Zambian mobile networks put many real readers behind one carrier address, so the
/// per-address limits allow for a whole village sharing it, not one phone.
/// </summary>
public sealed class RateLimitOptions
{
    public const string Section = "RateLimits";

    /// <summary>The window every limit below is counted over.</summary>
    public int WindowMinutes { get; set; } = 15;

    /// <summary>
    /// POST /auth/device per address. A phone calls this once per install and then refreshes,
    /// so this is far above ordinary use and still ends unlimited account minting.
    /// </summary>
    public int DeviceAuthPerWindow { get; set; } = 20;

    public int RefreshPerWindow { get; set; } = 90;

    /// <summary>Writes per signed-in account. Enough for a long evening of contributing.</summary>
    public int WritesPerWindow { get; set; } = 150;

    /// <summary>Everything else per address, as a backstop against a flood of plain reads.</summary>
    public int GlobalPerWindow { get; set; } = 900;

    public TimeSpan Window => TimeSpan.FromMinutes(Math.Max(1, WindowMinutes));
}
