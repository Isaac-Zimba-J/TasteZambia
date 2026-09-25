using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Contracts.Contributions;

namespace TasteZambia.Core.Tests.Fakes;

/// <summary>
/// Scripted contribution details for ViewModel tests. Mapping (TimelineFor/FlagsFor)
/// delegates to the real service so it stays under test in one place.
/// </summary>
public class FakeContributionService : IContributionService
{
    private readonly ContributionService _real = TestServices.Contributions();
    private readonly Dictionary<Guid, ContributionDetailDto> _details = [];

    public List<Guid> Withdrawn { get; } = [];
    public Dictionary<Guid, IReadOnlyList<FlagAnswerDto>> Resubmitted { get; } = [];

    public Guid Add(ContributionDetailDto detail)
    {
        _details[detail.Id] = detail;
        return detail.Id;
    }

    public ContributionDraft StartShareDraft() => _real.StartShareDraft();
    public ContributionDraft StartFamilyDraft() => _real.StartFamilyDraft();
    public IReadOnlyList<ReviewStage> ReviewPipeline => _real.ReviewPipeline;
    public IReadOnlyList<RecipeDraft> Drafts => _real.Drafts;
    public LocalDraft? FindDraft(Guid id) => _real.FindDraft(id);
    public LocalDraft SaveDraft(ContributionDraft draft, Guid? id = null) => _real.SaveDraft(draft, id);
    public void DeleteDraft(Guid id) => _real.DeleteDraft(id);

    public List<ContributionDraft> Submitted { get; } = [];

    /// <summary>Accepts the draft as a server would: the local copy is dropped and an InReview contribution comes back.</summary>
    public Task<Contribution> SubmitAsync(ContributionDraft draft, CancellationToken ct = default)
    {
        Submitted.Add(draft);
        var id = Guid.NewGuid();
        return Task.FromResult(new Contribution(id, draft.LocalName, Shared.Enums.ContributionStatus.InReview,
            $"{draft.Province} Province · submitted just now"));
    }

    public Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Contribution>>(
            _details.Values.Select(d => new Contribution(d.Id, d.LocalName, d.Status, "", d.PublishedDishId)).ToList());

    public Task<ContributionDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_details.GetValueOrDefault(id));

    public Task<ContributionDetailDto> ResubmitAsync(Guid id, IReadOnlyList<FlagAnswerDto> answers, CancellationToken ct = default)
    {
        Resubmitted[id] = answers;
        return Task.FromResult(_details[id]);
    }

    public virtual Task<ContributionDetailDto> WithdrawAsync(Guid id, CancellationToken ct = default)
    {
        Withdrawn.Add(id);
        return Task.FromResult(_details[id]);
    }

    public IReadOnlyList<ReviewStep> TimelineFor(ContributionDetailDto detail) => _real.TimelineFor(detail);
    public IReadOnlyList<FlaggedField> FlagsFor(ContributionDetailDto detail) => _real.FlagsFor(detail);
}
