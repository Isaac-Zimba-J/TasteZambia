using Microsoft.AspNetCore.Identity;

namespace TasteZambia.API.Data.Seed;

public static class RoleSeeder
{
    public static readonly string[] Roles = ["contributor", "reviewer", "admin"];

    public static async Task SeedAsync(RoleManager<IdentityRole> roles)
    {
        foreach (var name in Roles)
            if (!await roles.RoleExistsAsync(name))
                await roles.CreateAsync(new IdentityRole(name));
    }
}
