using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Media;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Validation;

namespace TasteZambia.API.Services;

public interface IMediaService
{
    Task<MediaAsset> UploadAsync(string userId, MediaKind kind, string contentType, Stream content, long declaredLength, CancellationToken ct);

    /// <summary>The asset and its bytes, or null when it does not exist or is not this user's to read.</summary>
    Task<(MediaAsset Asset, Stream Content)?> OpenAsync(Guid id, string userId, CancellationToken ct);

    Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct);
}

public sealed class MediaService(TasteZambiaDbContext db, IMediaStore store, IFamilyAccessService access, TimeProvider clock) : IMediaService
{
    public async Task<MediaAsset> UploadAsync(string userId, MediaKind kind, string contentType, Stream content, long declaredLength, CancellationToken ct)
    {
        if (MediaLimits.ExtensionFor(contentType) is null)
            throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Not an allowed media type.");

        // A JPEG posted as MediaKind.Audio would make a family recipe's HasAudio true
        // and every "with her recording" label fire on a photograph.
        var kindMatchesType = kind switch
        {
            MediaKind.Photo => MediaLimits.IsImage(contentType),
            MediaKind.Audio => MediaLimits.IsAudio(contentType),
            _ => false,
        };
        if (!kindMatchesType)
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "The declared kind does not match the file's content type.");

        var ceiling = MediaLimits.MaxBytesFor(contentType);
        if (declaredLength > ceiling)
            throw new InvalidDataException($"That file is larger than the {ceiling / (1024 * 1024)} MB limit.");

        var asset = new MediaAsset
        {
            UserId = userId,
            Kind = kind,
            ContentType = contentType,
            Caption = null,
            CreatedAt = clock.GetUtcNow(),
        };

        var stored = await store.SaveAsync(asset.Id, contentType, content, ct);

        // The bytes landed but the row did not: that is a leaked blob, so clean up.
        asset.Length = stored.Length;
        try
        {
            db.MediaAssets.Add(asset);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await store.DeleteAsync(asset.Id, contentType, CancellationToken.None);
            throw;
        }

        return asset;
    }

    public async Task<(MediaAsset Asset, Stream Content)?> OpenAsync(Guid id, string userId, CancellationToken ct)
    {
        var asset = await db.MediaAssets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return null;
        if (!await access.CanReadMediaAsync(asset, userId, ct)) return null;

        var content = await store.OpenAsync(id, asset.ContentType, ct);
        return content is null ? null : (asset, content);
    }

    public async Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct)
    {
        var asset = await db.MediaAssets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (asset is null) return false;

        db.MediaAssets.Remove(asset);
        await db.SaveChangesAsync(ct);
        await store.DeleteAsync(id, asset.ContentType, ct);
        return true;
    }
}
