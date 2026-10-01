using System.Net;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Services;

public interface IAccountService
{
    /// <summary>The account's own identifier. What Settings shows and what deletion confirms with.</summary>
    Task<string> DeviceIdAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes the account in the archive and then everything this phone kept. Returns what the
    /// archive took away, so the reader can be told rather than just obeyed.
    /// </summary>
    Task<DeleteAccountResultDto> DeleteAsync(CancellationToken ct = default);
}

/// <summary>
/// Deleting an account is the one action with nothing behind it, so the order matters: the
/// archive goes first, and this phone is only wiped once the archive has confirmed. Wiping
/// first would leave a reader signed out of an account that still exists, holding the only
/// copy of the secret that could have reached it.
/// </summary>
public sealed class AccountService(
    HttpClient client,
    IDeviceIdentity identity,
    ISecureStore secure,
    ILocalStore local) : IAccountService
{
    /// <summary>The keys this phone keeps. Every one of them is the deleted account's.</summary>
    private static readonly string[] LocalKeys = ["personal", "drafts", "onboarding"];
    private static readonly string[] SecureKeys = ["device.id", "device.secret"];

    public async Task<string> DeviceIdAsync(CancellationToken ct = default)
        => (await identity.GetOrCreateAsync(ct)).Id;

    public async Task<DeleteAccountResultDto> DeleteAsync(CancellationToken ct = default)
    {
        var deviceId = await DeviceIdAsync(ct);

        var request = new HttpRequestMessage(HttpMethod.Delete, ApiRoutes.Me.Account)
        {
            Content = JsonContent.Create(new DeleteAccountRequest(deviceId)),
        };

        var response = await client.SendAsync(request, ct);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
            // The account is already gone - a retry after a lost reply, most likely. Clearing
            // the phone is still the right end state, so this is not an error to report.
            return await ForgetEverythingAsync(new DeleteAccountResultDto(0, 0, 0, 0, 0, 0));

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DeleteAccountResultDto>(ct)
                     ?? new DeleteAccountResultDto(0, 0, 0, 0, 0, 0);

        return await ForgetEverythingAsync(result);
    }

    private Task<DeleteAccountResultDto> ForgetEverythingAsync(DeleteAccountResultDto result)
    {
        foreach (var key in LocalKeys) local.Remove(key);

        // Last: without the secret there is no way back into an account, so this is the point
        // of no return. The next launch generates a new one and starts at onboarding.
        foreach (var key in SecureKeys) secure.Remove(key);

        return Task.FromResult(result);
    }
}
