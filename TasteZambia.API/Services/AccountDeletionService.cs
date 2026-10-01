using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Media;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Services;

/// <summary>What a deletion took away, so the reader can be told rather than just obeyed.</summary>
public sealed record DeletionSummary(
    int UnpublishedContributions,
    int PublishedContributionsAnonymised,
    int FamilyRecipesDeleted,
    int FamilyRecipesLeft,
    int NotesAnonymised,
    int MediaDeleted);

/// <summary>
/// The result of asking for a deletion. A mismatched confirmation is an ordinary answer, not
/// an exception: it is what a retried or mistaken request looks like, and it is expected.
/// </summary>
public abstract record DeletionOutcome
{
    public sealed record Done(DeletionSummary Summary) : DeletionOutcome;

    /// <summary>The device id sent did not belong to the caller's account. Nothing was touched.</summary>
    public sealed record ConfirmationMismatch : DeletionOutcome;
}

public interface IAccountDeletionService
{
    /// <summary>
    /// <paramref name="confirmDeviceId"/> must be the account's own device id. The bearer token
    /// already proved who this is, so this is not a security check - it is what stops a retried
    /// or mistapped request destroying an archive of family recipes.
    /// </summary>
    Task<DeletionOutcome> DeleteAsync(string userId, string confirmDeviceId, CancellationToken ct = default);
}

/// <summary>
/// Deletes an account and everything the archive holds for it.
///
/// Two things are deliberately not destroyed, because they are not only this reader's:
///
/// A recipe already <b>published</b> is part of the archive - other people have saved it and
/// cooked from it - so the recipe stays and the credit goes. That is what the privacy policy
/// promises, and it is why deletion is not simply a cascade.
///
/// A note written on <b>someone else's</b> family recipe stays with that family, with its
/// author anonymised. Deleting it would take something from people who did not ask for it.
///
/// Everything else goes: the profile, the saved lists, drafts still in review, family recipes
/// this reader preserved, their members, notes and invitations, every media blob they own,
/// their refresh tokens, and the account itself.
/// </summary>
public sealed class AccountDeletionService(
    TasteZambiaDbContext db,
    UserManager<ArchiveUser> users,
    IMediaStore media,
    ILogger<AccountDeletionService> log) : IAccountDeletionService
{
    /// <summary>What a withdrawn credit reads as. Not a blank, which looks like a bug.</summary>
    public const string AnonymousCredit = "A contributor who has since left the archive";

    public async Task<DeletionOutcome> DeleteAsync(string userId, string confirmDeviceId, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"No account {userId} to delete.");

        // Checked here rather than in the controller, because here is where the account's real
        // device id is already in hand - the token does not carry it.
        if (!string.Equals(user.UserName, confirmDeviceId, StringComparison.Ordinal))
            return new DeletionOutcome.ConfirmationMismatch();

        // One transaction: a half-deleted account is worse than a failed deletion, because
        // nobody can tell which half it is.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var published = await AnonymisePublishedAsync(userId, ct);
        var unpublished = await DeleteUnpublishedAsync(userId, ct);
        var (recipesDeleted, blobsFromRecipes) = await DeleteOwnedFamilyRecipesAsync(userId, ct);
        var left = await LeaveOtherFamilyRecipesAsync(userId, ct);
        var notes = await AnonymiseNotesElsewhereAsync(userId, ct);
        var blobs = blobsFromRecipes + await DeleteRemainingMediaAsync(userId, ct);

        await DeletePersonalAsync(userId, ct);

        await db.SaveChangesAsync(ct);

        // Last, and through the UserManager so Identity's own rows go with it.
        var removed = await users.DeleteAsync(user);
        if (!removed.Succeeded)
            throw new InvalidOperationException(
                "Could not delete the account: " + string.Join("; ", removed.Errors.Select(e => e.Description)));

        await transaction.CommitAsync(ct);

        log.LogInformation(
            "Deleted account {UserId}: {Unpublished} unpublished, {Published} credits withdrawn, "
            + "{Owned} family recipes, {Left} left, {Notes} notes anonymised, {Blobs} blobs",
            userId, unpublished, published, recipesDeleted, left, notes, blobs);

        return new DeletionOutcome.Done(
            new DeletionSummary(unpublished, published, recipesDeleted, left, notes, blobs));
    }

    /// <summary>
    /// The recipe stays in the archive; the name on it does not. Both the Contribution row and
    /// the published Dish and Recipe carry a snapshot of the credit, so all three are cleared -
    /// missing one would leave the reader's name on the screen that shows it.
    /// </summary>
    private async Task<int> AnonymisePublishedAsync(string userId, CancellationToken ct)
    {
        var published = await db.Contributions
            .Where(c => c.UserId == userId && c.Status == ContributionStatus.Published)
            .ToListAsync(ct);

        foreach (var contribution in published)
        {
            contribution.ContributorName = AnonymousCredit;
            contribution.ContributorLocation = "";

            if (contribution.PublishedDishId is not { } dishId) continue;

            // Only the Recipe carries the credit; the Dish row does not, so there is nothing
            // to clear there. Its Provenance stays Community - the recipe did come from a
            // contributor, and saying otherwise would falsify the archive's own record.
            var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.DishId == dishId, ct);
            if (recipe is not null)
            {
                recipe.ContributorName = AnonymousCredit;
                recipe.ContributorLocation = "";
                recipe.ContributorAvatarAsset = null;
            }
        }

        return published.Count;
    }

    private async Task<int> DeleteUnpublishedAsync(string userId, CancellationToken ct)
    {
        // Include the children: the cascade is configured, but loading them keeps this honest
        // if that ever changes, and the count is what the reader is told.
        var drafts = await db.Contributions
            .Include(c => c.Ingredients).Include(c => c.Steps)
            .Include(c => c.Events).Include(c => c.Flags)
            .Where(c => c.UserId == userId && c.Status != ContributionStatus.Published)
            .ToListAsync(ct);

        db.Contributions.RemoveRange(drafts);
        return drafts.Count;
    }

    private async Task<(int Recipes, int Blobs)> DeleteOwnedFamilyRecipesAsync(string userId, CancellationToken ct)
    {
        var owned = await db.FamilyRecipes
            .Include(r => r.Members).Include(r => r.Notes).Include(r => r.Media)
            .Where(r => r.OwnerId == userId)
            .ToListAsync(ct);

        // Invitations hang off the members, not off the recipe, so they are collected by id.
        var ownedIds = owned.Select(r => r.Id).ToList();
        db.FamilyInvites.RemoveRange(
            await db.FamilyInvites.Where(i => ownedIds.Contains(i.FamilyRecipeId)).ToListAsync(ct));

        var blobs = 0;
        foreach (var recipe in owned)
        {
            // Every photograph and recording attached to it, whoever added them: the recipe
            // they belong to is going, so a blob left behind is an orphan nobody can reach.
            var assets = await db.MediaAssets.Where(m => m.FamilyRecipeId == recipe.Id).ToListAsync(ct);
            blobs += await DeleteBlobsAsync(assets, ct);
        }

        db.FamilyRecipes.RemoveRange(owned);
        return (owned.Count, blobs);
    }

    /// <summary>
    /// Membership of other people's recipes ends - they lose access, exactly as they would if
    /// the owner had removed them, because the access check requires a live UserId match.
    ///
    /// The row is marked Removed rather than deleted. The name on it was typed by the owner,
    /// about their own family, and deleting it would take a piece of their record away; cutting
    /// the UserId is what severs it from the account that is going.
    /// </summary>
    private async Task<int> LeaveOtherFamilyRecipesAsync(string userId, CancellationToken ct)
    {
        var memberships = await db.FamilyMembers
            .Where(m => m.UserId == userId)
            .ToListAsync(ct);

        foreach (var membership in memberships)
        {
            membership.UserId = null;
            membership.State = MemberState.Removed;
        }

        return memberships.Count;
    }

    private async Task<int> AnonymiseNotesElsewhereAsync(string userId, CancellationToken ct)
    {
        // Only notes on recipes somebody else owns; the ones on their own recipes are being
        // deleted with them. The owned rows are removed but not yet saved, so the subquery
        // still sees them in the database and correctly excludes their notes.
        var ownIds = db.FamilyRecipes.Where(r => r.OwnerId == userId).Select(r => r.Id);

        var notes = await db.FamilyNotes
            .Where(n => n.AuthorUserId == userId && !ownIds.Contains(n.FamilyRecipeId))
            .ToListAsync(ct);

        foreach (var note in notes)
        {
            note.AuthorUserId = "";
            note.AuthorName = AnonymousCredit;
        }

        return notes.Count;
    }

    private async Task<int> DeleteRemainingMediaAsync(string userId, CancellationToken ct)
    {
        var assets = await db.MediaAssets.Where(m => m.UserId == userId).ToListAsync(ct);
        var deleted = await DeleteBlobsAsync(assets, ct);
        db.MediaAssets.RemoveRange(assets);
        return deleted;
    }

    private async Task<int> DeleteBlobsAsync(List<MediaAsset> assets, CancellationToken ct)
    {
        var deleted = 0;
        foreach (var asset in assets)
        {
            try
            {
                await media.DeleteAsync(asset.Id, asset.ContentType, ct);
                deleted++;
            }
            catch (IOException ex)
            {
                // The row goes either way. A blob we could not unlink is a file to sweep up,
                // not a reason to leave the reader's account standing.
                log.LogWarning(ex, "Could not delete blob {AssetId} while deleting an account", asset.Id);
            }
        }

        return deleted;
    }

    private async Task DeletePersonalAsync(string userId, CancellationToken ct)
    {
        db.UserProfiles.RemoveRange(await db.UserProfiles.Where(p => p.UserId == userId).ToListAsync(ct));
        db.OnboardingChoices.RemoveRange(await db.OnboardingChoices.Where(o => o.UserId == userId).ToListAsync(ct));
        db.SavedDishes.RemoveRange(await db.SavedDishes.Where(s => s.UserId == userId).ToListAsync(ct));
        db.CookProgress.RemoveRange(await db.CookProgress.Where(c => c.UserId == userId).ToListAsync(ct));
        db.RefreshTokens.RemoveRange(await db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(ct));
    }
}
