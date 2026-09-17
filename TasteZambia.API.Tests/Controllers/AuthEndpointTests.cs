using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

[Collection(nameof(DatabaseCollection))]
public class AuthEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static DeviceAuthRequest NewDevice() => new(
        $"device-{Guid.NewGuid():N}",
        Convert.ToBase64String(Guid.NewGuid().ToByteArray().Concat(Guid.NewGuid().ToByteArray()).ToArray()));

    private static string SubjectOf(AuthTokensDto tokens)
        => new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken).Subject;

    [Fact]
    public async Task FirstCall_CreatesTheAccountAndReturnsTokens()
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, NewDevice());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>();
        Assert.NotEmpty(tokens!.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken);
        Assert.True(tokens.AccessExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SecondCallWithTheSameSecret_SignsInTheSameAccount()
    {
        var device = NewDevice();
        var a = await (await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, device)).Content.ReadFromJsonAsync<AuthTokensDto>();
        var b = await (await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, device)).Content.ReadFromJsonAsync<AuthTokensDto>();

        Assert.Equal(SubjectOf(a!), SubjectOf(b!));
    }

    [Fact]
    public async Task WrongSecretForAKnownDevice_Is401()
    {
        var device = NewDevice();
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, device);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, device with { DeviceSecret = new string('x', 40) });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MalformedDeviceId_Is400()
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest("BAD ID!", new string('x', 40)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesAndRejectsReuse()
    {
        var tokens = await (await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, NewDevice())).Content.ReadFromJsonAsync<AuthTokensDto>();

        var first = await _client.PostAsJsonAsync(ApiRoutes.Auth.Refresh, new RefreshRequest(tokens!.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var rotated = await first.Content.ReadFromJsonAsync<AuthTokensDto>();
        Assert.NotEqual(tokens.RefreshToken, rotated!.RefreshToken);

        var reuse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Refresh, new RefreshRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }
}
