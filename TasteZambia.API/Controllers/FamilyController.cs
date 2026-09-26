using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Auth;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

/// <summary>
/// The family archive tier: preserving a recipe for relatives, inviting them by a
/// shared code, notes and privacy. Reads go straight to the repository (it already
/// applies the one visibility rule); writes go through the service, which is where
/// the owner-only checks live.
/// </summary>
[ApiController]
[Authorize]
public sealed class FamilyController(
    ICurrentUser me,
    IFamilyRepository family,
    IFamilyService service,
    IUserProfileRepository profiles) : ControllerBase
{
    private string UserId => me.UserId ?? throw new UnauthorizedAccessException();

    [HttpPost(ApiRoutes.Family.Collection)]
    [ProducesResponseType<FamilyRecipeDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateFamilyRecipeRequest request, CancellationToken ct)
    {
        var recipe = await service.CreateAsync(UserId, request, ct);
        return Created(ApiRoutes.Family.ById.Replace("{id}", recipe.Id.ToString()), recipe.ToDto(UserId));
    }

    [HttpGet(ApiRoutes.Family.Collection)]
    [ProducesResponseType<IReadOnlyList<FamilyRecipeSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok((await family.ListForUserAsync(UserId, ct)).Select(r => r.ToSummary()).ToList());

    [HttpGet(ApiRoutes.Family.ById)]
    [ProducesResponseType<FamilyRecipeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var recipe = await family.GetAsync(id, UserId, ct);
        return recipe is null ? this.NotFoundProblem("Not found", $"No family recipe {id}.") : Ok(recipe.ToDto(UserId));
    }

    [HttpPut(ApiRoutes.Family.ById)]
    [ProducesResponseType<FamilyRecipeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFamilyRecipeRequest request, CancellationToken ct)
    {
        var recipe = await service.UpdateAsync(id, UserId, request, ct);
        return recipe is null ? this.NotFoundProblem("Not found", $"No family recipe {id}.") : Ok(recipe.ToDto(UserId));
    }

    [HttpPost(ApiRoutes.Family.Members)]
    [ProducesResponseType<InviteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Invite(Guid id, [FromBody] AddMemberRequest request, CancellationToken ct)
    {
        var invite = await service.InviteAsync(id, UserId, request, ct);
        if (invite is null) return this.NotFoundProblem("Not found", $"No family recipe {id}.");

        var location = ApiRoutes.Family.MemberById.Replace("{id}", id.ToString()).Replace("{memberId}", invite.MemberId.ToString());
        return Created(location, invite.ToDto());
    }

    [HttpDelete(ApiRoutes.Family.MemberById)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId, CancellationToken ct)
        => await service.RemoveMemberAsync(id, memberId, UserId, ct)
            ? NoContent()
            : this.NotFoundProblem("Not found", $"No member {memberId} on family recipe {id}.");

    [HttpPost(ApiRoutes.Family.Accept)]
    [ProducesResponseType<FamilyRecipeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept([FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        var profile = await profiles.GetOrCreateProfileAsync(UserId, ct);
        try
        {
            var recipe = await service.AcceptInviteAsync(request.Code, UserId, profile.DisplayName, ct);
            return recipe is null ? this.NotFoundProblem("Invite not found", "No such invite code.") : Ok(recipe.ToDto(UserId));
        }
        catch (InvalidOperationException ex)
        {
            return this.ConflictProblem(ex.Message);
        }
    }

    [HttpPost(ApiRoutes.Family.Notes)]
    [ProducesResponseType<FamilyNoteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddNoteRequest request, CancellationToken ct)
    {
        var profile = await profiles.GetOrCreateProfileAsync(UserId, ct);
        var authorName = profile.DisplayName.Length > 0 ? profile.DisplayName : "A family member";

        var note = await service.AddNoteAsync(id, UserId, authorName, request.Body, ct);
        if (note is null) return this.NotFoundProblem("Not found", $"No family recipe {id}.");

        return Created(ApiRoutes.Family.ById.Replace("{id}", id.ToString()), note.ToDto());
    }

    [HttpPut(ApiRoutes.Family.Privacy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrivacy(Guid id, [FromBody] SetPrivacyRequest request, CancellationToken ct)
        => await service.SetPrivacyAsync(id, UserId, request.Privacy, ct)
            ? NoContent()
            : this.NotFoundProblem("Not found", $"No family recipe {id}.");

    [HttpPut(ApiRoutes.Family.Media)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AttachMedia(Guid id, Guid mediaId, CancellationToken ct)
        => await service.AttachMediaAsync(id, mediaId, UserId, ct)
            ? NoContent()
            : this.NotFoundProblem("Not found", $"No family recipe {id} or media {mediaId} of yours to attach.");
}
