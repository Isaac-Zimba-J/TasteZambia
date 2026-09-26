using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

/// <summary>Wraps MediaPicker, the one MAUI-only piece a photo strip needs. TasteZambia.Core cannot see it.</summary>
public sealed class MauiPhotoPicker : IPhotoPicker
{
    public Task<PickedFile?> CapturePhotoAsync(CancellationToken ct = default) => PickAsync(() => MediaPicker.Default.CapturePhotoAsync());

    // PickPhotoAsync is obsolete in favour of the multi-select PickPhotosAsync; this
    // wizard only ever wants one file at a time, so the first pick is all it keeps.
    public Task<PickedFile?> PickPhotoAsync(CancellationToken ct = default) => PickAsync(async () =>
    {
        var photos = await MediaPicker.Default.PickPhotosAsync();
        return photos.Count > 0 ? photos[0] : null;
    });

    private static async Task<PickedFile?> PickAsync(Func<Task<FileResult?>> pick)
    {
        try
        {
            var result = await pick();
            return result is null ? null : new PickedFile(result.FileName, result.ContentType, _ => result.OpenReadAsync());
        }
        catch (PermissionException)
        {
            // The reader said no to camera or gallery access; that is their call, not a failure.
            return null;
        }
    }
}
