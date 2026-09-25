using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Services;

public interface IFamilyService
{
    Task<FamilyRecipe> CreateAsync(string userId, CreateFamilyRecipeRequest request, CancellationToken ct);
    Task<FamilyRecipe?> UpdateAsync(Guid id, string userId, UpdateFamilyRecipeRequest request, CancellationToken ct);
    Task<FamilyInvite?> InviteAsync(Guid id, string userId, AddMemberRequest request, CancellationToken ct);

    /// <summary>Returns the recipe joined, null when the code is unknown, or throws when it is already used.</summary>
    Task<FamilyRecipe?> AcceptInviteAsync(string code, string userId, string displayName, CancellationToken ct);

    Task<bool> RemoveMemberAsync(Guid id, Guid memberId, string userId, CancellationToken ct);
    Task<FamilyNote?> AddNoteAsync(Guid id, string userId, string authorName, string body, CancellationToken ct);
    Task<bool> SetPrivacyAsync(Guid id, string userId, PrivacyLevel privacy, CancellationToken ct);
    Task<bool> AttachMediaAsync(Guid id, Guid mediaId, string userId, CancellationToken ct);

    static int PercentComplete(FamilyRecipe r)
    {
        // The design's draft checklist, four equal parts.
        var done = 0;
        if (r.LocalName.Length > 0 && r.Province.Length > 0) done++;
        if (r.TaughtBy.Length > 0) done++;
        if (r.Story.Length > 0 || r.TraditionalMethod.Length > 0) done++;
        if (r.Privacy != PrivacyLevel.PrivateToMe) done++;
        return done * 25;
    }
}

public sealed class FamilyService(
    TasteZambiaDbContext db,
    IFamilyRepository family,
    IFamilyAccessService access,
    IUserProfileRepository profiles,
    TimeProvider clock) : IFamilyService
{
    /// <summary>No 0/O/1/I/l: these codes get read aloud and written on paper.</summary>
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public async Task<FamilyRecipe> CreateAsync(string userId, CreateFamilyRecipeRequest r, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var profile = await profiles.GetOrCreateProfileAsync(userId, ct);

        var recipe = new FamilyRecipe
        {
            OwnerId = userId,
            LocalName = r.LocalName.Trim(),
            Description = r.Description,
            Province = r.Province,
            Language = r.Language,
            TaughtBy = r.TaughtBy,
            TaughtByOrigin = r.TaughtByOrigin,
            Story = r.Story,
            TraditionalMethod = r.TraditionalMethod,
            Privacy = r.Privacy,
            CreatedAt = now,
            UpdatedAt = now,
            Members =
            {
                new FamilyMember
                {
                    UserId = userId,
                    DisplayName = profile.DisplayName.Length > 0 ? profile.DisplayName : "You",
                    Relation = "Owner",
                    State = MemberState.Joined,
                    InvitedAt = now,
                    JoinedAt = now,
                },
            },
        };

        family.Add(recipe);
        await family.SaveChangesAsync(ct);
        return recipe;
    }

    public async Task<FamilyRecipe?> UpdateAsync(Guid id, string userId, UpdateFamilyRecipeRequest r, CancellationToken ct)
    {
        var recipe = await OwnedAsync(id, userId, ct);
        if (recipe is null) return null;

        recipe.LocalName = r.LocalName.Trim();
        recipe.Description = r.Description;
        recipe.Province = r.Province;
        recipe.Language = r.Language;
        recipe.TaughtBy = r.TaughtBy;
        recipe.TaughtByOrigin = r.TaughtByOrigin;
        recipe.Story = r.Story;
        recipe.TraditionalMethod = r.TraditionalMethod;
        recipe.UpdatedAt = clock.GetUtcNow();
        await family.SaveChangesAsync(ct);
        return recipe;
    }

    public async Task<FamilyInvite?> InviteAsync(Guid id, string userId, AddMemberRequest request, CancellationToken ct)
    {
        var recipe = await OwnedAsync(id, userId, ct);
        if (recipe is null) return null;

        var now = clock.GetUtcNow();
        var member = new FamilyMember
        {
            DisplayName = request.DisplayName.Trim(),
            Relation = request.Relation,
            State = MemberState.Invited,
            InvitedAt = now,
        };
        recipe.Members.Add(member);
        // FamilyMember's key is a client-set Guid, not store-generated: reached only
        // through the collection on an already-tracked recipe, EF's change detection
        // reads that non-default key as "existing" and emits an UPDATE instead of an
        // INSERT. Adding it to the set explicitly forces the correct Added state.
        db.Set<FamilyMember>().Add(member);
        recipe.UpdatedAt = now;

        var invite = new FamilyInvite
        {
            FamilyRecipeId = recipe.Id,
            MemberId = member.Id,
            Code = NewCode(),
            ExpiresAt = now.AddDays(30),
        };
        family.AddInvite(invite);
        await family.SaveChangesAsync(ct);
        return invite;
    }

    public async Task<FamilyRecipe?> AcceptInviteAsync(string code, string userId, string displayName, CancellationToken ct)
    {
        var invite = await family.FindByInviteCodeAsync(code.ToUpperInvariant(), ct);
        if (invite is null) return null;

        var now = clock.GetUtcNow();
        if (!invite.IsOpen(now))
            throw new InvalidOperationException("That invite has already been used or has expired.");

        var member = await db.Set<FamilyMember>().FirstOrDefaultAsync(m => m.Id == invite.MemberId, ct);
        if (member is null) return null;

        member.UserId = userId;
        member.State = MemberState.Joined;
        member.JoinedAt = now;
        if (displayName.Length > 0) member.DisplayName = displayName;
        invite.RedeemedAt = now;

        try
        {
            await family.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Two people redeemed the same code at once: the loser's xmin no longer
            // matches (the winner already saved), which is exactly "already used".
            throw new InvalidOperationException("That invite has already been used or has expired.");
        }
        return await family.GetAsync(invite.FamilyRecipeId, userId, ct);
    }

    public async Task<bool> RemoveMemberAsync(Guid id, Guid memberId, string userId, CancellationToken ct)
    {
        var recipe = await OwnedAsync(id, userId, ct);
        var member = recipe?.Members.FirstOrDefault(m => m.Id == memberId);
        if (recipe is null || member is null || member.UserId == recipe.OwnerId) return false;

        // Soft: the note they left stays, and their name with it.
        member.State = MemberState.Removed;
        recipe.UpdatedAt = clock.GetUtcNow();
        await family.SaveChangesAsync(ct);
        return true;
    }

    public async Task<FamilyNote?> AddNoteAsync(Guid id, string userId, string authorName, string body, CancellationToken ct)
    {
        var recipe = await family.GetAsync(id, userId, ct);
        if (recipe is null) return null;

        var note = new FamilyNote
        {
            FamilyRecipeId = recipe.Id,
            AuthorUserId = userId,
            AuthorName = authorName,
            Body = body.Trim(),
            CreatedAt = clock.GetUtcNow(),
        };
        recipe.Notes.Add(note);
        db.Set<FamilyNote>().Add(note);   // same client-key-Guid reason as the member above
        recipe.UpdatedAt = note.CreatedAt;
        await family.SaveChangesAsync(ct);
        return note;
    }

    public async Task<bool> SetPrivacyAsync(Guid id, string userId, PrivacyLevel privacy, CancellationToken ct)
    {
        var recipe = await OwnedAsync(id, userId, ct);
        if (recipe is null) return false;

        recipe.Privacy = privacy;
        recipe.UpdatedAt = clock.GetUtcNow();
        await family.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AttachMediaAsync(Guid id, Guid mediaId, string userId, CancellationToken ct)
    {
        var recipe = await family.GetAsync(id, userId, ct);
        if (recipe is null) return false;

        // Members contribute photographs; only your own upload can be attached.
        var asset = await db.MediaAssets.FirstOrDefaultAsync(a => a.Id == mediaId && a.UserId == userId, ct);
        if (asset is null) return false;

        asset.FamilyRecipeId = recipe.Id;
        recipe.UpdatedAt = clock.GetUtcNow();
        await family.SaveChangesAsync(ct);
        return true;
    }

    private async Task<FamilyRecipe?> OwnedAsync(Guid id, string userId, CancellationToken ct)
    {
        var recipe = await family.GetAsync(id, userId, ct);
        return recipe is not null && await access.CanEditAsync(recipe, userId, ct) ? recipe : null;
    }

    private static string NewCode()
        => new(RandomNumberGenerator.GetItems<char>(CodeAlphabet, 8));
}
