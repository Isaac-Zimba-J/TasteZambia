namespace TasteZambia.Core.Services;

public interface IAuthSession
{
    string? AccessToken { get; }

    /// <summary>Signs in with the device credentials if there is no usable token yet.</summary>
    Task EnsureSignedInAsync(CancellationToken ct = default);

    /// <summary>Rotates the refresh token, falling back to a full sign-in. False means the server could not be reached or refused.</summary>
    Task<bool> RefreshAsync(CancellationToken ct = default);
}
