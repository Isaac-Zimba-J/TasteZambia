using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Services;

namespace TasteZambia.API.Repositories;

public interface IFamilyRepository
{
    /// <summary>The recipe, or null when it does not exist or is not visible to this user.</summary>
    Task<FamilyRecipe?> GetAsync(Guid id, string userId, CancellationToken ct = default);

    /// <summary>The owner's own, and any they have joined.</summary>
    Task<IReadOnlyList<FamilyRecipe>> ListForUserAsync(string userId, CancellationToken ct = default);

    Task<FamilyInvite?> FindByInviteCodeAsync(string code, CancellationToken ct = default);
    void Add(FamilyRecipe recipe);
    void AddInvite(FamilyInvite invite);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class FamilyRepository(TasteZambiaDbContext db, IFamilyAccessService access) : IFamilyRepository
{
    private IQueryable<FamilyRecipe> Graph => db.Set<FamilyRecipe>()
        .Include(r => r.Members.OrderBy(m => m.InvitedAt))
        .Include(r => r.Notes.OrderByDescending(n => n.CreatedAt))
        .Include(r => r.Media.OrderBy(m => m.CreatedAt));

    public Task<FamilyRecipe?> GetAsync(Guid id, string userId, CancellationToken ct = default)
        => access.VisibleTo(Graph, userId).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<FamilyRecipe>> ListForUserAsync(string userId, CancellationToken ct = default)
        => await access.VisibleTo(Graph.AsNoTracking(), userId)
            // A stranger's public recipe is not "theirs"; the shelf shows what they keep.
            .Where(r => r.OwnerId == userId || r.Members.Any(m => m.UserId == userId))
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(ct);

    public Task<FamilyInvite?> FindByInviteCodeAsync(string code, CancellationToken ct = default)
        => db.Set<FamilyInvite>().FirstOrDefaultAsync(i => i.Code == code, ct);

    public void Add(FamilyRecipe recipe) => db.Set<FamilyRecipe>().Add(recipe);
    public void AddInvite(FamilyInvite invite) => db.Set<FamilyInvite>().Add(invite);
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
