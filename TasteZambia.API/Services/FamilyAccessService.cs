using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Services;

public interface IFamilyAccessService
{
    /// <summary>
    /// The rule, in one place: a family recipe is readable by its owner, by members who
    /// have joined, and by everyone once it is public.
    /// </summary>
    IQueryable<FamilyRecipe> VisibleTo(IQueryable<FamilyRecipe> source, string userId);

    Task<bool> CanReadAsync(FamilyRecipe recipe, string userId, CancellationToken ct);

    /// <summary>Only the owner changes a family recipe. Members add notes; they do not edit.</summary>
    Task<bool> CanEditAsync(FamilyRecipe recipe, string userId, CancellationToken ct);

    /// <summary>
    /// A third thing, alongside reading and editing: may this person add to the recipe
    /// (attach media, leave a note)? Readable-because-public is not the same as
    /// belonging to the family - a stranger who can see a published recipe still may
    /// not write into its archive.
    /// </summary>
    Task<bool> CanContributeAsync(FamilyRecipe recipe, string userId, CancellationToken ct);

    Task<bool> CanReadMediaAsync(MediaAsset asset, string userId, CancellationToken ct);
}

public sealed class FamilyAccessService(TasteZambiaDbContext db) : IFamilyAccessService
{
    public IQueryable<FamilyRecipe> VisibleTo(IQueryable<FamilyRecipe> source, string userId)
        => source.Where(r =>
            r.OwnerId == userId
            || r.Privacy == PrivacyLevel.PublicInArchive
            || r.Members.Any(m => m.UserId == userId && m.State == MemberState.Joined));

    public Task<bool> CanReadAsync(FamilyRecipe recipe, string userId, CancellationToken ct)
        => Task.FromResult(
            recipe.OwnerId == userId
            || recipe.Privacy == PrivacyLevel.PublicInArchive
            || recipe.Members.Any(m => m.UserId == userId && m.State == MemberState.Joined));

    public Task<bool> CanEditAsync(FamilyRecipe recipe, string userId, CancellationToken ct)
        => Task.FromResult(recipe.OwnerId == userId);

    public Task<bool> CanContributeAsync(FamilyRecipe recipe, string userId, CancellationToken ct)
        => Task.FromResult(
            recipe.OwnerId == userId
            || recipe.Members.Any(m => m.UserId == userId && m.State == MemberState.Joined));

    public async Task<bool> CanReadMediaAsync(MediaAsset asset, string userId, CancellationToken ct)
    {
        if (asset.UserId == userId) return true;

        // A photo attached to a family recipe is readable by whoever may read the recipe.
        if (asset.FamilyRecipeId is not { } recipeId) return false;

        return await VisibleTo(db.Set<FamilyRecipe>().Include(r => r.Members), userId)
            .AnyAsync(r => r.Id == recipeId, ct);
    }
}
