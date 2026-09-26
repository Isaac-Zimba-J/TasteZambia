namespace TasteZambia.Core.Services;

/// <summary>A file the reader chose, opened lazily so a large photo is never held in memory twice.</summary>
public sealed record PickedFile(string FileName, string ContentType, Func<CancellationToken, Task<Stream>> Open);

public interface IPhotoPicker
{
    /// <summary>Null when the reader backed out, which is not a failure.</summary>
    Task<PickedFile?> CapturePhotoAsync(CancellationToken ct = default);
    Task<PickedFile?> PickPhotoAsync(CancellationToken ct = default);
}
