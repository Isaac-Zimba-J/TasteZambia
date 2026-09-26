using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Media;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Services;

/// <summary>
/// Where an upload is meant to end up. A family recipe is attached the moment the bytes
/// land; a contribution is claimed later, when its draft is submitted, so the queue only
/// has to remember that this file belongs to a draft rather than to nothing.
/// </summary>
public sealed record UploadTarget(Guid? FamilyRecipeId, Guid? DraftId)
{
    public static readonly UploadTarget None = new(null, null);
    public static UploadTarget FamilyRecipe(Guid id) => new(id, null);
    public static UploadTarget Draft(Guid id) => new(null, id);
}

public sealed record PendingUpload(Guid LocalId, string ContentType, MediaKind Kind, string CachePath, UploadTarget Target);

/// <summary>What happened to a file the reader handed over. Exactly one of these is true.</summary>
public abstract record UploadOutcome
{
    /// <summary>It is in the archive, under this id.</summary>
    public sealed record Sent(Guid MediaId) : UploadOutcome;

    /// <summary>Kept on this device and queued. It will go when the archive is reachable.</summary>
    public sealed record Queued(Guid LocalId) : UploadOutcome;

    /// <summary>The archive will never take it, and it has been deleted. <paramref name="Reason"/> is for the reader.</summary>
    public sealed record Refused(string Reason) : UploadOutcome;
}

public interface IMediaUploader
{
    /// <summary>
    /// Sends the file, or keeps it for later, or tells you it will never be accepted.
    /// The three are different things and the reader must not be told the wrong one.
    /// </summary>
    Task<UploadOutcome> UploadAsync(PickedFile file, MediaKind kind, UploadTarget target, CancellationToken ct = default);

    /// <summary>Retries everything queued, attaching each to its target. Returns how many went through.</summary>
    Task<int> DrainAsync(CancellationToken ct = default);

    /// <summary>Media ids uploaded on behalf of a draft, for a submission to claim.</summary>
    IReadOnlyList<Guid> ClaimFor(Guid draftId);

    /// <summary>Forget what a draft was owed, once it has been submitted or abandoned.</summary>
    void ForgetClaims(Guid draftId);

    /// <summary>Removes a queued upload before it is ever sent — a no-op if it already went, or never queued.</summary>
    void Cancel(Guid localId);

    IReadOnlyList<PendingUpload> Pending { get; }

    /// <summary>Something was queued or unqueued; a caller may want to retry sooner rather than later.</summary>
    event EventHandler? Changed;

    /// <summary>A queued file's cache copy was gone when a drain reached it: lost, not merely delayed.</summary>
    event EventHandler<Guid>? Lost;

    /// <summary>The archive refused a queued file outright. It is gone; say so rather than waiting.</summary>
    event EventHandler<string>? Rejected;

    /// <summary>A queued file reached the archive. Carries its local id and the id it now has there.</summary>
    event EventHandler<(Guid LocalId, Guid MediaId)>? Uploaded;
}

/// <summary>
/// Uploads a photograph or a recording, and keeps it when the archive cannot be reached.
/// A reader who has just taken a picture of their grandmother's cooking should not lose
/// it because the signal dropped.
/// </summary>
public sealed class MediaUploader(HttpClient api, ILocalStore local, TimeProvider clock, IAppStorage storage) : IMediaUploader
{
    private const string Key = "media.pending";
    private const string ClaimsKey = "media.claims";

    private readonly List<PendingUpload> _pending = local.Get<List<PendingUpload>>(Key) ?? [];

    // Uploads that belong to a draft, waiting for it to be submitted. Persisted, because
    // a draft can outlive several sessions before anyone submits it.
    private readonly Dictionary<Guid, List<Guid>> _claims = local.Get<Dictionary<Guid, List<Guid>>>(ClaimsKey) ?? [];

    // Not read yet: reserved for backing off retries once DrainAsync gets called on a timer.
    private readonly TimeProvider _clock = clock;

    public IReadOnlyList<PendingUpload> Pending => _pending.ToList();

    public event EventHandler? Changed;
    public event EventHandler<Guid>? Lost;
    public event EventHandler<string>? Rejected;
    public event EventHandler<(Guid LocalId, Guid MediaId)>? Uploaded;

    public IReadOnlyList<Guid> ClaimFor(Guid draftId)
        => _claims.TryGetValue(draftId, out var ids) ? ids.ToList() : [];

    public async Task<UploadOutcome> UploadAsync(PickedFile file, MediaKind kind, UploadTarget target, CancellationToken ct = default)
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

        if (outcome.Id is { } id)
        {
            File.Delete(path);
            await AttachAsync(id, target, ct);
            return new UploadOutcome.Sent(id);
        }

        if (outcome.Refused)
        {
            // The archive will never take it, so queueing would be a lie.
            File.Delete(path);
            return new UploadOutcome.Refused(RefusalReason(kind));
        }

        _pending.Add(new PendingUpload(localId, file.ContentType, kind, path, target));
        local.Set(Key, _pending);
        Changed?.Invoke(this, EventArgs.Empty);
        return new UploadOutcome.Queued(localId);
    }

    /// <summary>
    /// A family recipe gets its media the moment the bytes land. A draft's uploads are
    /// remembered instead, for the submission to claim - a draft has no server-side row
    /// to attach to until it is submitted.
    /// </summary>
    private async Task AttachAsync(Guid mediaId, UploadTarget target, CancellationToken ct)
    {
        if (target.FamilyRecipeId is { } recipeId)
        {
            var route = ApiRoutes.Family.Media
                .Replace("{id}", recipeId.ToString())
                .Replace("{mediaId}", mediaId.ToString());
            try
            {
                using var response = await api.PutAsync(route, null, ct);
                // A 404 means the recipe is no longer the caller's to add to. The bytes
                // are safely in the archive either way; there is nothing to retry.
                _ = response.IsSuccessStatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Uploaded but not attached. The next drain does not revisit this, so a
                // recipe can be missing a photo the archive holds - recorded as a known
                // gap rather than silently retried forever.
            }
        }
        else if (target.DraftId is { } draftId)
        {
            if (!_claims.TryGetValue(draftId, out var ids)) _claims[draftId] = ids = [];
            if (!ids.Contains(mediaId)) ids.Add(mediaId);
            local.Set(ClaimsKey, _claims);
        }
    }

    private static string RefusalReason(MediaKind kind) => kind == MediaKind.Audio
        ? "That recording is too long for the archive to take. Try a shorter one."
        : "The archive could not take that file. It needs a JPEG, PNG or WebP photograph under 10 MB.";

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

            if (outcome.Id is { } mediaId)
            {
                await AttachAsync(mediaId, item.Target, ct);
                Uploaded?.Invoke(this, (item.LocalId, mediaId));
                sent++;
            }
            else
            {
                // Refused after waiting. Forgetting quietly would leave the reader
                // believing a photograph is still on its way.
                Rejected?.Invoke(this, RefusalReason(item.Kind));
            }
        }
        return sent;
    }

    /// <summary>Forget what a draft was owed, once it has been submitted or abandoned.</summary>
    public void ForgetClaims(Guid draftId)
    {
        if (_claims.Remove(draftId)) local.Set(ClaimsKey, _claims);
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
