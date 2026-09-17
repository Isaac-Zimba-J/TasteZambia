using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Auth;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Tests.Auth;

[Collection(nameof(DatabaseCollection))]
public class TokenServiceTests(DatabaseFixture fixture)
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "tastezambia-tests",
        Audience = "tastezambia-app",
        SigningKey = "test-signing-key-that-is-at-least-32-bytes-long!!",
    };

    private async Task<(TokenService svc, ArchiveUser user)> SutAsync()
    {
        var db = fixture.NewContext();
        var user = new ArchiveUser { UserName = $"device-{Guid.NewGuid():N}" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (new TokenService(db, Microsoft.Extensions.Options.Options.Create(Options), TimeProvider.System), user);
    }

    [Fact]
    public async Task Issue_ReturnsAnAccessTokenCarryingTheUserId()
    {
        var (svc, user) = await SutAsync();
        var (access, refresh, expires) = await svc.IssueAsync(user, CancellationToken.None);

        Assert.NotEmpty(access);
        Assert.NotEmpty(refresh);
        Assert.True(expires > DateTimeOffset.UtcNow.AddMinutes(50));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(access);
        Assert.Equal(user.Id, jwt.Subject);
        Assert.Equal("tastezambia-tests", jwt.Issuer);
    }

    [Fact]
    public async Task ConsumeRefresh_ReturnsTheUserOnceThenRejectsReuse()
    {
        var (svc, user) = await SutAsync();
        var (_, refresh, _) = await svc.IssueAsync(user, CancellationToken.None);

        var first = await svc.ConsumeRefreshAsync(refresh, CancellationToken.None);
        var second = await svc.ConsumeRefreshAsync(refresh, CancellationToken.None);

        Assert.Equal(user.Id, first!.Id);
        Assert.Null(second);   // rotated: the old token is revoked on use
    }

    [Fact]
    public async Task ConsumeRefresh_RejectsAnUnknownToken()
    {
        var (svc, _) = await SutAsync();
        Assert.Null(await svc.ConsumeRefreshAsync("not-a-real-token", CancellationToken.None));
    }

    [Fact]
    public async Task RefreshTokens_AreStoredHashedNotRaw()
    {
        var (svc, user) = await SutAsync();
        var (_, refresh, _) = await svc.IssueAsync(user, CancellationToken.None);

        await using var db = fixture.NewContext();
        var stored = await db.RefreshTokens.SingleAsync(t => t.UserId == user.Id);
        Assert.NotEqual(refresh, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);   // SHA-256 hex
    }
}
