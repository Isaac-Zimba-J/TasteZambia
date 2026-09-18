using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Contracts.Review;

namespace TasteZambia.API.Mapping;

public static class ContributionMappings
{
    public static ContributionSummaryDto ToSummary(this Contribution c)
        => new(c.Id, c.LocalName, c.Province, c.Status, c.SubmittedAt, c.UpdatedAt, c.PublishedDishId);

    public static ContributionDetailDto ToDetail(this Contribution c)
        => new(c.Id, c.LocalName, c.EnglishDescription, c.Province, c.Status, c.SubmittedAt, c.PublishedDishId,
            c.ContributorName, c.ContributorLocation, c.TaughtBy, c.TaughtByOrigin, c.CreditTeacher,
            c.Events.OrderBy(e => e.At).Select(e => new ReviewEventDto(e.Kind, e.At, e.Actor, e.Note)).ToList(),
            c.Flags.OrderBy(f => f.Id).Select(f => new FlaggedFieldDto(f.Id, f.Field, f.Question, f.CurrentValue, f.Answer)).ToList());

    public static ReviewQueueItemDto ToQueueItem(this Contribution c)
        => new(c.Id, c.LocalName, c.Province, c.ContributorName, c.Status, c.SubmittedAt, c.Flags.Count(f => f.Answer is null));
}
