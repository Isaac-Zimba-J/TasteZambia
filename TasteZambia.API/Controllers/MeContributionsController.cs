using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Auth;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

/// <summary>The signed-in user's own contributions. Everything here is scoped to that user; nothing leaks.</summary>
[ApiController]
[Authorize]
public sealed class MeContributionsController(
    ICurrentUser me,
    IContributionRepository contributions,
    IContributionService service) : ControllerBase
{
    private string UserId => me.UserId ?? throw new UnauthorizedAccessException();

    [HttpPost(ApiRoutes.Me.Contributions)]
    [ProducesResponseType<ContributionDetailDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Submit([FromBody] SubmitContributionRequest request, CancellationToken ct)
    {
        var c = await service.SubmitAsync(UserId, request, ct);
        return Created(ApiRoutes.Me.ContributionById.Replace("{id}", c.Id.ToString()), c.ToDetail());
    }

    [HttpGet(ApiRoutes.Me.Contributions)]
    [ProducesResponseType<IReadOnlyList<ContributionSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok((await contributions.ListForUserAsync(UserId, ct)).Select(c => c.ToSummary()).ToList());

    [HttpGet(ApiRoutes.Me.ContributionById)]
    [ProducesResponseType<ContributionDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var c = await contributions.GetForUserAsync(id, UserId, ct);
        return c is null ? this.NotFoundProblem("Contribution not found", $"No contribution {id}.") : Ok(c.ToDetail());
    }

    [HttpPost(ApiRoutes.Me.Resubmit)]
    [ProducesResponseType<ContributionDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Resubmit(Guid id, [FromBody] ResubmitRequest request, CancellationToken ct)
        => Transition(() => service.ResubmitAsync(id, UserId, request.Answers, ct));

    [HttpPost(ApiRoutes.Me.Withdraw)]
    [ProducesResponseType<ContributionDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Withdraw(Guid id, CancellationToken ct)
        => Transition(() => service.WithdrawAsync(id, UserId, ct));

    private async Task<IActionResult> Transition(Func<Task<Contribution>> action)
    {
        try
        {
            return Ok((await action()).ToDetail());
        }
        catch (KeyNotFoundException)
        {
            return this.NotFoundProblem("Contribution not found", "No such contribution.");
        }
        catch (InvalidContributionTransitionException ex)
        {
            return this.ConflictProblem(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Problem(title: "Incomplete", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
