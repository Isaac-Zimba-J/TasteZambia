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

    /// <summary>Removes a queued upload before it is ever sent — a no-op if it already went, or never queued.</summary>
    void Cancel(Guid localId);

    IReadOnlyList<PendingUpload> Pending { get; }

    /// <summary>Something was queued or unqueued; a caller may want to retry sooner rather than later.</summary>
    event EventHandler? Changed;

    /// <summary>A queued file's cache copy was gone when a drain reached it: lost, not merely delayed.</summary>
    event EventHandler<Guid>? Lost;
}

/// <summary>
/// Uploads a photograph or a recording, and keeps it when the archive cannot be reached.
/// A reader who has just taken a picture of their grandmother's cooking should not lose
/// it because the signal dropped.
/// </summary>
public sealed class MediaUploader(HttpClient api, ILocalStore local, TimeProvider clock, IAppStorage storage) : IMediaUploader
{
    private const string Key = "media.pending";
    private readonly List<PendingUpload> _pending = local.Get<List<PendingUpload>>(Key) ?? [];

    // Not read yet: reserved for backing off retries once DrainAsync gets called on a timer.
    private readonly TimeProvider _clock = clock;

    public IReadOnlyList<PendingUpload> Pending => _pending.ToList();

    public event EventHandler? Changed;
    public event EventHandler<Guid>? Lost;

    public async Task<Guid?> UploadAsync(PickedFile file, MediaKind kind, CancellationToken ct = default)
    {
        // Land the bytes on disk once, at the path a retry would use anyway. Sending from
        // this file (rather than a second in-memory copy) is what keeps a 40 MB recording
        // from ever needing 80 MB resident at once.
        var localId = Guid.NewGuid();
        var path = CachePath(localId);
        System.IO.Directory.CreateDirectory(storage.Directory);
        await using (var content = await file.Open(ct))
        await using (var dest = File.Create(path))
            await content.CopyToAsync(dest, ct);

        var outcome = await SendFileAsync(path, file.ContentType, kind, ct);
        if (outcome.Id is { } id) { File.Delete(path); return id; }
        if (outcome.Refused) { File.Delete(path); return null; }   // the archive will never take it; queueing would be a lie

        _pending.Add(new PendingUpload(localId, file.ContentType, kind, path));
        local.Set(Key, _pending);
        Changed?.Invoke(this, EventArgs.Empty);
        return null;
    }

    public async Task<int> DrainAsync(CancellationToken ct = default)
    {
        var sent = 0;
        foreach (var item in _pending.ToList())
        {
            if (!File.Exists(item.CachePath))
            {
                // The file this queue exists to protect is simply gone (Android reclaimed
                // it, or something else deleted it). Forgetting silently would repeat the
                // exact loss this feature was built to prevent, so this is reported instead.
                Forget(item);
                Lost?.Invoke(this, item.LocalId);
                continue;
            }

            var outcome = await SendFileAsync(item.CachePath, item.ContentType, item.Kind, ct);
            if (outcome.Id is null && !outcome.Refused)
                break;   // still offline: keep the rest queued, in order

            Forget(item);
            if (outcome.Id is not null) sent++;
        }
        return sent;
    }

    public void Cancel(Guid localId)
    {
        var item = _pending.FirstOrDefault(p => p.LocalId == localId);
        if (item is not null) Forget(item);
    }

    private async Task<SendOutcome> SendFileAsync(string path, string contentType, MediaKind kind, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await SendAsync(stream, contentType, kind, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return SendOutcome.Unreachable;
        }
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

    private string CachePath(Guid localId) => Path.Combine(storage.Directory, $"tz-upload-{localId:N}");

    private void Forget(PendingUpload item)
    {
        _pending.Remove(item);
        local.Set(Key, _pending);
        if (File.Exists(item.CachePath)) File.Delete(item.CachePath);
        Changed?.Invoke(this, EventArgs.Empty);
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
