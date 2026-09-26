using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Contracts.Review;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Services;

public interface IContributionService
{
    Task<Contribution> SubmitAsync(string userId, SubmitContributionRequest request, CancellationToken ct);
    Task<Contribution> RequestChangesAsync(Guid id, string reviewerName, string note, IReadOnlyList<FlagRequestDto> flags, CancellationToken ct);
    Task<Contribution> ResubmitAsync(Guid id, string userId, IReadOnlyList<FlagAnswerDto> answers, CancellationToken ct);
    Task<Contribution> PublishAsync(Guid id, string reviewerName, CancellationToken ct);
    Task<Contribution> WithdrawAsync(Guid id, string userId, CancellationToken ct);
    Task MarkReadAsync(Guid id, string reviewerName, CancellationToken ct);
}

/// <summary>
/// The one place a contribution's Status changes. Contributor and reviewer controllers
/// both come through here, so neither can skip a step. Publishing writes the dish into
/// the archive itself: a published recipe IS an archive entry, not a copy of one.
/// </summary>
public sealed partial class ContributionService(
    IContributionRepository contributions,
    IUserProfileRepository profiles,
    TasteZambiaDbContext db,
    TimeProvider clock) : IContributionService
{
    public const string AnonymousContributor = "A Taste Zambia contributor";

    public async Task<Contribution> SubmitAsync(string userId, SubmitContributionRequest r, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var profile = await profiles.GetOrCreateProfileAsync(userId, ct);

        var c = new Contribution
        {
            UserId = userId,
            Status = ContributionStatus.InReview,
            LocalName = r.LocalName.Trim(),
            EnglishDescription = r.EnglishDescription.Trim(),
            Province = r.Province,
            MealType = r.MealType,
            Language = r.Language,
            Origin = r.Origin,
            CulturalSignificance = r.CulturalSignificance,
            TraditionalMethod = r.TraditionalMethod,
            TaughtBy = r.TaughtBy,
            TaughtByOrigin = r.TaughtByOrigin,
            CreditTeacher = r.CreditTeacher,
            ContributorName = profile.DisplayName.Length > 0 ? profile.DisplayName : AnonymousContributor,
            ContributorLocation = profile.Location,
            SubmittedAt = now,
            UpdatedAt = now,
            Ingredients = r.Ingredients.Select((i, n) => new ContributionIngredient
            {
                SortOrder = n,
                IngredientKey = i.IngredientKey,
                DisplayName = i.DisplayName,
                DisplaySubtitle = i.DisplaySubtitle,
                Quantity = i.Quantity,
            }).ToList(),
            Steps = r.Steps.Select((s, n) => new ContributionStep { SortOrder = n, Text = s }).ToList(),
            Events = { new ReviewEvent { Kind = ReviewEventKind.Submitted, At = now } },
        };

        contributions.Add(c);

        if (r.PhotoIds.Count > 0)
        {
            // Only the submitter's own uploads attach; an id that is not theirs (typo'd,
            // guessed, or someone else's) is quietly skipped rather than failing a
            // submission that is otherwise a person's real work.
            var photos = await db.MediaAssets
                .Where(a => r.PhotoIds.Contains(a.Id) && a.UserId == userId)
                .ToListAsync(ct);
            foreach (var photo in photos) photo.ContributionId = c.Id;
        }

        await contributions.SaveChangesAsync(ct);
        return c;
    }

    public async Task<Contribution> RequestChangesAsync(Guid id, string reviewerName, string note, IReadOnlyList<FlagRequestDto> flags, CancellationToken ct)
    {
        var c = await RequireAsync(id, ct);
        Require(c, ContributionStatus.InReview, "request changes on");

        var now = clock.GetUtcNow();
        c.Status = ContributionStatus.ChangesRequested;
        c.UpdatedAt = now;
        c.Events.Add(new ReviewEvent { Kind = ReviewEventKind.ChangesRequested, At = now, Actor = reviewerName, Note = note });
        foreach (var f in flags)
            c.Flags.Add(new FlaggedField { Field = f.Field, Question = f.Question, CurrentValue = f.CurrentValue });

        await contributions.SaveChangesAsync(ct);
        return c;
    }

    public async Task<Contribution> ResubmitAsync(Guid id, string userId, IReadOnlyList<FlagAnswerDto> answers, CancellationToken ct)
    {
        var c = await contributions.GetForUserAsync(id, userId, ct) ?? throw new KeyNotFoundException();
        Require(c, ContributionStatus.ChangesRequested, "resubmit");

        var now = clock.GetUtcNow();
        foreach (var flag in c.Flags.Where(f => f.Answer is null).ToList())
        {
            var answer = answers.FirstOrDefault(a => a.FlagId == flag.Id)
                ?? throw new ArgumentException($"Flag {flag.Id} ({flag.Field}) has no answer.");
            flag.Answer = answer.Answer.Trim();
            flag.AnsweredAt = now;
        }

        c.Status = ContributionStatus.InReview;
        c.UpdatedAt = now;
        c.Events.Add(new ReviewEvent { Kind = ReviewEventKind.Resubmitted, At = now });
        await contributions.SaveChangesAsync(ct);
        return c;
    }

    public async Task<Contribution> PublishAsync(Guid id, string reviewerName, CancellationToken ct)
    {
        var c = await RequireAsync(id, ct);
        Require(c, ContributionStatus.InReview, "publish");

        var now = clock.GetUtcNow();
        var dishId = await UniqueSlugAsync(c.LocalName, ct);
        contributions.AddDish(BuildDish(c, dishId, now));

        c.Status = ContributionStatus.Published;
        c.PublishedDishId = dishId;
        c.UpdatedAt = now;
        c.Events.Add(new ReviewEvent { Kind = ReviewEventKind.Published, At = now, Actor = reviewerName });
        await contributions.SaveChangesAsync(ct);
        return c;
    }

    public async Task<Contribution> WithdrawAsync(Guid id, string userId, CancellationToken ct)
    {
        var c = await contributions.GetForUserAsync(id, userId, ct) ?? throw new KeyNotFoundException();
        if (c.Status is not (ContributionStatus.InReview or ContributionStatus.ChangesRequested))
            throw new InvalidContributionTransitionException(c.Status, "withdraw");

        var now = clock.GetUtcNow();
        c.Status = ContributionStatus.Withdrawn;
        c.UpdatedAt = now;
        c.Events.Add(new ReviewEvent { Kind = ReviewEventKind.Withdrawn, At = now });
        await contributions.SaveChangesAsync(ct);
        return c;
    }

    public async Task MarkReadAsync(Guid id, string reviewerName, CancellationToken ct)
    {
        var c = await RequireAsync(id, ct);
        if (c.Events.Any(e => e.Kind == ReviewEventKind.Read)) return;
        c.Events.Add(new ReviewEvent { Kind = ReviewEventKind.Read, At = clock.GetUtcNow(), Actor = reviewerName });
        await contributions.SaveChangesAsync(ct);
    }

    private async Task<Contribution> RequireAsync(Guid id, CancellationToken ct)
        => await contributions.GetAsync(id, ct) ?? throw new KeyNotFoundException();

    private static void Require(Contribution c, ContributionStatus expected, string action)
    {
        if (c.Status != expected) throw new InvalidContributionTransitionException(c.Status, action);
    }

    // ---- Publishing: the contribution becomes an archive dish ----

    private async Task<string> UniqueSlugAsync(string name, CancellationToken ct)
    {
        var slug = Slug(name);
        if (!await contributions.DishIdExistsAsync(slug, ct)) return slug;
        for (var n = 2; ; n++)
        {
            var candidate = $"{slug}-{n}";
            if (!await contributions.DishIdExistsAsync(candidate, ct)) return candidate;
        }
    }

    private static string Slug(string name)
    {
        var lower = name.Normalize(NormalizationForm.FormD).ToLowerInvariant();
        var ascii = new string(lower.Where(ch => ch is (>= 'a' and <= 'z') or (>= '0' and <= '9') or ' ' or '-').ToArray());
        var collapsed = SlugSeparators().Replace(ascii.Trim(), "-");
        return collapsed.Length > 0 ? collapsed[..Math.Min(collapsed.Length, 64)] : "dish";
    }

    [GeneratedRegex("[ -]+")]
    private static partial Regex SlugSeparators();

    private static Dish BuildDish(Contribution c, string dishId, DateTimeOffset now)
    {
        var credit = c.CreditTeacher && c.TaughtBy.Length > 0
            ? $"As taught by {c.TaughtBy}{(c.TaughtByOrigin.Length > 0 ? $" of {c.TaughtByOrigin}" : "")}."
            : "";

        return new Dish
        {
            Id = dishId,
            LocalName = c.LocalName,
            EnglishName = c.EnglishDescription,
            Region = c.Province,
            TimeLabel = "Community recipe",
            Difficulty = "As cooked at home",
            Description = c.CulturalSignificance.Length > 0 ? c.CulturalSignificance : c.Origin,
            SortOrder = int.MaxValue,           // after the editorial archive
            Provenance = Provenance.Community,
            ContributionId = c.Id,
            UpdatedAt = now,
            Recipe = new Recipe
            {
                DishId = dishId,
                Subtitle = c.EnglishDescription,
                IsVerified = true,              // it has been through review
                ContributorName = c.ContributorName,
                ContributorLocation = c.ContributorLocation,
                UpdatedAt = now,
                CulturalContext = new[] { c.Origin, c.CulturalSignificance, credit }
                    .Where(t => t.Length > 0)
                    .Select((t, n) => new RecipeParagraph { SortOrder = n, Text = t }).ToList(),
                Ingredients = c.Ingredients.Select(i => new RecipeIngredient
                {
                    SortOrder = i.SortOrder,
                    IngredientKey = i.IngredientKey,
                    DisplayName = i.DisplayName,
                    DisplaySubtitle = i.DisplaySubtitle,
                    Quantity = i.Quantity,
                }).ToList(),
                Steps = c.Steps.Select(s => new CookingStep { Number = s.SortOrder + 1, Title = $"Step {s.SortOrder + 1}", Body = s.Text }).ToList(),
                Methods = c.TraditionalMethod.Length > 0
                    ?
                    [
                        new MethodNarrative
                        {
                            Kind = MethodKind.Traditional,
                            Heading = "The traditional way",
                            Paragraphs = { new MethodParagraph { SortOrder = 0, Text = c.TraditionalMethod } },
                        },
                    ]
                    : [],
            },
        };
    }
}
