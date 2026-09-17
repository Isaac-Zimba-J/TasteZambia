using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Auth;

public interface ITokenService
{
    Task<(string Access, string Refresh, DateTimeOffset AccessExpires)> IssueAsync(ArchiveUser user, CancellationToken ct);

    /// <summary>Validates and revokes a refresh token. Returns its user, or null if unknown, expired or already used.</summary>
    Task<ArchiveUser?> ConsumeRefreshAsync(string rawRefresh, CancellationToken ct);
}

public sealed class TokenService(TasteZambiaDbContext db, IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JwtOptions _jwt = options.Value;

    public async Task<(string Access, string Refresh, DateTimeOffset AccessExpires)> IssueAsync(ArchiveUser user, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(_jwt.AccessMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var jwt = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ],
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var access = new JwtSecurityTokenHandler().WriteToken(jwt);

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(raw),
            ExpiresAt = now.AddDays(_jwt.RefreshDays),
        });
        await db.SaveChangesAsync(ct);

        return (access, raw, expires);
    }

    public async Task<ArchiveUser?> ConsumeRefreshAsync(string rawRefresh, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var hash = Hash(rawRefresh);
        var token = await db.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null || !token.IsActive(now))
            return null;

        token.RevokedAt = now;   // single use
        await db.SaveChangesAsync(ct);
        return token.User;
    }

    private static string Hash(string raw)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
