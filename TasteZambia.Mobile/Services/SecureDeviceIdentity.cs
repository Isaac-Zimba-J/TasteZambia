using System.Security.Cryptography;
using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

public sealed class SecureStore : ISecureStore
{
    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);
    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);
    public void Remove(string key) => SecureStorage.Default.Remove(key);
}

public sealed class SecureDeviceIdentity(ISecureStore secure) : IDeviceIdentity
{
    private const string IdKey = "device.id";
    private const string SecretKey = "device.secret";

    public async Task<DeviceCredentials> GetOrCreateAsync(CancellationToken ct = default)
    {
        var id = await secure.GetAsync(IdKey);
        var secret = await secure.GetAsync(SecretKey);
        if (id is not null && secret is not null)
            return new DeviceCredentials(id, secret);

        // Lowercase hex fits the server's [a-z0-9-] rule; 48 random bytes is well past the 32-char minimum.
        id = "device-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(12));
        secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await secure.SetAsync(IdKey, id);
        await secure.SetAsync(SecretKey, secret);
        return new DeviceCredentials(id, secret);
    }
}
