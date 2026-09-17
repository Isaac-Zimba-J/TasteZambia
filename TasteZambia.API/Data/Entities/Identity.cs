using Microsoft.AspNetCore.Identity;

namespace TasteZambia.API.Data.Entities;

public class ArchiveUser : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>One row per issued refresh token. Rotated: consuming one revokes it and issues another.</summary>
public class RefreshToken
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public ArchiveUser User { get; set; } = null!;

    /// <summary>SHA-256 hex of the raw token. The raw value is returned to the client once and never stored.</summary>
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
