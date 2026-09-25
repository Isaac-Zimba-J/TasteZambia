namespace TasteZambia.Shared.Validation;

/// <summary>
/// Shared field limits so client-side validation and server-side validation
/// can never disagree about what will be accepted.
/// </summary>
public static class FieldLimits
{
    public const int LocalNameMax = 120;
    public const int EnglishNameMax = 160;
    public const int ShortDescriptionMax = 400;
    public const int StoryMax = 4000;
    public const int StepBodyMax = 1000;
    public const int QuantityMax = 60;
    public const int MaxStepsPerRecipe = 40;
    public const int MaxIngredientsPerRecipe = 60;
    public const int MaxPhotosPerRecipe = 10;
}

/// <summary>
/// What the archive will accept as an upload. Types are allow-listed rather than sniffed
/// from a filename, and the extension is derived here so a client-supplied name never
/// reaches the filesystem.
/// </summary>
public static class MediaLimits
{
    public const long MaxImageBytes = 10 * 1024 * 1024;
    public const long MaxAudioBytes = 40 * 1024 * 1024;

    public static readonly string[] ImageTypes = ["image/jpeg", "image/png", "image/webp"];
    public static readonly string[] AudioTypes = ["audio/mp4", "audio/aac", "audio/mpeg", "audio/wav"];

    public static bool IsImage(string contentType) => ImageTypes.Contains(contentType);
    public static bool IsAudio(string contentType) => AudioTypes.Contains(contentType);

    public static long MaxBytesFor(string contentType)
        => IsAudio(contentType) ? MaxAudioBytes : MaxImageBytes;

    /// <summary>The stored extension for an allowed type, or null when the type is not allowed.</summary>
    public static string? ExtensionFor(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "audio/mp4" => ".m4a",
        "audio/aac" => ".aac",
        "audio/mpeg" => ".mp3",
        "audio/wav" => ".wav",
        _ => null,
    };
}
