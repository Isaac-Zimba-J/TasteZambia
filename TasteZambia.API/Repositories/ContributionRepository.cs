using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Repositories;

public interface IContributionRepository
{
    /// <summary>Tracked, full graph. For the service's transitions.</summary>
    Task<Contribution?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Contribution?> GetForUserAsync(Guid id, string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Contribution>> ListForUserAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Contribution>> ListByStatusAsync(ContributionStatus status, CancellationToken ct = default);
    void Add(Contribution contribution);
    Task<bool> DishIdExistsAsync(string dishId, CancellationToken ct = default);
    void AddDish(Dish dish);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class ContributionRepository(TasteZambiaDbContext db) : IContributionRepository
{
    private IQueryable<Contribution> Graph => db.Contributions
        .Include(c => c.Ingredients.OrderBy(i => i.SortOrder))
        .Include(c => c.Steps.OrderBy(s => s.SortOrder))
        .Include(c => c.Events.OrderBy(e => e.At))
        .Include(c => c.Flags.OrderBy(f => f.Id));

    public Task<Contribution?> GetAsync(Guid id, CancellationToken ct = default)
        => Graph.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Contribution?> GetForUserAsync(Guid id, string userId, CancellationToken ct = default)
        => Graph.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

    public async Task<IReadOnlyList<Contribution>> ListForUserAsync(string userId, CancellationToken ct = default)
        => await Graph.AsNoTracking().Where(c => c.UserId == userId).OrderByDescending(c => c.SubmittedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Contribution>> ListByStatusAsync(ContributionStatus status, CancellationToken ct = default)
        => await Graph.AsNoTracking().Where(c => c.Status == status).OrderBy(c => c.SubmittedAt).ToListAsync(ct);

    public void Add(Contribution contribution) => db.Contributions.Add(contribution);
    public Task<bool> DishIdExistsAsync(string dishId, CancellationToken ct = default) => db.Dishes.AnyAsync(d => d.Id == dishId, ct);
    public void AddDish(Dish dish) => db.Dishes.Add(dish);
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
