using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Media;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Media;

[Collection(nameof(DatabaseCollection))]
public class MediaEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        (_client, _) = await _factory.SignedInClientAsync();
    }

    public Task DisposeAsync() { _client.Dispose(); _factory.Dispose(); return Task.CompletedTask; }

    private static MultipartFormDataContent Upload(byte[] bytes, string contentType, MediaKind kind = MediaKind.Photo)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        return new MultipartFormDataContent
        {
            { file, "file", "whatever-the-client-called-it.jpg" },
            { new StringContent(kind.ToString()), "kind" },
        };
    }

    [Fact]
    public async Task Upload_ThenFetch_ReturnsTheBytesWithTheirType()
    {
        var bytes = new byte[] { 7, 7, 7, 7 };

        var response = await _client.PostAsync(ApiRoutes.Media.Collection, Upload(bytes, "image/jpeg"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UploadResultDto>();

        var fetched = await _client.GetAsync(result!.Url);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        Assert.Equal("image/jpeg", fetched.Content.Headers.ContentType!.MediaType);
        Assert.Equal(bytes, await fetched.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Upload_OfADisallowedType_Is415()
    {
        var response = await _client.PostAsync(ApiRoutes.Media.Collection, Upload([1], "application/pdf"));
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Upload_OverTheImageCeiling_Is413()
    {
        var tooBig = new byte[TasteZambia.Shared.Validation.MediaLimits.MaxImageBytes + 1];
        var response = await _client.PostAsync(ApiRoutes.Media.Collection, Upload(tooBig, "image/png"));
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task SomeoneElsesMedia_Is404_EvenHoldingTheId()
    {
        var mine = await (await _client.PostAsync(ApiRoutes.Media.Collection, Upload([1, 2], "image/png")))
            .Content.ReadFromJsonAsync<UploadResultDto>();

        var (stranger, _) = await _factory.SignedInClientAsync();
        using (stranger)
            Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(mine!.Url)).StatusCode);
    }

    [Fact]
    public async Task Media_WithoutAToken_Is401()
    {
        using var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync(ApiRoutes.Media.ById.Replace("{id}", Guid.NewGuid().ToString()))).StatusCode);
    }

    [Fact]
    public async Task AnAudioUpload_IsAllowedUpToTheAudioCeiling()
    {
        var bytes = new byte[12 * 1024 * 1024];   // a 12-minute recording, over the image limit
        var response = await _client.PostAsync(ApiRoutes.Media.Collection, Upload(bytes, "audio/mp4", MediaKind.Audio));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
