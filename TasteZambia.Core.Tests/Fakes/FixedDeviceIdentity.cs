using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Fakes;

/// <summary>The same account every time, so a test can assert what Settings shows.</summary>
public sealed class FixedDeviceIdentity(string id = "device-0123456789abcdef") : IDeviceIdentity
{
    public Task<DeviceCredentials> GetOrCreateAsync(CancellationToken ct = default)
        => Task.FromResult(new DeviceCredentials(id, "secret-not-used-here-0000000000000000"));
}
