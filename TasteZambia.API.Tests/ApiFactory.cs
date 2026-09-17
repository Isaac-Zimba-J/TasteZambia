using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Seed;

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
}
