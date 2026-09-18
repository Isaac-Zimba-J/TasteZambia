using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using Testcontainers.PostgreSql;

namespace TasteZambia.API.Tests;

/// <summary>One Postgres container for the whole test run; a fresh context per test.</summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("tastezambia_test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = NewContext();
        await db.Database.MigrateAsync();
    }

    public TasteZambiaDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<TasteZambiaDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new TasteZambiaDbContext(options);
    }

    /// <summary>Publishing tests write real dishes; the archive read tests assume only the seed. Call from DisposeAsync.</summary>
    public async Task RemoveCommunityDishesAsync()
    {
        await using var db = NewContext();
        await db.Dishes.Where(d => d.Provenance == TasteZambia.Shared.Enums.Provenance.Community).ExecuteDeleteAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition(nameof(DatabaseCollection))]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
