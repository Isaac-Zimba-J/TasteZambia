using System.Net;
using Microsoft.Extensions.Time.Testing;
using TasteZambia.Core.Services;
using TasteZambia.Core.Tests.Fakes;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.Services;

public class MediaUploaderTests
{
    private static PickedFile Photo(string name = "kitchen.jpg")
        => new(name, "image/jpeg", _ => Task.FromResult<Stream>(new MemoryStream([1, 2, 3, 4])));

    private static (MediaUploader uploader, ScriptedHandler server, InMemoryLocalStore store, TemporaryAppStorage storage) Sut()
    {
        var server = new ScriptedHandler();
        var store = new InMemoryLocalStore();
        var storage = new TemporaryAppStorage();
        var client = new HttpClient(server) { BaseAddress = new Uri("http://archive.test") };
        return (new MediaUploader(client, store, new FakeTimeProvider(), storage), server, store, storage);
    }

    [Fact]
    public async Task ASuccessfulUpload_ReturnsTheServerIdAndQueuesNothing()
    {
        var (uploader, server, _, _) = Sut();
        var id = Guid.NewGuid();
        server.NextId = id;

        var outcome = Assert.IsType<UploadOutcome.Sent>(await uploader.UploadAsync(Photo(), MediaKind.Photo, UploadTarget.None));
        Assert.Equal(id, outcome.MediaId);
        Assert.Empty(uploader.Pending);
    }

    [Fact]
    public async Task AnOfflineUpload_KeepsThePhotoAndQueuesIt()
    {
        var (uploader, server, _, _) = Sut();
        server.IsOffline = true;

        Assert.IsType<UploadOutcome.Queued>(await uploader.UploadAsync(Photo(), MediaKind.Photo, UploadTarget.None));

        var pending = Assert.Single(uploader.Pending);
        Assert.Equal("image/jpeg", pending.ContentType);
        Assert.True(File.Exists(pending.CachePath));   // the bytes the reader chose are still here
    }

    [Fact]
    public async Task Draining_AfterTheArchiveComesBack_SendsTheQueueInOrder()
    {
        var (uploader, server, _, _) = Sut();
        server.IsOffline = true;
        await uploader.UploadAsync(Photo("first.jpg"), MediaKind.Photo, UploadTarget.None);
        await uploader.UploadAsync(Photo("second.jpg"), MediaKind.Photo, UploadTarget.None);
        Assert.Equal(2, uploader.Pending.Count);

        server.IsOffline = false;
        Assert.Equal(2, await uploader.DrainAsync(default));

        Assert.Empty(uploader.Pending);
        Assert.Equal(2, server.Uploads);
    }

    [Fact]
    public async Task AQueuedUpload_SurvivesARestart()
    {
        var (uploader, server, store, _) = Sut();
        server.IsOffline = true;
        await uploader.UploadAsync(Photo(), MediaKind.Photo, UploadTarget.None);

        var again = new MediaUploader(new HttpClient(server) { BaseAddress = new Uri("http://archive.test") }, store, new FakeTimeProvider(), new TemporaryAppStorage());

        Assert.Single(again.Pending);
    }

    [Fact]
    public async Task ARejectedUpload_IsDroppedRatherThanRetriedForever()
    {
        var (uploader, server, _, _) = Sut();
        server.Status = HttpStatusCode.UnsupportedMediaType;

        Assert.IsType<UploadOutcome.Refused>(await uploader.UploadAsync(Photo("notes.pdf"), MediaKind.Photo, UploadTarget.None));
        Assert.Empty(uploader.Pending);   // the archive will never take it; queueing is a lie
    }

    [Fact]
    public async Task ACancelledPendingUpload_IsNotSentOnTheNextDrain()
    {
        var (uploader, server, _, _) = Sut();
        server.IsOffline = true;
        await uploader.UploadAsync(Photo(), MediaKind.Photo, UploadTarget.None);
        var queued = Assert.Single(uploader.Pending);

        uploader.Cancel(queued.LocalId);
        Assert.Empty(uploader.Pending);
        Assert.False(File.Exists(queued.CachePath));   // the reader deleted it; nothing should keep it around

        server.IsOffline = false;
        Assert.Equal(0, await uploader.DrainAsync(default));
        Assert.Equal(0, server.Uploads);
    }

    [Fact]
    public async Task ASuccessfulUpload_LeavesNoFileBehind()
    {
        var (uploader, server, _, storage) = Sut();

        Assert.NotNull(await uploader.UploadAsync(Photo(), MediaKind.Photo, UploadTarget.None));

        Assert.Empty(Directory.GetFiles(storage.Directory));
    }

    /// <summary>Answers uploads, or refuses to connect at all.</summary>
    // ---- What the whole-branch review found broken: the queue had no destination, and
    // ---- "queued" was indistinguishable from "refused". ----

    [Fact]
    public async Task AnUploadForAFamilyRecipe_AttachesItselfOnTheSpot()
    {
        var (uploader, server, _, _) = Sut();
        var recipe = Guid.NewGuid();
        server.NextId = Guid.NewGuid();

        await uploader.UploadAsync(Photo(), MediaKind.Photo, UploadTarget.FamilyRecipe(recipe));

        var attach = Assert.Single(server.Attaches);
        Assert.Contains(recipe.ToString(), attach);
        Assert.Contains(server.NextId.ToString(), attach);
    }

    [Fact]
    public async Task AQueuedFamilyUpload_AttachesWhenTheDrainFinallyGoesThrough()
    {
        var (uploader, server, _, _) = Sut();
        var recipe = Guid.NewGuid();
        server.IsOffline = true;
        await uploader.UploadAsync(Photo(), MediaKind.Photo, UploadTarget.FamilyRecipe(recipe));
        Assert.Empty(server.Attaches);

        server.IsOffline = false;
        Assert.Equal(1, await uploader.DrainAsync());

        // The point of the queue: the photograph reaches the recipe, not just the archive.
        Assert.Contains(recipe.ToString(), Assert.Single(server.Attaches));
    }

    [Fact]
    public async Task ADraftsUploads_AreHeldForItsSubmissionToClaim()
    {
        var (uploader, server, _, _) = Sut();
        var draft = Guid.NewGuid();
        var first = Guid.NewGuid();
        server.NextId = first;
        await uploader.UploadAsync(Photo("one.jpg"), MediaKind.Photo, UploadTarget.Draft(draft));

        var second = Guid.NewGuid();
        server.NextId = second;
        await uploader.UploadAsync(Photo("two.jpg"), MediaKind.Photo, UploadTarget.Draft(draft));

        Assert.Equal([first, second], uploader.ClaimFor(draft));
        Assert.Empty(uploader.ClaimFor(Guid.NewGuid()));
        Assert.Empty(server.Attaches);   // a draft has nothing to attach to yet

        uploader.ForgetClaims(draft);
        Assert.Empty(uploader.ClaimFor(draft));
    }

    [Fact]
    public async Task AQueuedDraftUpload_JoinsTheClaimOnceItGoes()
    {
        var (uploader, server, _, _) = Sut();
        var draft = Guid.NewGuid();
        server.IsOffline = true;
        await uploader.UploadAsync(Photo(), MediaKind.Photo, UploadTarget.Draft(draft));
        Assert.Empty(uploader.ClaimFor(draft));

        server.IsOffline = false;
        server.NextId = Guid.NewGuid();
        await uploader.DrainAsync();

        Assert.Equal([server.NextId], uploader.ClaimFor(draft));
    }

    [Fact]
    public async Task ARefusalCarriesAReasonWorthShowing_AndIsNotConfusedWithQueueing()
    {
        var (uploader, server, _, _) = Sut();
        server.Status = HttpStatusCode.RequestEntityTooLarge;

        var photo = Assert.IsType<UploadOutcome.Refused>(await uploader.UploadAsync(Photo(), MediaKind.Photo, UploadTarget.None));
        Assert.Contains("photograph", photo.Reason);

        var recording = Assert.IsType<UploadOutcome.Refused>(
            await uploader.UploadAsync(new PickedFile("her-voice.m4a", "audio/mp4", _ => Task.FromResult<Stream>(new MemoryStream([1]))),
                MediaKind.Audio, UploadTarget.None));
        Assert.Contains("recording", recording.Reason);

        Assert.Empty(uploader.Pending);   // refused, deleted - never queued
    }

    [Fact]
    public async Task AQueuedUploadRefusedLater_TellsSomeoneRatherThanVanishing()
    {
        var (uploader, server, _, _) = Sut();
        server.IsOffline = true;
        await uploader.UploadAsync(Photo(), MediaKind.Photo, UploadTarget.None);

        string? reported = null;
        uploader.Rejected += (_, reason) => reported = reason;

        server.IsOffline = false;
        server.Status = HttpStatusCode.UnsupportedMediaType;
        Assert.Equal(0, await uploader.DrainAsync());

        Assert.NotNull(reported);
        Assert.Empty(uploader.Pending);
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        public bool IsOffline { get; set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.Created;
        public Guid NextId { get; set; } = Guid.NewGuid();
        public int Uploads { get; private set; }

        /// <summary>Every PUT the uploader made to attach a blob to a family recipe.</summary>
        public List<string> Attaches { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (IsOffline) throw new HttpRequestException("offline");

            if (request.Method == HttpMethod.Put)
            {
                Attaches.Add(request.RequestUri!.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            Uploads++;
            var response = new HttpResponseMessage(Status);
            if (Status == HttpStatusCode.Created)
                response.Content = System.Net.Http.Json.JsonContent.Create(
                    new TasteZambia.Shared.Contracts.Media.UploadResultDto(NextId, $"/api/v1/media/{NextId}"));
            return Task.FromResult(response);
        }
    }
}
