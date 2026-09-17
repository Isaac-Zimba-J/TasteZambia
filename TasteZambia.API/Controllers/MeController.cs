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
    IPersonalSyncService sync) : ControllerBase
{
    private string UserId => me.UserId ?? throw new UnauthorizedAccessException();

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
