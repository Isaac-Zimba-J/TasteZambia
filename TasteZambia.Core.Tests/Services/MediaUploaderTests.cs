using System.Net;
using Microsoft.Extensions.Time.Testing;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.Services;

public class MediaUploaderTests
{
    private static PickedFile Photo(string name = "kitchen.jpg")
        => new(name, "image/jpeg", _ => Task.FromResult<Stream>(new MemoryStream([1, 2, 3, 4])));

    private static (MediaUploader uploader, ScriptedHandler server, InMemoryLocalStore store) Sut()
    {
        var server = new ScriptedHandler();
        var store = new InMemoryLocalStore();
        var client = new HttpClient(server) { BaseAddress = new Uri("http://archive.test") };
        return (new MediaUploader(client, store, new FakeTimeProvider()), server, store);
    }

    [Fact]
    public async Task ASuccessfulUpload_ReturnsTheServerIdAndQueuesNothing()
    {
        var (uploader, server, _) = Sut();
        var id = Guid.NewGuid();
        server.NextId = id;

        Assert.Equal(id, await uploader.UploadAsync(Photo(), MediaKind.Photo, default));
        Assert.Empty(uploader.Pending);
    }

    [Fact]
    public async Task AnOfflineUpload_KeepsThePhotoAndQueuesIt()
    {
        var (uploader, server, _) = Sut();
        server.IsOffline = true;

        Assert.Null(await uploader.UploadAsync(Photo(), MediaKind.Photo, default));

        var pending = Assert.Single(uploader.Pending);
        Assert.Equal("image/jpeg", pending.ContentType);
        Assert.True(File.Exists(pending.CachePath));   // the bytes the reader chose are still here
    }

    [Fact]
    public async Task Draining_AfterTheArchiveComesBack_SendsTheQueueInOrder()
    {
        var (uploader, server, _) = Sut();
        server.IsOffline = true;
        await uploader.UploadAsync(Photo("first.jpg"), MediaKind.Photo, default);
        await uploader.UploadAsync(Photo("second.jpg"), MediaKind.Photo, default);
        Assert.Equal(2, uploader.Pending.Count);

        server.IsOffline = false;
        Assert.Equal(2, await uploader.DrainAsync(default));

        Assert.Empty(uploader.Pending);
        Assert.Equal(2, server.Uploads);
    }

    [Fact]
    public async Task AQueuedUpload_SurvivesARestart()
    {
        var (uploader, server, store) = Sut();
        server.IsOffline = true;
        await uploader.UploadAsync(Photo(), MediaKind.Photo, default);

        var again = new MediaUploader(new HttpClient(server) { BaseAddress = new Uri("http://archive.test") }, store, new FakeTimeProvider());

        Assert.Single(again.Pending);
    }

    [Fact]
    public async Task ARejectedUpload_IsDroppedRatherThanRetriedForever()
    {
        var (uploader, server, _) = Sut();
        server.Status = HttpStatusCode.UnsupportedMediaType;

        Assert.Null(await uploader.UploadAsync(Photo("notes.pdf"), MediaKind.Photo, default));
        Assert.Empty(uploader.Pending);   // the archive will never take it; queueing is a lie
    }

    /// <summary>Answers uploads, or refuses to connect at all.</summary>
    private sealed class ScriptedHandler : HttpMessageHandler
    {
        public bool IsOffline { get; set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.Created;
        public Guid NextId { get; set; } = Guid.NewGuid();
        public int Uploads { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (IsOffline) throw new HttpRequestException("offline");

            Uploads++;
            var response = new HttpResponseMessage(Status);
            if (Status == HttpStatusCode.Created)
                response.Content = System.Net.Http.Json.JsonContent.Create(
                    new TasteZambia.Shared.Contracts.Media.UploadResultDto(NextId, $"/api/v1/media/{NextId}"));
            return Task.FromResult(response);
        }
    }
}
