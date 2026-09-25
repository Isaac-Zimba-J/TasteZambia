using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Data.Entities;

/// <summary>
/// One stored blob. The row is the record; the bytes live in IMediaStore. Deleting the
/// row without the blob leaks disk, so MediaService always does both.
/// </summary>
public class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public MediaKind Kind { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public string? Caption { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>What it belongs to. Both null while it is still just an upload.</summary>
    public Guid? FamilyRecipeId { get; set; }
    public Guid? ContributionId { get; set; }
}
