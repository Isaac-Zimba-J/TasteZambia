namespace TasteZambia.Core.Services;

public sealed record DeviceCredentials(string Id, string Secret);

/// <summary>
/// The anonymous account's credentials, generated once per install and kept in secure
/// storage. The same pair signs in the same account for as long as the app is installed.
/// </summary>
public interface IDeviceIdentity
{
    Task<DeviceCredentials> GetOrCreateAsync(CancellationToken ct = default);
}

/// <summary>Secure key-value storage. Implemented over SecureStorage on device.</summary>
public interface ISecureStore
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    void Remove(string key);
}
