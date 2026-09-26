using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Shared.Contracts.Media;

public sealed record MediaAssetDto(Guid Id, MediaKind Kind, string ContentType, long Length, string? Caption, DateTimeOffset CreatedAt)
{
    /// <summary>Where the bytes are. Relative, so it works against whichever host the app is pointed at.</summary>
    public string Url => ApiRoutes.Media.ById.Replace("{id}", Id.ToString());
}

public sealed record UploadResultDto(Guid Id, string Url);
