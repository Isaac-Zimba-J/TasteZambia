using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Data.Seed;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests;

/// <summary>
/// Boots the real API against the shared Testcontainers Postgres. Environment is
/// "Testing" so Program's Development-only migrate-and-seed does not run; the
/// fixture has already migrated, and we seed here explicitly.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ApiFactory(string connectionString) => _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "tastezambia-tests",
            ["Jwt:Audience"] = "tastezambia-app",
            ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-bytes-long!!",
        }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<TasteZambiaDbContext>));
            services.AddDbContext<TasteZambiaDbContext>(o => o.UseNpgsql(_connectionString));
        });
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TasteZambiaDbContext>();
        await ArchiveSeeder.SeedAsync(db);
        await RoleSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
    }

    private const string TestSecret = "ssssssssssssssssssssssssssssssssssssssss";

    /// <summary>
    /// A client signed in as a device account - a brand-new one, or an existing one by id.
    /// With a role, the role is granted and the token re-issued so it carries the claim.
    /// </summary>
    public async Task<(HttpClient Client, string DeviceId)> SignedInClientAsync(string? role = null, string? existingDeviceId = null)
    {
        var client = CreateClient();
        var deviceId = existingDeviceId ?? $"device-{Guid.NewGuid():N}";
        var tokens = await SignInAsync(client, deviceId);

        if (role is not null)
        {
            using var scope = Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ArchiveUser>>();
            await users.AddToRoleAsync((await users.FindByNameAsync(deviceId))!, role);
            tokens = await SignInAsync(client, deviceId);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return (client, deviceId);
    }

    private static async Task<AuthTokensDto> SignInAsync(HttpClient client, string deviceId)
        => (await (await client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest(deviceId, TestSecret)))
            .Content.ReadFromJsonAsync<AuthTokensDto>())!;
}
