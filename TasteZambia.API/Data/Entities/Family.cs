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

/// <summary>
/// A recipe kept for a family rather than the public archive. Its visibility is decided
/// by Privacy plus the member list, and nothing reads one except through
/// FamilyAccessService.
/// </summary>
public class FamilyRecipe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string OwnerId { get; set; }

    public required string LocalName { get; set; }
    public string Description { get; set; } = "";
    public string Province { get; set; } = "";
    public string Language { get; set; } = "";
    public string TaughtBy { get; set; } = "";
    public string TaughtByOrigin { get; set; } = "";
    public string Story { get; set; } = "";
    public string TraditionalMethod { get; set; } = "";

    public PrivacyLevel Privacy { get; set; } = PrivacyLevel.PrivateToMe;
    public TranscriptState Transcript { get; set; } = TranscriptState.None;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the family agreed to publish it and a reviewer did.</summary>
    public string? PublishedDishId { get; set; }

    public List<FamilyMember> Members { get; set; } = [];
    public List<FamilyNote> Notes { get; set; } = [];
    public List<MediaAsset> Media { get; set; } = [];
}

public class FamilyMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FamilyRecipeId { get; set; }

    /// <summary>Null until the invite is accepted on that person's own device.</summary>
    public string? UserId { get; set; }

    public required string DisplayName { get; set; }
    public string Relation { get; set; } = "";
    public MemberState State { get; set; }
    public DateTimeOffset InvitedAt { get; set; }
    public DateTimeOffset? JoinedAt { get; set; }
}

public class FamilyNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FamilyRecipeId { get; set; }
    public required string AuthorUserId { get; set; }
    public required string AuthorName { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// A short code the owner passes on however they like - there is no email on an
/// anonymous account. Whoever enters it on their own device becomes that member.
/// </summary>
public class FamilyInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FamilyRecipeId { get; set; }
    public Guid MemberId { get; set; }
    public required string Code { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RedeemedAt { get; set; }

    public bool IsOpen(DateTimeOffset now) => RedeemedAt is null && ExpiresAt > now;
}
