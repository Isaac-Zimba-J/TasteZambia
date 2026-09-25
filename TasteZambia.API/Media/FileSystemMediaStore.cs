using TasteZambia.Shared.Validation;

namespace TasteZambia.API.Media;

/// <summary>
/// Blobs on disk, two levels of fan-out so no directory holds a million entries.
/// Writes go to a temporary file and are moved into place, so a dropped connection
/// leaves nothing readable behind.
/// </summary>
public sealed class FileSystemMediaStore(string root) : IMediaStore
{
    public async Task<StoredBlob> SaveAsync(Guid id, string contentType, Stream content, CancellationToken ct)
    {
        var path = PathFor(id, contentType);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temporary = path + ".partial";
        long length;
        try
        {
            await using (var file = File.Create(temporary))
            {
                await content.CopyToAsync(file, ct);
                length = file.Length;
            }
            File.Move(temporary, path, overwrite: true);
        }
        catch
        {
            // Nothing half-written survives: the reader asked for a photo, not a fragment.
            if (File.Exists(temporary)) File.Delete(temporary);
            throw;
        }

        return new StoredBlob(id, contentType, length);
    }

    public Task<Stream?> OpenAsync(Guid id, string contentType, CancellationToken ct)
    {
        var path = PathFor(id, contentType);
        return Task.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }

    public Task DeleteAsync(Guid id, string contentType, CancellationToken ct)
    {
        var path = PathFor(id, contentType);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string PathFor(Guid id, string contentType)
    {
        var name = id.ToString("N");
        var extension = MediaLimits.ExtensionFor(contentType)
            ?? throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Not an allowed media type.");
        return Path.Combine(root, name[..2], name[2..4], name + extension);
    }
}
