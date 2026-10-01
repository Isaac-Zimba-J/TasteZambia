namespace TasteZambia.API.Data.Entities;

/// <summary>
/// One privileged action, recorded so it can be answered for afterwards.
///
/// This is not the same as a ReviewEvent. Those exist so a contributor can see what happened
/// to their recipe, and their Actor is a display name the reviewer can change at any time -
/// which makes them a story, not a record. This table keeps the account id, is written only
/// by the server, and is never updated or deleted.
/// </summary>
public class AuditEntry
{
    public long Id { get; set; }

    public DateTimeOffset At { get; set; }

    /// <summary>The account that acted. Stable, unlike the name.</summary>
    public required string ActorUserId { get; set; }

    /// <summary>What they were called at the time, so the row reads without a join.</summary>
    public required string ActorName { get; set; }

    /// <summary>A dotted verb: "review.publish", "review.request-changes", "admin.roles.set".</summary>
    public required string Action { get; set; }

    /// <summary>What it was done to - a contribution id, a user name.</summary>
    public required string Subject { get; set; }

    /// <summary>Anything worth knowing that is not in the other columns. Never a secret.</summary>
    public string? Detail { get; set; }
}
