using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Auth;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

/// <summary>The signed-in account's own data. Nothing here is reachable without a bearer token.</summary>
[ApiController]
[Authorize]
public sealed class MeController(
    ICurrentUser me,
    IUserProfileRepository profiles,
    IPersonalDataRepository personal,
    IPersonalSyncService sync,
    IAccountDeletionService deletion,
    IAuditLog audit) : ControllerBase
{
    private string UserId => me.UserId ?? throw new UnauthorizedAccessException();

    /// <summary>
    /// Deletes the account and everything the archive holds for it. Required by Google Play
    /// for any app that creates an account, and the right thing regardless.
    ///
    /// The body must carry the caller's own device id. It is not a security measure - the
    /// bearer token already proved who this is - it is there so a retried or mistaken DELETE
    /// cannot destroy an archive of family recipes by accident.
    /// </summary>
    [HttpDelete(ApiRoutes.Me.Account)]
    [ProducesResponseType<DeleteAccountResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request, CancellationToken ct)
    {
        if (await deletion.DeleteAsync(UserId, request.ConfirmDeviceId, ct) is not DeletionOutcome.Done done)
            return Problem(
                title: "Confirmation does not match",
                detail: "Send this account's own device id to confirm. Nothing has been deleted.",
                statusCode: StatusCodes.Status400BadRequest);

        // After the deletion, not before: a refused confirmation must not leave a record saying
        // an account was deleted. ActorUserId is a plain column with no foreign key, so the row
        // survives the account it names - which is the point.
        await audit.WriteAsync(AuditActions.AccountDelete, request.ConfirmDeviceId,
            "requested by the account itself", ct);

        var summary = done.Summary;
        return Ok(new DeleteAccountResultDto(
            summary.UnpublishedContributions,
            summary.PublishedContributionsAnonymised,
            summary.FamilyRecipesDeleted,
            summary.FamilyRecipesLeft,
            summary.NotesAnonymised,
            summary.MediaDeleted));
    }

    [HttpGet(ApiRoutes.Me.Profile)]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var p = await profiles.GetOrCreateProfileAsync(UserId, ct);
        return Ok(new ProfileDto(p.DisplayName, p.Location, p.Languages, p.AvatarAsset));
    }

    [HttpPut(ApiRoutes.Me.Profile)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var p = await profiles.GetOrCreateProfileAsync(UserId, ct);
        p.DisplayName = request.DisplayName;
        p.Location = request.Location;
        p.Languages = request.Languages;
        p.UpdatedAt = DateTimeOffset.UtcNow;
        await profiles.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet(ApiRoutes.Me.Onboarding)]
    [ProducesResponseType<OnboardingChoicesDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOnboarding(CancellationToken ct)
    {
        var o = await profiles.GetOrCreateOnboardingAsync(UserId, ct);
        return Ok(new OnboardingChoicesDto(
            o.Language, o.Who,
            o.Tastes.Split(',', StringSplitOptions.RemoveEmptyEntries),
            o.OfflineEnabled, o.StoryNotifications, o.IsComplete));
    }

    [HttpPut(ApiRoutes.Me.Onboarding)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateOnboarding([FromBody] OnboardingChoicesDto request, CancellationToken ct)
    {
        var o = await profiles.GetOrCreateOnboardingAsync(UserId, ct);
        o.Language = request.Language;
        o.Who = request.Who;
        o.Tastes = string.Join(',', request.Tastes);
        o.OfflineEnabled = request.OfflineEnabled;
        o.StoryNotifications = request.StoryNotifications;
        o.IsComplete = request.IsComplete;
        o.UpdatedAt = DateTimeOffset.UtcNow;
        await profiles.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet(ApiRoutes.Me.Saved)]
    [ProducesResponseType<IReadOnlyList<SavedDishDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSaved(CancellationToken ct)
        => Ok((await personal.GetSavedAsync(UserId, ct))
            .Where(s => s.IsSaved)
            .Select(s => new SavedDishDto(s.DishId, s.IsSaved, s.UpdatedAt))
            .ToList());

    [HttpGet(ApiRoutes.Me.Progress)]
    [ProducesResponseType<IReadOnlyList<CookProgressDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProgress(string dishId, CancellationToken ct)
        => Ok((await personal.GetProgressAsync(UserId, dishId, ct))
            .Select(p => new CookProgressDto(p.DishId, p.StepNumber, p.IsDone, p.UpdatedAt))
            .ToList());

    [HttpPost(ApiRoutes.Me.Sync)]
    [ProducesResponseType<SyncResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync([FromBody] SyncRequest request, CancellationToken ct)
        => Ok(await sync.ApplyAsync(UserId, request, ct));
}
