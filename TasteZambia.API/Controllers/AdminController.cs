using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Data.Seed;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
public sealed class AdminController(UserManager<ArchiveUser> users) : ControllerBase
{
    /// <summary>Replaces a user's role set. Unknown role names are rejected.</summary>
    [HttpPut(ApiRoutes.Admin.UserRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetRoles(string userName, [FromBody] string[] roles)
    {
        var unknown = roles.Except(RoleSeeder.Roles).ToList();
        if (unknown.Count > 0)
            return Problem(title: "Unknown role", detail: string.Join(", ", unknown), statusCode: StatusCodes.Status400BadRequest);

        var user = await users.FindByNameAsync(userName);
        if (user is null) return this.NotFoundProblem("User not found", $"No user {userName}.");

        var current = await users.GetRolesAsync(user);
        await users.RemoveFromRolesAsync(user, current.Except(roles));
        await users.AddToRolesAsync(user, roles.Except(current));
        return NoContent();
    }
}
