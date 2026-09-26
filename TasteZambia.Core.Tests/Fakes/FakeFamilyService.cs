using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.Fakes;

/// <summary>An in-memory account of family recipes, standing in for the archive API in ViewModel tests.</summary>
public sealed class FakeFamilyService : IFamilyArchiveService
{
    private readonly Dictionary<Guid, FamilyRecipeDto> _recipes = [];

    /// <summary>Seeds a recipe as though it already existed on the account. Returns its id.</summary>
    public Guid Add(FamilyRecipeDto dto)
    {
        _recipes[dto.Id] = dto;
        return dto.Id;
    }

    public PrivacyLevel PrivacyOf(Guid id) => _recipes[id].Privacy;

    public Task<IReadOnlyList<PreservedRecipe>> GetShelfAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PreservedRecipe>>(_recipes.Values.Select(ToSummary).Select(PreservedRecipe.From).ToList());

    public Task<FamilyRecipeDto?> GetAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_recipes.GetValueOrDefault(id));

    public Task<FamilyRecipeDto> PreserveAsync(ContributionDraft draft, CancellationToken ct = default)
    {
        var dto = new FamilyRecipeDto(Guid.NewGuid(), draft.LocalName, draft.EnglishDescription, draft.Province, draft.Language,
            draft.TaughtBy, draft.TaughtByOrigin, draft.Story, draft.TraditionalMethod, draft.Privacy,
            TranscriptState.None, null, PercentComplete(draft), true, DateTimeOffset.UtcNow, [], [], []);
        _recipes[dto.Id] = dto;
        return Task.FromResult(dto);
    }

    public Task<InviteDto?> InviteAsync(Guid id, string displayName, string relation, CancellationToken ct = default)
    {
        var dto = _recipes[id];
        var memberId = Guid.NewGuid();
        var members = dto.Members.Append(new FamilyMemberDto(memberId, displayName, relation, MemberState.Invited)).ToList();
        _recipes[id] = dto with { Members = members };
        return Task.FromResult<InviteDto?>(new InviteDto(memberId, "ABCD1234", DateTimeOffset.UtcNow.AddDays(7)));
    }

    public Task<FamilyRecipeDto?> AcceptInviteAsync(string code, CancellationToken ct = default)
        => Task.FromResult<FamilyRecipeDto?>(null);

    public Task RemoveMemberAsync(Guid id, Guid memberId, CancellationToken ct = default)
    {
        var dto = _recipes[id];
        _recipes[id] = dto with { Members = dto.Members.Where(m => m.Id != memberId).ToList() };
        return Task.CompletedTask;
    }

    public Task AddNoteAsync(Guid id, string body, CancellationToken ct = default)
    {
        var dto = _recipes[id];
        _recipes[id] = dto with { Notes = dto.Notes.Append(new FamilyNoteDto(Guid.NewGuid(), "You", body, DateTimeOffset.UtcNow)).ToList() };
        return Task.CompletedTask;
    }

    public Task SetPrivacyAsync(Guid id, PrivacyLevel privacy, CancellationToken ct = default)
    {
        _recipes[id] = _recipes[id] with { Privacy = privacy };
        return Task.CompletedTask;
    }

    public Task AttachMediaAsync(Guid id, Guid mediaId, CancellationToken ct = default) => Task.CompletedTask;

    private static int PercentComplete(ContributionDraft draft)
        => new[] { draft.LocalName, draft.Province, draft.TaughtBy, draft.Story, draft.TraditionalMethod }
            .Count(f => f.Length > 0) * 100 / 5;

    private static FamilyRecipeSummaryDto ToSummary(FamilyRecipeDto d) => new(
        d.Id, d.LocalName, d.TaughtBy, d.Privacy,
        d.Media.Count(m => m.Kind == MediaKind.Photo),
        d.Notes.Count,
        d.Media.Any(m => m.Kind == MediaKind.Audio),
        d.PercentComplete, d.UpdatedAt);
}
