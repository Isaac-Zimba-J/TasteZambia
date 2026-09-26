using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Services;

public interface IContributionService
{
    ContributionDraft StartShareDraft();
    ContributionDraft StartFamilyDraft();
    IReadOnlyList<ReviewStage> ReviewPipeline { get; }

    /// <summary>The phone's drafts, most recently edited first.</summary>
    IReadOnlyList<RecipeDraft> Drafts { get; }
    LocalDraft? FindDraft(Guid id);
    LocalDraft SaveDraft(ContributionDraft draft, Guid? id = null);
    void DeleteDraft(Guid id);

    /// <summary>Sends the draft to the archive. On success the local draft is gone; the contribution is the record now.</summary>
    Task<Contribution> SubmitAsync(ContributionDraft draft, CancellationToken ct = default);
    Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default);
    Task<ContributionDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default);
    Task<ContributionDetailDto> ResubmitAsync(Guid id, IReadOnlyList<FlagAnswerDto> answers, CancellationToken ct = default);
    Task<ContributionDetailDto> WithdrawAsync(Guid id, CancellationToken ct = default);

    /// <summary>The four design stages of the Review screen, lit according to the contribution's history.</summary>
    IReadOnlyList<ReviewStep> TimelineFor(ContributionDetailDto detail);
    /// <summary>The reviewer's open questions, ready for answers.</summary>
    IReadOnlyList<FlaggedField> FlagsFor(ContributionDetailDto detail);
}

/// <summary>
/// Drafts are local (DraftStore); everything from submission onward is the account's.
/// The design copy for the walkthrough drafts and the pipeline stays seeded.
/// </summary>
public sealed class ContributionService(DraftStore drafts, HttpClient api) : IContributionService
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public IReadOnlyList<ReviewStage> ReviewPipeline => SeedData.ReviewPipeline;

    public IReadOnlyList<RecipeDraft> Drafts
        => drafts.All.Select(d => RecipeDraft.From(d, DateTimeOffset.UtcNow)).ToList();

    public LocalDraft? FindDraft(Guid id) => drafts.Find(id);

    public LocalDraft SaveDraft(ContributionDraft draft, Guid? id = null)
    {
        var existing = id is { } known ? drafts.Find(known) : null;
        if (existing is null) return drafts.Create(draft);
        existing.Draft = draft;
        drafts.Save(existing);
        return existing;
    }

    /// <summary>A blank draft. The contributor types everything; nothing is pre-filled.</summary>
    public ContributionDraft StartShareDraft() => new() { CreditTeacher = true };

    /// <summary>A blank family draft. Nothing here is anyone's real grandmother; the contributor types it all.</summary>
    public ContributionDraft StartFamilyDraft() => new()
    {
        AddToFoodStories = true,
        Privacy = PrivacyLevel.SharedWithFamily,
    };

    public void DeleteDraft(Guid id) => drafts.Remove(id);

    public async Task<Contribution> SubmitAsync(ContributionDraft draft, CancellationToken ct = default)
    {
        var request = new SubmitContributionRequest(
            draft.LocalName, draft.EnglishDescription, draft.Province, draft.MealType, draft.Language,
            draft.Ingredients.Select(i => new ContributionIngredientDto(i.IngredientKey, i.DisplayName, i.DisplaySubtitle, i.Quantity)).ToList(),
            draft.Steps, draft.Origin, draft.CulturalSignificance, draft.TraditionalMethod,
            draft.TaughtBy, draft.TaughtByOrigin, draft.CreditTeacher,
            // Sending the wizard's photo ids along is the other half of this fix (mobile side);
            // this compile fix only keeps the contract change from breaking the build.
            []);

        var response = await api.PostAsJsonAsync(ApiRoutes.Me.Contributions, request, ct);
        response.EnsureSuccessStatusCode();
        var detail = (await response.Content.ReadFromJsonAsync<ContributionDetailDto>(ct))!;

        foreach (var d in drafts.All.Where(d => ReferenceEquals(d.Draft, draft)).ToList())
            drafts.Remove(d.Id);

        return ToModel(detail.Id, detail.LocalName, detail.Province, detail.Status, detail.SubmittedAt, detail.Events, detail.PublishedDishId);
    }

    public async Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default)
    {
        var list = await api.GetFromJsonAsync<List<ContributionSummaryDto>>(ApiRoutes.Me.Contributions, ct) ?? [];
        return list.Select(s => ToModel(s.Id, s.LocalName, s.Province, s.Status, s.SubmittedAt, [], s.PublishedDishId, s.UpdatedAt)).ToList();
    }

    public async Task<ContributionDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var response = await api.GetAsync(ApiRoutes.Me.ContributionById.Replace("{id}", id.ToString()), ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContributionDetailDto>(ct);
    }

    public async Task<ContributionDetailDto> ResubmitAsync(Guid id, IReadOnlyList<FlagAnswerDto> answers, CancellationToken ct = default)
    {
        var response = await api.PostAsJsonAsync(ApiRoutes.Me.Resubmit.Replace("{id}", id.ToString()), new ResubmitRequest(answers), ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContributionDetailDto>(ct))!;
    }

    public async Task<ContributionDetailDto> WithdrawAsync(Guid id, CancellationToken ct = default)
    {
        var response = await api.PostAsync(ApiRoutes.Me.Withdraw.Replace("{id}", id.ToString()), null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContributionDetailDto>(ct))!;
    }

    public IReadOnlyList<ReviewStep> TimelineFor(ContributionDetailDto d)
    {
        var submitted = d.Events.FirstOrDefault(e => e.Kind == ReviewEventKind.Submitted);
        var read = d.Events.FirstOrDefault(e => e.Kind == ReviewEventKind.Read);
        var published = d.Events.FirstOrDefault(e => e.Kind == ReviewEventKind.Published);
        var withdrawn = d.Status == ContributionStatus.Withdrawn;
        var seeded = SeedData.SubmissionTimeline;

        ReviewStep Stage(int i, ReviewEventDto? at, bool inProgress, string? note = null)
        {
            var state = at is not null ? ReviewState.Done : inProgress ? ReviewState.InProgress : ReviewState.Pending;
            var when = at is not null ? Date(at.At) : withdrawn ? "Withdrawn" : inProgress ? "In progress" : "Pending";
            return new ReviewStep(seeded[i].Label, when, note ?? seeded[i].Note, state, i < 3);
        }

        return
        [
            Stage(0, submitted, false),
            Stage(1, read, !withdrawn && read is null, read is null ? null : $"Reviewed by {read.Actor}."),
            Stage(2, published, !withdrawn && read is not null && published is null),
            Stage(3, published, false),
        ];
    }

    public IReadOnlyList<FlaggedField> FlagsFor(ContributionDetailDto d)
        => d.Flags.Where(f => f.Answer is null)
            .Select(f => new FlaggedField(f.Field, f.Question, f.CurrentValue) { Id = f.Id })
            .ToList();

    private static Contribution ToModel(Guid id, string name, string province, ContributionStatus status, DateTimeOffset submitted,
        IReadOnlyList<ReviewEventDto> events, string? dishId, DateTimeOffset? updated = null)
    {
        var (phrase, at) = status switch
        {
            ContributionStatus.Published => ("published", updated ?? events.LastOrDefault(e => e.Kind == ReviewEventKind.Published)?.At ?? submitted),
            ContributionStatus.ChangesRequested => ("changes requested", updated ?? submitted),
            ContributionStatus.Withdrawn => ("withdrawn", updated ?? submitted),
            _ => ("submitted", submitted),
        };
        return new Contribution(id, name, status, $"{province} Province · {phrase} {Date(at)}", dishId);
    }

    private static string Date(DateTimeOffset at) => at.ToString("d MMM yyyy", Invariant);
}
