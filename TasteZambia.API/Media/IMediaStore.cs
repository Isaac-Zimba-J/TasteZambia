namespace TasteZambia.API.Media;

public readonly record struct StoredBlob(Guid Id, string ContentType, long Length);

/// <summary>
/// Where the bytes live. One implementation today writes to disk; production swaps it
/// for object storage and nothing above this interface changes.
/// </summary>
public interface IMediaStore
{
    Task<StoredBlob> SaveAsync(Guid id, string contentType, Stream content, CancellationToken ct);

    /// <summary>The blob's bytes, or null when it is not there.</summary>
    Task<Stream?> OpenAsync(Guid id, string contentType, CancellationToken ct);

    Task DeleteAsync(Guid id, string contentType, CancellationToken ct);
}
