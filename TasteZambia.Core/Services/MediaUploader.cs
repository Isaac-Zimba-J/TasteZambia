using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Media;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Services;

public sealed record PendingUpload(Guid LocalId, string ContentType, MediaKind Kind, string CachePath);

public interface IMediaUploader
{
    /// <summary>The archive's id for the file, or null when it could not be sent right now.</summary>
    Task<Guid?> UploadAsync(PickedFile file, MediaKind kind, CancellationToken ct = default);

    /// <summary>Retries everything queued. Returns how many went through.</summary>
    Task<int> DrainAsync(CancellationToken ct = default);

    IReadOnlyList<PendingUpload> Pending { get; }
}

/// <summary>
/// Uploads a photograph or a recording, and keeps it when the archive cannot be reached.
/// A reader who has just taken a picture of their grandmother's cooking should not lose
/// it because the signal dropped.
/// </summary>
public sealed class MediaUploader(HttpClient api, ILocalStore local, TimeProvider clock) : IMediaUploader
{
    private const string Key = "media.pending";
    private readonly List<PendingUpload> _pending = local.Get<List<PendingUpload>>(Key) ?? [];

    // Not read yet: reserved for backing off retries once DrainAsync gets called on a timer.
    private readonly TimeProvider _clock = clock;

    public IReadOnlyList<PendingUpload> Pending => _pending.ToList();

    public async Task<Guid?> UploadAsync(PickedFile file, MediaKind kind, CancellationToken ct = default)
    {
        await using var content = await file.Open(ct);
        var bytes = new MemoryStream();
        await content.CopyToAsync(bytes, ct);
        bytes.Position = 0;

        var outcome = await SendAsync(bytes, file.ContentType, kind, ct);
        if (outcome.Id is { } id) return id;
        if (outcome.Refused) return null;   // the archive will never take it; queueing would be a lie

        Queue(bytes.ToArray(), file.ContentType, kind);
        return null;
    }

    public async Task<int> DrainAsync(CancellationToken ct = default)
    {
        var sent = 0;
        foreach (var item in _pending.ToList())
        {
            if (!File.Exists(item.CachePath)) { Forget(item); continue; }

            await using var stream = File.OpenRead(item.CachePath);
            var outcome = await SendAsync(stream, item.ContentType, item.Kind, ct);
            if (outcome.Id is null && !outcome.Refused)
                break;   // still offline: keep the rest queued, in order

            Forget(item);
            if (outcome.Id is not null) sent++;
        }
        return sent;
    }

    private async Task<SendOutcome> SendAsync(Stream content, string contentType, MediaKind kind, CancellationToken ct)
    {
        try
        {
            var file = new StreamContent(content);
            file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

            using var form = new MultipartFormDataContent
            {
                { file, "file", "upload" },
                { new StringContent(kind.ToString()), "kind" },
            };

            using var response = await api.PostAsync(ApiRoutes.Media.Collection, form, ct);
            if (response.StatusCode is HttpStatusCode.UnsupportedMediaType or HttpStatusCode.RequestEntityTooLarge)
                return SendOutcome.Rejected;   // retrying will not change the answer

            response.EnsureSuccessStatusCode();
            var id = (await response.Content.ReadFromJsonAsync<UploadResultDto>(ct))?.Id;
            return id is { } value ? SendOutcome.Sent(value) : SendOutcome.Unreachable;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return SendOutcome.Unreachable;
        }
    }

    private void Queue(byte[] bytes, string contentType, MediaKind kind)
    {
        var localId = Guid.NewGuid();
        var path = Path.Combine(Path.GetTempPath(), $"tz-upload-{localId:N}");
        File.WriteAllBytes(path, bytes);

        _pending.Add(new PendingUpload(localId, contentType, kind, path));
        local.Set(Key, _pending);
    }

    private void Forget(PendingUpload item)
    {
        _pending.Remove(item);
        local.Set(Key, _pending);
        if (File.Exists(item.CachePath)) File.Delete(item.CachePath);
    }

    /// <summary>What became of one attempt to send a file to the archive.</summary>
    private readonly record struct SendOutcome(Guid? Id, bool Refused)
    {
        public static SendOutcome Sent(Guid id) => new(id, false);

        /// <summary>The archive will never accept this file: the wrong type, or too large.</summary>
        public static readonly SendOutcome Rejected = new(null, true);

        /// <summary>Could not reach the archive. Worth keeping and trying later.</summary>
        public static readonly SendOutcome Unreachable = new(null, false);
    }
}
