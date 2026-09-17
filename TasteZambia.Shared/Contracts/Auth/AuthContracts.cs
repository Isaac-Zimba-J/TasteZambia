using System.ComponentModel.DataAnnotations;

namespace TasteZambia.Shared.Contracts.Auth;

/// <summary>
/// Anonymous, device-bound sign-in. The app generates both values once and keeps them
/// in secure storage; the same pair signs in the same account for the life of the install.
/// </summary>
public sealed record DeviceAuthRequest(
    [Required, RegularExpression("^[a-z0-9-]{16,64}$")] string DeviceId,
    [Required, MinLength(32), MaxLength(256)] string DeviceSecret);

public sealed record RefreshRequest([Required] string RefreshToken);

public sealed record AuthTokensDto(string AccessToken, string RefreshToken, DateTimeOffset AccessExpiresAt);
