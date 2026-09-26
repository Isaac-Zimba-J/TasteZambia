using TasteZambia.Core.Data.Http;
using TasteZambia.Core.Models;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Services;

public interface IFamilyArchiveService
{
    Task<IReadOnlyList<PreservedRecipe>> GetShelfAsync(CancellationToken ct = default);
    Task<FamilyRecipeDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<FamilyRecipeDto> PreserveAsync(ContributionDraft draft, CancellationToken ct = default);
    Task<InviteDto?> InviteAsync(Guid id, string displayName, string relation, CancellationToken ct = default);
    Task<FamilyRecipeDto?> AcceptInviteAsync(string code, CancellationToken ct = default);
    /// <summary>False when the API refused it as not the caller's to change - a real answer, not a network failure.</summary>
    Task<bool> RemoveMemberAsync(Guid id, Guid memberId, CancellationToken ct = default);
    Task AddNoteAsync(Guid id, string body, CancellationToken ct = default);
    /// <summary>False when the API refused it as not the caller's to change - a real answer, not a network failure.</summary>
    Task<bool> SetPrivacyAsync(Guid id, PrivacyLevel privacy, CancellationToken ct = default);
    /// <summary>False when the recipe or the upload could not be found for this account.</summary>
    Task<bool> AttachMediaAsync(Guid id, Guid mediaId, CancellationToken ct = default);
}

/// <summary>
/// A thin client over the account's family recipes. <see cref="HttpFamilyRepository"/> carries
/// the wire calls; this maps the shelf listing to <see cref="PreservedRecipe"/> and passes the
/// rest through as the DTOs the five screens already bind to.
/// </summary>
public sealed class FamilyArchiveService(HttpFamilyRepository repository) : IFamilyArchiveService
{
    public async Task<IReadOnlyList<PreservedRecipe>> GetShelfAsync(CancellationToken ct = default)
        => (await repository.GetShelfAsync(ct)).Select(PreservedRecipe.From).ToList();

    public Task<FamilyRecipeDto?> GetAsync(Guid id, CancellationToken ct = default)
        => repository.GetAsync(id, ct);

    public Task<FamilyRecipeDto> PreserveAsync(ContributionDraft draft, CancellationToken ct = default)
        => repository.CreateAsync(new CreateFamilyRecipeRequest(
            draft.LocalName, draft.EnglishDescription, draft.Province, draft.Language,
            draft.TaughtBy, draft.TaughtByOrigin, draft.Story, draft.TraditionalMethod, draft.Privacy), ct);

    public Task<InviteDto?> InviteAsync(Guid id, string displayName, string relation, CancellationToken ct = default)
        => repository.InviteAsync(id, new AddMemberRequest(displayName, relation), ct);

    public Task<FamilyRecipeDto?> AcceptInviteAsync(string code, CancellationToken ct = default)
        => repository.AcceptAsync(new AcceptInviteRequest(code), ct);

    public Task<bool> RemoveMemberAsync(Guid id, Guid memberId, CancellationToken ct = default)
        => repository.RemoveMemberAsync(id, memberId, ct);

    public Task AddNoteAsync(Guid id, string body, CancellationToken ct = default)
        => repository.AddNoteAsync(id, new AddNoteRequest(body), ct);

    public Task<bool> SetPrivacyAsync(Guid id, PrivacyLevel privacy, CancellationToken ct = default)
        => repository.SetPrivacyAsync(id, new SetPrivacyRequest(privacy), ct);

    public Task<bool> AttachMediaAsync(Guid id, Guid mediaId, CancellationToken ct = default)
        => repository.AttachMediaAsync(id, mediaId, ct);
}
