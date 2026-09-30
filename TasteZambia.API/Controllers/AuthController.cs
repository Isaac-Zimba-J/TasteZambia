using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TasteZambia.API.Auth;
using TasteZambia.API.Common;
using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

[ApiController]
public sealed class AuthController(UserManager<ArchiveUser> users, ITokenService tokens) : ControllerBase
{
    /// <summary>Register-or-sign-in for a device. Idempotent for a given (id, secret) pair.</summary>
    /// <remarks>
    /// Rate limited per address. This is the one endpoint that creates accounts, and it also
    /// hashes a password on every call, so unlimited it would be both an account mint and a
    /// way to spend the server's CPU.
    /// </remarks>
    [EnableRateLimiting(RateLimitPolicies.DeviceAuth)]
    [HttpPost(ApiRoutes.Auth.Device)]
    [ProducesResponseType<AuthTokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Device([FromBody] DeviceAuthRequest request, CancellationToken ct)
    {
        var user = await users.FindByNameAsync(request.DeviceId);

        if (user is null)
        {
            user = new ArchiveUser { UserName = request.DeviceId };
            var created = await users.CreateAsync(user, request.DeviceSecret);
            if (!created.Succeeded)
                return Problem(title: "Could not create account",
                               detail: string.Join("; ", created.Errors.Select(e => e.Description)),
                               statusCode: StatusCodes.Status400BadRequest);

            await users.AddToRoleAsync(user, "contributor");
        }
        else if (!await users.CheckPasswordAsync(user, request.DeviceSecret))
        {
            return Unauthorized();
        }

        return Ok(await IssueAsync(user, ct));
    }

    [EnableRateLimiting(RateLimitPolicies.Refresh)]
    [HttpPost(ApiRoutes.Auth.Refresh)]
    [ProducesResponseType<AuthTokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var user = await tokens.ConsumeRefreshAsync(request.RefreshToken, ct);
        return user is null ? Unauthorized() : Ok(await IssueAsync(user, ct));
    }

    private async Task<AuthTokensDto> IssueAsync(ArchiveUser user, CancellationToken ct)
    {
        var (access, refresh, expires) = await tokens.IssueAsync(user, [.. await users.GetRolesAsync(user)], ct);
        return new AuthTokensDto(access, refresh, expires);
    }
}
