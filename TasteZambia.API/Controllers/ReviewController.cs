using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Auth;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Contracts.Review;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

/// <summary>The archive team's side of review. Every route needs the reviewer role.</summary>
[ApiController]
[Authorize(Roles = "reviewer")]
public sealed class ReviewController(
    ICurrentUser me,
    IContributionRepository contributions,
    IContributionService service,
    IUserProfileRepository profiles) : ControllerBase
{
    private const string DefaultReviewer = "The archive team";

    [HttpGet(ApiRoutes.Review.Queue)]
    [ProducesResponseType<IReadOnlyList<ReviewQueueItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Queue(CancellationToken ct)
        => Ok((await contributions.ListByStatusAsync(ContributionStatus.InReview, ct)).Select(c => c.ToQueueItem()).ToList());

    [HttpPost(ApiRoutes.Review.RequestChanges)]
    [ProducesResponseType<ContributionDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestChanges(Guid id, [FromBody] RequestChangesRequest request, CancellationToken ct)
    {
        var reviewer = await ReviewerNameAsync(ct);
        try
        {
            // The first reviewer action is when the submission was "read by the archive team".
            await service.MarkReadAsync(id, reviewer, ct);
            return Ok((await service.RequestChangesAsync(id, reviewer, request.Note, request.Flags, ct)).ToDetail());
        }
        catch (KeyNotFoundException)
        {
            return this.NotFoundProblem("Contribution not found", $"No contribution {id}.");
        }
        catch (InvalidContributionTransitionException ex)
        {
            return this.ConflictProblem(ex.Message);
        }
    }

    [HttpPost(ApiRoutes.Review.Publish)]
    [ProducesResponseType<PublishResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var reviewer = await ReviewerNameAsync(ct);
        try
        {
            await service.MarkReadAsync(id, reviewer, ct);
            var c = await service.PublishAsync(id, reviewer, ct);
            return Ok(new PublishResultDto(c.Id, c.PublishedDishId!));
        }
        catch (KeyNotFoundException)
        {
            return this.NotFoundProblem("Contribution not found", $"No contribution {id}.");
        }
        catch (InvalidContributionTransitionException ex)
        {
            return this.ConflictProblem(ex.Message);
        }
    }

    private async Task<string> ReviewerNameAsync(CancellationToken ct)
    {
        var p = await profiles.GetOrCreateProfileAsync(me.UserId!, ct);
        return p.DisplayName.Length > 0 ? p.DisplayName : DefaultReviewer;
    }
}
