using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Services;

/// <summary>
/// Holds the current tokens. The refresh token lives in secure storage; the access
/// token is memory-only. Uses a plain HttpClient with NO AuthenticatedHandler - auth
/// calls must never recurse into the handler that depends on them.
/// </summary>
public sealed class AuthSession(HttpClient authClient, IDeviceIdentity device, ISecureStore secure, ILogger<AuthSession> log) : IAuthSession
{
    private const string RefreshKey = "auth.refresh";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset _accessExpires;

    public string? AccessToken { get; private set; }

    private bool HasUsableToken => AccessToken is not null && _accessExpires > DateTimeOffset.UtcNow.AddMinutes(1);

    public async Task EnsureSignedInAsync(CancellationToken ct = default)
    {
        if (HasUsableToken) return;

        await _gate.WaitAsync(ct);
        try
        {
            if (HasUsableToken) return;

            // Prefer a refresh: it does not need the device secret over the wire.
            if (await secure.GetAsync(RefreshKey) is { } refresh && await TryRefreshAsync(refresh, ct))
                return;

            await SignInAsync(ct);
        }
        catch (Exception ex)
        {
            // Offline, or secure storage hiccuped (Android's keystore can throw on first
            // access). Either way the request goes out without a token and the caller
            // sees the 401; the local store keeps the app usable until the next attempt.
            Report("Sign-in skipped", ex);
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (await secure.GetAsync(RefreshKey) is { } refresh && await TryRefreshAsync(refresh, ct))
                return true;

            // Refresh rejected (rotated elsewhere, expired). Fall back to a full sign-in.
            AccessToken = null;
            await SignInAsync(ct);
            return AccessToken is not null;
        }
        catch (Exception ex)
        {
            Report("Refresh failed", ex);
            return false;
        }
        finally { _gate.Release(); }
    }

    private void Report(string what, Exception ex)
    {
        if (ex is HttpRequestException or TaskCanceledException)
            log.LogDebug("{What}, offline: {Message}", what, ex.Message);
        else
            log.LogError(ex, "{What}", what);
    }

    private async Task SignInAsync(CancellationToken ct)
    {
        var creds = await device.GetOrCreateAsync(ct);
        var response = await authClient.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest(creds.Id, creds.Secret), ct);
        if (!response.IsSuccessStatusCode) return;
        await AdoptAsync((await response.Content.ReadFromJsonAsync<AuthTokensDto>(ct))!);
    }

    private async Task<bool> TryRefreshAsync(string refresh, CancellationToken ct)
    {
        var response = await authClient.PostAsJsonAsync(ApiRoutes.Auth.Refresh, new RefreshRequest(refresh), ct);
        if (!response.IsSuccessStatusCode) return false;
        await AdoptAsync((await response.Content.ReadFromJsonAsync<AuthTokensDto>(ct))!);
        return true;
    }

    private async Task AdoptAsync(AuthTokensDto tokens)
    {
        AccessToken = tokens.AccessToken;
        _accessExpires = tokens.AccessExpiresAt;
        await secure.SetAsync(RefreshKey, tokens.RefreshToken);
    }
}
