using Microsoft.AspNetCore.Identity;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Seed;

/// <summary>
/// Development only: one reviewer+admin device account from configuration, so the whole
/// review loop can be driven from Scalar on a laptop. No section, no account.
/// </summary>
public static class ReviewerSeeder
{
    public static async Task SeedAsync(IConfiguration config, UserManager<ArchiveUser> users, TasteZambiaDbContext db)
    {
        var section = config.GetSection("Reviewer");
        var id = section["DeviceId"];
        var secret = section["DeviceSecret"];
        var name = section["DisplayName"];
        if (id is null || secret is null) return;

        var user = await users.FindByNameAsync(id);
        if (user is null)
        {
            user = new ArchiveUser { UserName = id };
            var created = await users.CreateAsync(user, secret);
            if (!created.Succeeded)
                throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        foreach (var role in new[] { "reviewer", "admin" })
            if (!await users.IsInRoleAsync(user, role))
                await users.AddToRoleAsync(user, role);

        if (name is not null && await db.UserProfiles.FindAsync(user.Id) is null)
        {
            db.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id, DisplayName = name, Location = "Kasama, Northern", UpdatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }
    }
}
