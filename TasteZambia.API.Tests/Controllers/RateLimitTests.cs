using System.Net;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

/// <summary>
/// POST /auth/device is the one endpoint that creates accounts, and it hashes a password on
/// every call. Unlimited it is both an account mint and a way to spend the server's CPU, so
/// these hold the limit in place - including that it says how long to wait, and that the
/// health check the proxy depends on is never the thing that gets limited.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class RateLimitTests(DatabaseFixture fixture)
{
    private const string Secret = "ssssssssssssssssssssssssssssssssssssssss";

    private ApiFactory Factory(params (string Key, string Value)[] limits)
        => new(fixture.ConnectionString, limits.ToDictionary(l => l.Key, l => (string?)l.Value));

    [Fact]
    public async Task DeviceAuth_StopsMintingAccountsOnceTheLimitIsReached()
    {
        await using var api = Factory(("RateLimits:DeviceAuthPerWindow", "3"));
        await api.SeedAsync();
        var client = api.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var allowed = await client.PostAsJsonAsync(ApiRoutes.Auth.Device,
                new DeviceAuthRequest($"device-{Guid.NewGuid():N}", Secret));
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var refused = await client.PostAsJsonAsync(ApiRoutes.Auth.Device,
            new DeviceAuthRequest($"device-{Guid.NewGuid():N}", Secret));

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
    }

    [Fact]
    public async Task ARefusedRequest_SaysHowLongToWait_AndSaysItInProblemJson()
    {
        await using var api = Factory(("RateLimits:DeviceAuthPerWindow", "1"));
        await api.SeedAsync();
        var client = api.CreateClient();

        await client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest($"device-{Guid.NewGuid():N}", Secret));
        var refused = await client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest($"device-{Guid.NewGuid():N}", Secret));

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);

        // "Not yet" rather than "no": without this the app cannot tell a wait from a failure.
        Assert.NotNull(refused.Headers.RetryAfter);

        Assert.Equal("application/problem+json", refused.Content.Headers.ContentType?.MediaType);
        var problem = await refused.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Contains("Try again in about", problem!["detail"].ToString());
    }

    [Fact]
    public async Task AnAccountThatAlreadyExists_IsStillRefusedOnceTheLimitIsReached()
    {
        // The limit is on the endpoint, not on account creation, because hashing the password
        // costs the same either way - a sign-in flood is as expensive as a registration one.
        await using var api = Factory(("RateLimits:DeviceAuthPerWindow", "2"));
        await api.SeedAsync();
        var client = api.CreateClient();
        var deviceId = $"device-{Guid.NewGuid():N}";

        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest(deviceId, Secret))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest(deviceId, Secret))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest(deviceId, Secret))).StatusCode);
    }

    [Fact]
    public async Task TheHealthCheck_IsNeverLimited()
    {
        // The proxy checks this every 30 seconds from one address. A limiter must never be
        // the reason a healthy container is reported unhealthy.
        await using var api = Factory(("RateLimits:GlobalPerWindow", "2"));
        await api.SeedAsync();
        var client = api.CreateClient();

        for (var i = 0; i < 8; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task OrdinaryReads_AreLimitedByTheBackstop()
    {
        // Nothing decorates the dishes endpoint, so this proves the global limiter is what
        // catches an endpoint nobody remembered.
        await using var api = Factory(("RateLimits:GlobalPerWindow", "3"));
        await api.SeedAsync();
        var client = api.CreateClient();

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(ApiRoutes.Dishes.Collection)).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.GetAsync(ApiRoutes.Dishes.Collection)).StatusCode);
    }
}
