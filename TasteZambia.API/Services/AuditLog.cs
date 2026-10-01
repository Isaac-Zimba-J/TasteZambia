using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Auth;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Services;

/// <summary>Verbs, in one place, so a query can rely on their spelling.</summary>
public static class AuditActions
{
    public const string ReviewPublish = "review.publish";
    public const string ReviewRequestChanges = "review.request-changes";
    public const string AdminSetRoles = "admin.roles.set";
    public const string AccountDelete = "account.delete";
}

public interface IAuditLog
{
    /// <summary>
    /// Records a privileged action. Saved on the spot rather than left to the caller's
    /// SaveChanges, so a record exists even when what followed it failed - an attempt is worth
    /// knowing about too.
    /// </summary>
    Task WriteAsync(string action, string subject, string? detail = null, CancellationToken ct = default);
}

public sealed class AuditLog(
    TasteZambiaDbContext db,
    ICurrentUser me,
    TimeProvider clock,
    ILogger<AuditLog> log) : IAuditLog
{
    public async Task WriteAsync(string action, string subject, string? detail = null, CancellationToken ct = default)
    {
        var userId = me.UserId ?? "anonymous";

        var entry = new AuditEntry
        {
            At = clock.GetUtcNow(),
            ActorUserId = userId,
            ActorName = await NameForAsync(userId, ct),
            Action = action,
            Subject = subject,
            Detail = detail,
        };

        db.Add(entry);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // The action itself must not fail because its record could not be written - but a
            // missing audit row is worth shouting about, because it is the thing you reach for
            // when something has gone wrong and nobody remembers doing it.
            log.LogError(ex, "Could not write audit entry {Action} on {Subject} by {UserId}", action, subject, userId);
            db.Entry(entry).State = EntityState.Detached;
        }
    }

    /// <summary>
    /// What to call them in the record. The profile name if they set one, otherwise the device
    /// id - read rather than created, because an audit write must not bring a profile row into
    /// existence as a side effect.
    /// </summary>
    private async Task<string> NameForAsync(string userId, CancellationToken ct)
    {
        var display = await db.UserProfiles
            .Where(p => p.UserId == userId)
            .Select(p => p.DisplayName)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrWhiteSpace(display)) return display;

        return await db.Users.Where(u => u.Id == userId).Select(u => u.UserName).FirstOrDefaultAsync(ct)
               ?? "unknown";
    }
}
