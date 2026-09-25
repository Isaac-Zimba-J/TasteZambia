using TasteZambia.Shared.Enums;

namespace TasteZambia.Shared.Contracts.Media;

public sealed record MediaAssetDto(Guid Id, MediaKind Kind, string ContentType, long Length, string? Caption, DateTimeOffset CreatedAt)
{
    /// <summary>Where the bytes are. Relative, so it works against whichever host the app is pointed at.</summary>
    public string Url => $"/api/v1/media/{Id}";
}

public sealed record UploadResultDto(Guid Id, string Url);
