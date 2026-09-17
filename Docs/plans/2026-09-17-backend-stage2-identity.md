# Stage 2 — Identity and the Personal Layer

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every phone an account without a login screen, and make onboarding choices, favourites and cook progress persist to it — surviving reinstalls, and working offline first.

**Architecture:** ASP.NET Core Identity on the existing `TasteZambiaDbContext`, JWT bearer with rotating refresh tokens. Accounts are **anonymous and device-bound**: on first launch the app registers a user whose username is a generated device id and whose password is a generated device secret, both kept in `SecureStorage`. No screen changes — the design has no login, and now it does not need one. Personal data is **written locally first** (a small JSON store behind `ILocalStore`) with an outbox, and reconciled to the account through `POST /me/sync` using last-write-wins on a client timestamp. Every personal row is a soft-delete flag plus `UpdatedAt`, which makes LWW trivial and makes "unsave" a normal write.

**Tech Stack:** ASP.NET Core Identity (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`), `Microsoft.AspNetCore.Authentication.JwtBearer`, MAUI `SecureStorage` + `Preferences`, `Microsoft.Extensions.Http` `DelegatingHandler`.

**Spec:** `Docs/plans/2026-09-09-backend-api-plan.md` — "Stage 2 — Identity and the personal layer", plus the two decisions recorded below.
**Builds on:** Stage 1 (`Controllers/`, `Repositories/`, `Services/`, `ControllerResultExtensions`, the mobile `LoadOnceView` and `AppNavigationService`).

## Decisions recorded

1. **Anonymous device accounts, upgradable later.** No sign-up screen. A later "add an email to keep your recipes" step links an email to the same user; nothing here prevents it.
2. **Offline first, last-write-wins.** A heart toggled in a kitchen with no signal works instantly and reconciles later. Losing a toggle to a concurrent edit on another device is acceptable; a merge UI is not.

## Global Constraints

### Carried from Stage 1
- The dependency rule: `Controllers/ → Services/ → Repositories/ → DbContext`. No controller injects `DbContext`.
- Every wire type lives in `TasteZambia.Shared/Contracts/`. Changes are additive.
- Do **not** put `[Produces("application/json")]` on a controller.
- `TasteZambia.Core` references nothing MAUI. Anything touching `SecureStorage`, `Preferences` or `DeviceInfo` lives in `TasteZambia.Mobile` behind a Core interface.
- Repair the `.sln` header after any `dotnet sln` command (see Stage 1 plan).

### New for Stage 2
- **Identity's `OnModelCreating` must run first.** `base.OnModelCreating(b)` currently runs *after* `ApplyConfigurationsFromAssembly`; with `IdentityDbContext` it must run before, or the Identity tables are not configured. Task 1 reorders it.
- **JWT signing key comes from configuration**, never a literal in code. Development uses `appsettings.Development.json`; production supplies `Jwt__SigningKey` through the environment. Minimum 32 bytes.
- **Access tokens live 60 minutes, refresh tokens 30 days and rotate on every use.** A refresh token is stored hashed; the raw value is returned once.
- **Device secrets are high-entropy, not human passwords.** Identity's password policy is relaxed to length ≥ 32 only. When email accounts arrive they get their own validator; do not tighten this one.
- **Personal rows are never hard-deleted.** `SavedDish.IsSaved`, `CookProgress.IsDone` and `UpdatedAt` are the whole model; sync compares timestamps.
- **Client timestamps are `DateTimeOffset.UtcNow` at the moment of the tap**, sent with every change. The server never substitutes its own clock for the client's on a sync write.
- **Local store is JSON in `Preferences`** behind `ILocalStore`. SQLite is the upgrade path if it ever grows past a few hundred rows; nothing above `ILocalStore` would change.

---

## File Structure

### `TasteZambia.Shared` (contracts)
| Path | Holds |
|---|---|
| `Contracts/Auth/AuthContracts.cs` | `DeviceAuthRequest`, `RefreshRequest`, `AuthTokensDto` |
| `Contracts/Me/ProfileContracts.cs` | `ProfileDto`, `UpdateProfileRequest`, `OnboardingChoicesDto` |
| `Contracts/Me/PersonalContracts.cs` | `SavedDishDto`, `CookProgressDto`, `SyncChangeDto`, `SyncRequest`, `SyncResponse` |
| `Routes/ApiRoutes.cs` | `Auth.*`, `Me.*` added |

### `TasteZambia.API`
| Path | Holds |
|---|---|
| `Data/Entities/Identity.cs` | `ArchiveUser : IdentityUser`, `RefreshToken` |
| `Data/Entities/Personal.cs` | `UserProfile`, `OnboardingChoices`, `SavedDish`, `CookProgress` |
| `Data/Configurations/PersonalConfigurations.cs` | Keys, indexes, cascades for the four |
| `Data/TasteZambiaDbContext.cs` | Now `IdentityDbContext<ArchiveUser>`; `base.OnModelCreating` first |
| `Data/Seed/RoleSeeder.cs` | `contributor`, `reviewer`, `admin` |
| `Auth/JwtOptions.cs` | Bound from `Jwt` config section |
| `Auth/TokenService.cs` | `ITokenService`: issue access + refresh, validate refresh, rotate |
| `Auth/CurrentUser.cs` | `ICurrentUser`: the caller's user id from the claims principal |
| `Repositories/PersonalRepositories.cs` | `IUserProfileRepository`, `IPersonalDataRepository` |
| `Services/PersonalSyncService.cs` | `IPersonalSyncService`: applies LWW, returns current state |
| `Controllers/AuthController.cs` | `POST auth/device`, `POST auth/refresh` |
| `Controllers/MeController.cs` | `GET/PUT me`, `GET/PUT me/onboarding`, `GET me/saved`, `GET me/progress/{dishId}`, `POST me/sync` |

### `TasteZambia.Core` (mobile, no MAUI)
| Path | Holds |
|---|---|
| `Services/IDeviceIdentity.cs` | `DeviceCredentials(Id, Secret)`; `IDeviceIdentity.GetOrCreateAsync()` |
| `Services/IAuthSession.cs` | `IAuthSession`: `AccessToken`, `EnsureSignedInAsync()`, `RefreshAsync()` |
| `Services/ILocalStore.cs` | `ILocalStore`: typed get/set of JSON documents by key |
| `Services/PersonalStore.cs` | Local model: `PersonalState` (saved, progress, outbox) + `PersonalStore` over `ILocalStore` |
| `Services/FavouritesService.cs` | **Rewritten** over `PersonalStore`; same interface |
| `Services/CookingProgressService.cs` | **Rewritten** over `PersonalStore`; same interface |
| `Services/OnboardingService.cs` | **Rewritten**: `IsComplete`/`Current` from `ILocalStore`, pushed to `/me/onboarding` |
| `Services/PersonalSyncService.cs` | `IPersonalSyncService.SyncAsync()`: drain outbox → `/me/sync` → apply response |
| `Data/Http/AuthenticatedHandler.cs` | `DelegatingHandler`: bearer header, one retry after refresh on 401 |
| `Data/Http/HttpProfileRepository.cs` | `IProfileRepository` over `GET /me` |

### `TasteZambia.Mobile`
| Path | Holds |
|---|---|
| `Services/SecureDeviceIdentity.cs` | `IDeviceIdentity` over `SecureStorage` |
| `Services/PreferencesLocalStore.cs` | `ILocalStore` over `Preferences` |
| `Services/SyncScheduler.cs` | Runs `SyncAsync` on launch, on resume, and 2 s after any local change |
| `MauiProgram.cs` | Registrations; `AuthenticatedHandler` on the five API clients |

### Tests
| Path | Covers |
|---|---|
| `TasteZambia.API.Tests/Auth/TokenServiceTests.cs` | Issue/validate/rotate/reject |
| `TasteZambia.API.Tests/Controllers/AuthEndpointTests.cs` | Device register-or-login, refresh rotation, bad secret |
| `TasteZambia.API.Tests/Controllers/MeEndpointTests.cs` | Profile, onboarding, 401 without token |
| `TasteZambia.API.Tests/Services/PersonalSyncTests.cs` | LWW: newer client wins, older loses, unsave, cross-device |
| `TasteZambia.Core.Tests/Services/PersonalStoreTests.cs` | Local write, outbox, apply-server-state |
| `TasteZambia.Core.Tests/Services/AuthenticatedHandlerTests.cs` | Bearer attached; 401 → refresh → retry once |
| `TasteZambia.Core.Tests/Services/OnboardingPersistenceTests.cs` | `IsComplete` survives a new service instance |

---

## Task 1: Identity, JWT, and the token service

**Files:**
- Modify: `TasteZambia.API/TasteZambia.API.csproj`, `Data/TasteZambiaDbContext.cs`, `Program.cs`, `appsettings.Development.json`
- Create: `Data/Entities/Identity.cs`, `Auth/JwtOptions.cs`, `Auth/TokenService.cs`, `Auth/CurrentUser.cs`, `Data/Seed/RoleSeeder.cs`
- Test: `TasteZambia.API.Tests/Auth/TokenServiceTests.cs`

**Interfaces:**
- Produces:
  - `ArchiveUser : IdentityUser` with `DateTimeOffset CreatedAt`
  - `RefreshToken { int Id; string UserId; string TokenHash; DateTimeOffset ExpiresAt; DateTimeOffset? RevokedAt; }`
  - `JwtOptions { string Issuer; string Audience; string SigningKey; int AccessMinutes = 60; int RefreshDays = 30; }`
  - `ITokenService`: `Task<(string Access, string Refresh, DateTimeOffset AccessExpires)> IssueAsync(ArchiveUser user, CancellationToken)`, `Task<ArchiveUser?> ConsumeRefreshAsync(string rawRefresh, CancellationToken)` (validates, revokes, returns the user — caller issues new pair)
  - `ICurrentUser { string? UserId; bool IsAuthenticated; }`
  - `RoleSeeder.SeedAsync(RoleManager<IdentityRole>)` creating `contributor`, `reviewer`, `admin`

- [ ] **Step 1: Add the packages**

```bash
cd "/Users/zimbadev/Documents/Workspace/Maui Projects/TasteZambia"
export PATH="/usr/local/share/dotnet:$PATH"
dotnet add TasteZambia.API package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add TasteZambia.API package Microsoft.AspNetCore.Authentication.JwtBearer
```

- [ ] **Step 2: Write the failing test**

`TasteZambia.API.Tests/Auth/TokenServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TasteZambia.API.Auth;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Tests.Auth;

[Collection(nameof(DatabaseCollection))]
public class TokenServiceTests(DatabaseFixture fixture)
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "tastezambia-tests", Audience = "tastezambia-app",
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

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(access);
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
        var stored = await db.Set<RefreshToken>().SingleAsync(t => t.UserId == user.Id);
        Assert.NotEqual(refresh, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);   // SHA-256 hex
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~TokenServiceTests`
Expected: FAIL — `TasteZambia.API.Auth` does not exist.

- [ ] **Step 4: Write the entities and reorder the context**

`TasteZambia.API/Data/Entities/Identity.cs`:

```csharp
using Microsoft.AspNetCore.Identity;

namespace TasteZambia.API.Data.Entities;

public class ArchiveUser : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>One row per issued refresh token. Rotated: consuming one revokes it and issues another.</summary>
public class RefreshToken
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public ArchiveUser User { get; set; } = null!;

    /// <summary>SHA-256 hex of the raw token. The raw value is returned to the client once and never stored.</summary>
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}
```

In `TasteZambiaDbContext.cs`, change the class declaration and `OnModelCreating`:

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
// ...
public class TasteZambiaDbContext(DbContextOptions<TasteZambiaDbContext> options)
    : IdentityDbContext<ArchiveUser>(options)
{
    // existing DbSets...
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Identity configures its own tables here. It MUST run before ours.
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(TasteZambiaDbContext).Assembly);

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UserId);
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
```

- [ ] **Step 5: Write `JwtOptions`, `TokenService`, `CurrentUser`**

`TasteZambia.API/Auth/JwtOptions.cs`:

```csharp
namespace TasteZambia.API.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }

    /// <summary>At least 32 bytes. Never a literal in code; production supplies Jwt__SigningKey.</summary>
    public required string SigningKey { get; init; }

    public int AccessMinutes { get; init; } = 60;
    public int RefreshDays { get; init; } = 30;
}
```

`TasteZambia.API/Auth/TokenService.cs`:

```csharp
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
        var token = await db.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == Hash(rawRefresh), ct);

        if (token is null || !token.IsActive(now))
            return null;

        token.RevokedAt = now;   // single use
        await db.SaveChangesAsync(ct);
        return token.User;
    }

    private static string Hash(string raw)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
```

`TasteZambia.API/Auth/CurrentUser.cs`:

```csharp
using System.Security.Claims;

namespace TasteZambia.API.Auth;

public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? accessor.HttpContext?.User.FindFirstValue("sub");

    public bool IsAuthenticated => UserId is not null;
}
```

`TasteZambia.API/Data/Seed/RoleSeeder.cs`:

```csharp
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
```

- [ ] **Step 6: Register Identity and JWT in `Program.cs`**

After the DbContext registration:

```csharp
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddIdentityCore<ArchiveUser>(o =>
    {
        // Device secrets are 48 random bytes, not human passwords. Length is the only
        // meaningful check. Email accounts, when they come, get their own validator.
        o.Password.RequiredLength = 32;
        o.Password.RequireDigit = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz0123456789-";
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<TasteZambiaDbContext>();

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();
```

And in the pipeline, **before** `MapControllers()`:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

In the Development startup block, after seeding the archive:

```csharp
await RoleSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
```

`appsettings.Development.json` gains:

```json
"Jwt": {
  "Issuer": "tastezambia-dev",
  "Audience": "tastezambia-app",
  "SigningKey": "dev-only-signing-key-change-me-at-least-32-bytes"
}
```

The test factory (`ApiFactory`) must supply the same section — add to `ConfigureWebHost`:

```csharp
builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Issuer"] = "tastezambia-tests",
    ["Jwt:Audience"] = "tastezambia-app",
    ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-bytes-long!!",
}));
```

- [ ] **Step 7: Migrate**

```bash
export PATH="/usr/local/share/dotnet:$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add Identity --project TasteZambia.API
dotnet ef database update --project TasteZambia.API
```

Verify: `docker exec tastezambia-db psql -U tastezambia -d tastezambia -tAc "\dt"` now lists `AspNetUsers`, `AspNetRoles`, `refresh_tokens` alongside the archive tables.

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.API.Tests`
Expected: PASS — the 4 new tests, and all 40 existing ones still green (the `Testing` environment skips role seeding; the fixture migrates).

- [ ] **Step 9: Commit**

```bash
git add -A && git commit -m "feat(api): add Identity, JWT bearer auth and rotating refresh tokens

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 2: Device authentication endpoints

**Files:**
- Create: `TasteZambia.Shared/Contracts/Auth/AuthContracts.cs`, `TasteZambia.API/Controllers/AuthController.cs`
- Modify: `TasteZambia.Shared/Routes/ApiRoutes.cs`
- Test: `TasteZambia.API.Tests/Controllers/AuthEndpointTests.cs`

**Interfaces:**
- Produces:
  - `DeviceAuthRequest(string DeviceId, string DeviceSecret)` — `DeviceId` is `[a-z0-9-]{16,64}`, `DeviceSecret` ≥ 32 chars
  - `RefreshRequest(string RefreshToken)`
  - `AuthTokensDto(string AccessToken, string RefreshToken, DateTimeOffset AccessExpiresAt)`
  - `ApiRoutes.Auth.Device = "/api/v1/auth/device"`, `ApiRoutes.Auth.Refresh = "/api/v1/auth/refresh"`
  - `POST auth/device` → 200 `AuthTokensDto` (creates the user on first call, signs in thereafter); 401 on wrong secret; 400 on malformed
  - `POST auth/refresh` → 200 new pair; 401 if the token is unknown, expired or reused

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

[Collection(nameof(DatabaseCollection))]
public class AuthEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }
    public Task DisposeAsync() { _client.Dispose(); _factory.Dispose(); return Task.CompletedTask; }

    private static DeviceAuthRequest NewDevice() => new(
        $"device-{Guid.NewGuid():N}",
        Convert.ToBase64String(Guid.NewGuid().ToByteArray().Concat(Guid.NewGuid().ToByteArray()).ToArray()));

    [Fact]
    public async Task FirstCall_CreatesTheAccountAndReturnsTokens()
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, NewDevice());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>();
        Assert.NotEmpty(tokens!.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken);
        Assert.True(tokens.AccessExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SecondCallWithTheSameSecret_SignsInTheSameAccount()
    {
        var device = NewDevice();
        var a = await (await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, device)).Content.ReadFromJsonAsync<AuthTokensDto>();
        var b = await (await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, device)).Content.ReadFromJsonAsync<AuthTokensDto>();

        var subA = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(a!.AccessToken).Subject;
        var subB = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(b!.AccessToken).Subject;
        Assert.Equal(subA, subB);
    }

    [Fact]
    public async Task WrongSecretForAKnownDevice_Is401()
    {
        var device = NewDevice();
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, device);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, device with { DeviceSecret = new string('x', 40) });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MalformedDeviceId_Is400()
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest("BAD ID!", new string('x', 40)));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesAndRejectsReuse()
    {
        var tokens = await (await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, NewDevice())).Content.ReadFromJsonAsync<AuthTokensDto>();

        var first = await _client.PostAsJsonAsync(ApiRoutes.Auth.Refresh, new RefreshRequest(tokens!.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var rotated = await first.Content.ReadFromJsonAsync<AuthTokensDto>();
        Assert.NotEqual(tokens.RefreshToken, rotated!.RefreshToken);

        var reuse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Refresh, new RefreshRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~AuthEndpointTests`
Expected: FAIL — contracts do not exist.

- [ ] **Step 3: Write the contracts and routes**

`TasteZambia.Shared/Contracts/Auth/AuthContracts.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace TasteZambia.Shared.Contracts.Auth;

/// <summary>
/// Anonymous, device-bound sign-in. The app generates both values once and keeps them
/// in secure storage; the same pair signs in the same account for the life of the install.
/// </summary>
public sealed record DeviceAuthRequest(
    [property: Required, RegularExpression("^[a-z0-9-]{16,64}$")] string DeviceId,
    [property: Required, MinLength(32), MaxLength(256)] string DeviceSecret);

public sealed record RefreshRequest([property: Required] string RefreshToken);

public sealed record AuthTokensDto(string AccessToken, string RefreshToken, DateTimeOffset AccessExpiresAt);
```

Add to `ApiRoutes`:

```csharp
public static class Auth
{
    public const string Device = $"{Root}/auth/device";
    public const string Refresh = $"{Root}/auth/refresh";
}

public static class Me
{
    public const string Profile = $"{Root}/me";
    public const string Onboarding = $"{Root}/me/onboarding";
    public const string Saved = $"{Root}/me/saved";
    public const string Progress = $"{Root}/me/progress/{{dishId}}";
    public const string Sync = $"{Root}/me/sync";
}
```

- [ ] **Step 4: Write the controller**

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Auth;
using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

[ApiController]
public sealed class AuthController(UserManager<ArchiveUser> users, ITokenService tokens) : ControllerBase
{
    /// <summary>Register-or-sign-in for a device. Idempotent for a given (id, secret) pair.</summary>
    [HttpPost(ApiRoutes.Auth.Device)]
    [ProducesResponseType<AuthTokensDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
        var (access, refresh, expires) = await tokens.IssueAsync(user, ct);
        return new AuthTokensDto(access, refresh, expires);
    }
}
```

`AddToRoleAsync` needs the role to exist. The test factory runs in `Testing` where `RoleSeeder` does not run — add role seeding to `ApiFactory.SeedAsync()` too, using a scope's `RoleManager<IdentityRole>`. Do it there rather than in `Program.cs` unconditionally, so production still seeds roles only through a deliberate step.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~AuthEndpointTests`
Expected: PASS, 5 tests.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(api): device register-or-sign-in and refresh endpoints

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 3: Personal entities, repositories and the sync service

**Files:**
- Create: `TasteZambia.API/Data/Entities/Personal.cs`, `Data/Configurations/PersonalConfigurations.cs`, `Repositories/PersonalRepositories.cs`, `Services/PersonalSyncService.cs`
- Create: `TasteZambia.Shared/Contracts/Me/PersonalContracts.cs`
- Modify: `Data/TasteZambiaDbContext.cs` (DbSets), `Program.cs` (registrations)
- Test: `TasteZambia.API.Tests/Services/PersonalSyncTests.cs`

**Interfaces:**
- Produces (entities):
  - `UserProfile { string UserId (PK); string DisplayName; string Location; string Languages; string? AvatarAsset; DateTimeOffset UpdatedAt }`
  - `OnboardingChoices { string UserId (PK); string Language; string Who; string Tastes (comma-joined); bool OfflineEnabled; bool StoryNotifications; bool IsComplete; DateTimeOffset UpdatedAt }`
  - `SavedDish { string UserId; string DishId; bool IsSaved; DateTimeOffset UpdatedAt }` — PK `(UserId, DishId)`
  - `CookProgress { string UserId; string DishId; int StepNumber; bool IsDone; DateTimeOffset UpdatedAt }` — PK `(UserId, DishId, StepNumber)`
- Produces (contracts):
  - `SyncChangeDto(string Kind, string DishId, int? Step, bool Value, DateTimeOffset At)` — `Kind` is `"saved"` or `"progress"`
  - `SyncRequest(IReadOnlyList<SyncChangeDto> Changes)`
  - `SavedDishDto(string DishId, bool IsSaved, DateTimeOffset UpdatedAt)`, `CookProgressDto(string DishId, int StepNumber, bool IsDone, DateTimeOffset UpdatedAt)`
  - `SyncResponse(IReadOnlyList<SavedDishDto> Saved, IReadOnlyList<CookProgressDto> Progress, DateTimeOffset ServerTime)`
- Produces (services):
  - `IPersonalDataRepository`: `GetSavedAsync(userId)`, `GetProgressAsync(userId, dishId?)`, `UpsertSavedAsync(userId, dishId, isSaved, at)`, `UpsertProgressAsync(userId, dishId, step, isDone, at)` — upserts apply **only if `at` is newer** than the stored `UpdatedAt`
  - `IPersonalSyncService.ApplyAsync(string userId, SyncRequest request, CancellationToken) → SyncResponse`

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Me;

namespace TasteZambia.API.Tests.Services;

[Collection(nameof(DatabaseCollection))]
public class PersonalSyncTests(DatabaseFixture fixture)
{
    private async Task<(PersonalSyncService svc, string userId)> SutAsync()
    {
        var db = fixture.NewContext();
        var user = new ArchiveUser { UserName = $"device-{Guid.NewGuid():N}" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (new PersonalSyncService(new PersonalDataRepository(db), TimeProvider.System), user.Id);
    }

    private static SyncChangeDto Save(string dish, bool value, DateTimeOffset at) => new("saved", dish, null, value, at);
    private static SyncChangeDto Step(string dish, int step, bool value, DateTimeOffset at) => new("progress", dish, step, value, at);

    [Fact]
    public async Task NewerClientChange_Wins()
    {
        var (svc, uid) = await SutAsync();
        var t0 = DateTimeOffset.UtcNow.AddMinutes(-10);

        await svc.ApplyAsync(uid, new SyncRequest([Save("ifisashi", true, t0)]), default);
        var result = await svc.ApplyAsync(uid, new SyncRequest([Save("ifisashi", false, t0.AddMinutes(1))]), default);

        Assert.False(result.Saved.Single(s => s.DishId == "ifisashi").IsSaved);
    }

    [Fact]
    public async Task OlderClientChange_Loses()
    {
        var (svc, uid) = await SutAsync();
        var t0 = DateTimeOffset.UtcNow.AddMinutes(-10);

        await svc.ApplyAsync(uid, new SyncRequest([Save("ifisashi", true, t0)]), default);
        var result = await svc.ApplyAsync(uid, new SyncRequest([Save("ifisashi", false, t0.AddMinutes(-1))]), default);

        Assert.True(result.Saved.Single(s => s.DishId == "ifisashi").IsSaved);   // stale unsave ignored
    }

    [Fact]
    public async Task ProgressIsPerStep_AndReturnedInFull()
    {
        var (svc, uid) = await SutAsync();
        var t = DateTimeOffset.UtcNow.AddMinutes(-5);

        var result = await svc.ApplyAsync(uid, new SyncRequest([Step("ifisashi", 1, true, t), Step("ifisashi", 3, true, t)]), default);

        Assert.Equal(2, result.Progress.Count(p => p.DishId == "ifisashi" && p.IsDone));
        Assert.DoesNotContain(result.Progress, p => p.StepNumber == 2 && p.IsDone);
    }

    [Fact]
    public async Task EmptyRequest_ReturnsCurrentState()
    {
        var (svc, uid) = await SutAsync();
        await svc.ApplyAsync(uid, new SyncRequest([Save("chikanda", true, DateTimeOffset.UtcNow.AddMinutes(-1))]), default);

        var result = await svc.ApplyAsync(uid, new SyncRequest([]), default);

        Assert.Single(result.Saved, s => s.DishId == "chikanda" && s.IsSaved);
        Assert.True(result.ServerTime <= DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task UsersDoNotSeeEachOther()
    {
        var (svc, a) = await SutAsync();
        var (_, b) = await SutAsync();
        await svc.ApplyAsync(a, new SyncRequest([Save("delele", true, DateTimeOffset.UtcNow)]), default);

        var forB = await svc.ApplyAsync(b, new SyncRequest([]), default);
        Assert.Empty(forB.Saved);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~PersonalSyncTests`
Expected: FAIL.

- [ ] **Step 3: Write the entities and configurations**

`TasteZambia.API/Data/Entities/Personal.cs`:

```csharp
namespace TasteZambia.API.Data.Entities;

public class UserProfile
{
    public required string UserId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "";
    public string Languages { get; set; } = "";
    public string? AvatarAsset { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class OnboardingChoices
{
    public required string UserId { get; set; }
    public string Language { get; set; } = "English";
    public string Who { get; set; } = "I grew up here";
    /// <summary>Comma-joined taste keys, e.g. "traditional,veg".</summary>
    public string Tastes { get; set; } = "traditional,veg";
    public bool OfflineEnabled { get; set; } = true;
    public bool StoryNotifications { get; set; }
    public bool IsComplete { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Soft flag, never deleted: "unsave" is IsSaved=false with a newer UpdatedAt.</summary>
public class SavedDish
{
    public required string UserId { get; set; }
    public required string DishId { get; set; }
    public bool IsSaved { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class CookProgress
{
    public required string UserId { get; set; }
    public required string DishId { get; set; }
    public int StepNumber { get; set; }
    public bool IsDone { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

`TasteZambia.API/Data/Configurations/PersonalConfigurations.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> e)
    {
        e.ToTable("user_profiles");
        e.HasKey(x => x.UserId);
        e.Property(x => x.DisplayName).HasMaxLength(120);
        e.Property(x => x.Location).HasMaxLength(120);
        e.Property(x => x.Languages).HasMaxLength(200);
        e.HasOne<ArchiveUser>().WithOne().HasForeignKey<UserProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OnboardingChoicesConfiguration : IEntityTypeConfiguration<OnboardingChoices>
{
    public void Configure(EntityTypeBuilder<OnboardingChoices> e)
    {
        e.ToTable("onboarding_choices");
        e.HasKey(x => x.UserId);
        e.Property(x => x.Language).HasMaxLength(40);
        e.Property(x => x.Who).HasMaxLength(80);
        e.Property(x => x.Tastes).HasMaxLength(200);
        e.HasOne<ArchiveUser>().WithOne().HasForeignKey<OnboardingChoices>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SavedDishConfiguration : IEntityTypeConfiguration<SavedDish>
{
    public void Configure(EntityTypeBuilder<SavedDish> e)
    {
        e.ToTable("saved_dishes");
        e.HasKey(x => new { x.UserId, x.DishId });
        e.Property(x => x.DishId).HasMaxLength(64);
        e.HasOne<ArchiveUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CookProgressConfiguration : IEntityTypeConfiguration<CookProgress>
{
    public void Configure(EntityTypeBuilder<CookProgress> e)
    {
        e.ToTable("cook_progress");
        e.HasKey(x => new { x.UserId, x.DishId, x.StepNumber });
        e.Property(x => x.DishId).HasMaxLength(64);
        e.HasOne<ArchiveUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

Add the DbSets to the context: `UserProfiles`, `OnboardingChoices`, `SavedDishes`, `CookProgress`.

- [ ] **Step 4: Write the contracts**

`TasteZambia.Shared/Contracts/Me/PersonalContracts.cs`:

```csharp
namespace TasteZambia.Shared.Contracts.Me;

/// <summary>One local change, stamped with the client's clock at the moment of the tap.</summary>
public sealed record SyncChangeDto(string Kind, string DishId, int? Step, bool Value, DateTimeOffset At)
{
    public const string Saved = "saved";
    public const string Progress = "progress";
}

public sealed record SyncRequest(IReadOnlyList<SyncChangeDto> Changes);

public sealed record SavedDishDto(string DishId, bool IsSaved, DateTimeOffset UpdatedAt);
public sealed record CookProgressDto(string DishId, int StepNumber, bool IsDone, DateTimeOffset UpdatedAt);

/// <summary>The account's full current state after applying the request. The client replaces its local copy with this.</summary>
public sealed record SyncResponse(IReadOnlyList<SavedDishDto> Saved, IReadOnlyList<CookProgressDto> Progress, DateTimeOffset ServerTime);
```

- [ ] **Step 5: Write the repository and service**

`TasteZambia.API/Repositories/PersonalRepositories.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Repositories;

public interface IPersonalDataRepository
{
    Task<IReadOnlyList<SavedDish>> GetSavedAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<CookProgress>> GetProgressAsync(string userId, string? dishId = null, CancellationToken ct = default);

    /// <summary>Applies only if <paramref name="at"/> is newer than the stored row. Returns whether it applied.</summary>
    Task<bool> UpsertSavedAsync(string userId, string dishId, bool isSaved, DateTimeOffset at, CancellationToken ct = default);
    Task<bool> UpsertProgressAsync(string userId, string dishId, int step, bool isDone, DateTimeOffset at, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class PersonalDataRepository(TasteZambiaDbContext db) : IPersonalDataRepository
{
    public async Task<IReadOnlyList<SavedDish>> GetSavedAsync(string userId, CancellationToken ct = default)
        => await db.SavedDishes.AsNoTracking().Where(s => s.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<CookProgress>> GetProgressAsync(string userId, string? dishId = null, CancellationToken ct = default)
    {
        var q = db.CookProgress.AsNoTracking().Where(p => p.UserId == userId);
        if (dishId is not null) q = q.Where(p => p.DishId == dishId);
        return await q.ToListAsync(ct);
    }

    public async Task<bool> UpsertSavedAsync(string userId, string dishId, bool isSaved, DateTimeOffset at, CancellationToken ct = default)
    {
        var row = await db.SavedDishes.FindAsync([userId, dishId], ct);
        if (row is null)
        {
            db.SavedDishes.Add(new SavedDish { UserId = userId, DishId = dishId, IsSaved = isSaved, UpdatedAt = at });
            return true;
        }
        if (at <= row.UpdatedAt) return false;   // last write wins; this one is older
        row.IsSaved = isSaved; row.UpdatedAt = at;
        return true;
    }

    public async Task<bool> UpsertProgressAsync(string userId, string dishId, int step, bool isDone, DateTimeOffset at, CancellationToken ct = default)
    {
        var row = await db.CookProgress.FindAsync([userId, dishId, step], ct);
        if (row is null)
        {
            db.CookProgress.Add(new CookProgress { UserId = userId, DishId = dishId, StepNumber = step, IsDone = isDone, UpdatedAt = at });
            return true;
        }
        if (at <= row.UpdatedAt) return false;
        row.IsDone = isDone; row.UpdatedAt = at;
        return true;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
```

`TasteZambia.API/Services/PersonalSyncService.cs`:

```csharp
using TasteZambia.API.Repositories;
using TasteZambia.Shared.Contracts.Me;

namespace TasteZambia.API.Services;

public interface IPersonalSyncService
{
    Task<SyncResponse> ApplyAsync(string userId, SyncRequest request, CancellationToken ct);
}

/// <summary>
/// Last-write-wins reconciliation. Each incoming change carries the client's timestamp;
/// it applies only if newer than what the server holds. The response is the account's
/// complete state, which the client adopts wholesale - there is no partial merge.
/// </summary>
public sealed class PersonalSyncService(IPersonalDataRepository personal, TimeProvider clock) : IPersonalSyncService
{
    public async Task<SyncResponse> ApplyAsync(string userId, SyncRequest request, CancellationToken ct)
    {
        foreach (var change in request.Changes)
        {
            switch (change.Kind)
            {
                case SyncChangeDto.Saved:
                    await personal.UpsertSavedAsync(userId, change.DishId, change.Value, change.At, ct);
                    break;
                case SyncChangeDto.Progress when change.Step is { } step:
                    await personal.UpsertProgressAsync(userId, change.DishId, step, change.Value, change.At, ct);
                    break;
                // Unknown kinds are ignored, not rejected: an older client must not be
                // able to break sync for itself by sending something this server predates.
            }
        }

        if (request.Changes.Count > 0)
            await personal.SaveChangesAsync(ct);

        var saved = (await personal.GetSavedAsync(userId, ct))
            .Select(s => new SavedDishDto(s.DishId, s.IsSaved, s.UpdatedAt)).ToList();
        var progress = (await personal.GetProgressAsync(userId, null, ct))
            .Select(p => new CookProgressDto(p.DishId, p.StepNumber, p.IsDone, p.UpdatedAt)).ToList();

        return new SyncResponse(saved, progress, clock.GetUtcNow());
    }
}
```

Register both as `Scoped` in `Program.cs`. Add a migration `Personal` and apply it.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~PersonalSyncTests`
Expected: PASS, 5 tests.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat(api): personal data entities and last-write-wins sync service

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 4: The `/me` endpoints

**Files:**
- Create: `TasteZambia.Shared/Contracts/Me/ProfileContracts.cs`, `TasteZambia.API/Controllers/MeController.cs`
- Modify: `Repositories/PersonalRepositories.cs` (add `IUserProfileRepository`)
- Test: `TasteZambia.API.Tests/Controllers/MeEndpointTests.cs`

**Interfaces:**
- Produces:
  - `ProfileDto(string DisplayName, string Location, string Languages, string? AvatarAsset)`
  - `UpdateProfileRequest(string DisplayName, string Location, string Languages)`
  - `OnboardingChoicesDto(string Language, string Who, IReadOnlyList<string> Tastes, bool OfflineEnabled, bool StoryNotifications, bool IsComplete)`
  - `IUserProfileRepository`: `GetProfileAsync(userId)`, `UpsertProfileAsync(userId, ...)`, `GetOnboardingAsync(userId)`, `UpsertOnboardingAsync(userId, OnboardingChoices)`
  - All `/me/*` routes carry `[Authorize]`; the controller reads `ICurrentUser.UserId`
  - `GET me` → `ProfileDto` (an empty profile is created on first read, never 404)
  - `PUT me` → 204
  - `GET me/onboarding` → `OnboardingChoicesDto` (defaults if none, `IsComplete=false`)
  - `PUT me/onboarding` → 204
  - `GET me/saved` → `IReadOnlyList<SavedDishDto>` (only `IsSaved=true`)
  - `GET me/progress/{dishId}` → `IReadOnlyList<CookProgressDto>`
  - `POST me/sync` → `SyncResponse`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

[Collection(nameof(DatabaseCollection))]
public class MeEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        _client = _factory.CreateClient();

        var tokens = await (await _client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest(
            $"device-{Guid.NewGuid():N}", new string('s', 40)))).Content.ReadFromJsonAsync<AuthTokensDto>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
    }
    public Task DisposeAsync() { _client.Dispose(); _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task WithoutAToken_MeIs401()
    {
        using var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(ApiRoutes.Me.Profile)).StatusCode);
    }

    [Fact]
    public async Task Profile_StartsEmptyAndRoundTrips()
    {
        var empty = await _client.GetFromJsonAsync<ProfileDto>(ApiRoutes.Me.Profile);
        Assert.Equal("", empty!.DisplayName);

        var put = await _client.PutAsJsonAsync(ApiRoutes.Me.Profile, new UpdateProfileRequest("Chanda Mwaba", "Kitwe, Copperbelt", "Bemba, English"));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var after = await _client.GetFromJsonAsync<ProfileDto>(ApiRoutes.Me.Profile);
        Assert.Equal("Chanda Mwaba", after!.DisplayName);
        Assert.Equal("Kitwe, Copperbelt", after.Location);
    }

    [Fact]
    public async Task Onboarding_DefaultsThenPersistsCompletion()
    {
        var before = await _client.GetFromJsonAsync<OnboardingChoicesDto>(ApiRoutes.Me.Onboarding);
        Assert.False(before!.IsComplete);
        Assert.Equal("English", before.Language);
        Assert.Equal(["traditional", "veg"], before.Tastes.OrderBy(t => t));

        var put = await _client.PutAsJsonAsync(ApiRoutes.Me.Onboarding,
            new OnboardingChoicesDto("Bemba", "I live abroad", ["quick", "family"], true, true, true));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var after = await _client.GetFromJsonAsync<OnboardingChoicesDto>(ApiRoutes.Me.Onboarding);
        Assert.True(after!.IsComplete);
        Assert.Equal("Bemba", after.Language);
        Assert.Equal(["family", "quick"], after.Tastes.OrderBy(t => t));
    }

    [Fact]
    public async Task Sync_ThenSavedAndProgressReadBack()
    {
        var now = DateTimeOffset.UtcNow;
        var sync = await _client.PostAsJsonAsync(ApiRoutes.Me.Sync, new SyncRequest(
        [
            new(SyncChangeDto.Saved, "ifisashi", null, true, now),
            new(SyncChangeDto.Saved, "delele", null, true, now),
            new(SyncChangeDto.Saved, "delele", null, false, now.AddSeconds(1)),
            new(SyncChangeDto.Progress, "ifisashi", 2, true, now),
        ]));
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);

        var saved = await _client.GetFromJsonAsync<List<SavedDishDto>>(ApiRoutes.Me.Saved);
        Assert.Equal(["ifisashi"], saved!.Select(s => s.DishId));   // delele was unsaved a second later

        var progress = await _client.GetFromJsonAsync<List<CookProgressDto>>(ApiRoutes.Me.Progress.Replace("{dishId}", "ifisashi"));
        Assert.Single(progress!, p => p.StepNumber == 2 && p.IsDone);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~MeEndpointTests`
Expected: FAIL.

- [ ] **Step 3: Write the contracts**

`TasteZambia.Shared/Contracts/Me/ProfileContracts.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace TasteZambia.Shared.Contracts.Me;

public sealed record ProfileDto(string DisplayName, string Location, string Languages, string? AvatarAsset);

public sealed record UpdateProfileRequest(
    [property: MaxLength(120)] string DisplayName,
    [property: MaxLength(120)] string Location,
    [property: MaxLength(200)] string Languages);

public sealed record OnboardingChoicesDto(
    string Language, string Who, IReadOnlyList<string> Tastes,
    bool OfflineEnabled, bool StoryNotifications, bool IsComplete);
```

- [ ] **Step 4: Add `IUserProfileRepository`**

Append to `PersonalRepositories.cs`:

```csharp
public interface IUserProfileRepository
{
    Task<UserProfile> GetOrCreateProfileAsync(string userId, CancellationToken ct = default);
    Task<OnboardingChoices> GetOrCreateOnboardingAsync(string userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class UserProfileRepository(TasteZambiaDbContext db) : IUserProfileRepository
{
    public async Task<UserProfile> GetOrCreateProfileAsync(string userId, CancellationToken ct = default)
    {
        var row = await db.UserProfiles.FindAsync([userId], ct);
        if (row is not null) return row;
        row = new UserProfile { UserId = userId, UpdatedAt = DateTimeOffset.UtcNow };
        db.UserProfiles.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    public async Task<OnboardingChoices> GetOrCreateOnboardingAsync(string userId, CancellationToken ct = default)
    {
        var row = await db.OnboardingChoices.FindAsync([userId], ct);
        if (row is not null) return row;
        row = new OnboardingChoices { UserId = userId, UpdatedAt = DateTimeOffset.UtcNow };
        db.OnboardingChoices.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
```

- [ ] **Step 5: Write the controller**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Auth;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

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
        p.DisplayName = request.DisplayName; p.Location = request.Location; p.Languages = request.Languages;
        p.UpdatedAt = DateTimeOffset.UtcNow;
        await profiles.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet(ApiRoutes.Me.Onboarding)]
    [ProducesResponseType<OnboardingChoicesDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOnboarding(CancellationToken ct)
    {
        var o = await profiles.GetOrCreateOnboardingAsync(UserId, ct);
        return Ok(new OnboardingChoicesDto(o.Language, o.Who,
            o.Tastes.Split(',', StringSplitOptions.RemoveEmptyEntries), o.OfflineEnabled, o.StoryNotifications, o.IsComplete));
    }

    [HttpPut(ApiRoutes.Me.Onboarding)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateOnboarding([FromBody] OnboardingChoicesDto request, CancellationToken ct)
    {
        var o = await profiles.GetOrCreateOnboardingAsync(UserId, ct);
        o.Language = request.Language; o.Who = request.Who;
        o.Tastes = string.Join(',', request.Tastes);
        o.OfflineEnabled = request.OfflineEnabled; o.StoryNotifications = request.StoryNotifications;
        o.IsComplete = request.IsComplete;
        o.UpdatedAt = DateTimeOffset.UtcNow;
        await profiles.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet(ApiRoutes.Me.Saved)]
    [ProducesResponseType<IReadOnlyList<SavedDishDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSaved(CancellationToken ct)
        => Ok((await personal.GetSavedAsync(UserId, ct)).Where(s => s.IsSaved)
              .Select(s => new SavedDishDto(s.DishId, s.IsSaved, s.UpdatedAt)).ToList());

    [HttpGet(ApiRoutes.Me.Progress)]
    [ProducesResponseType<IReadOnlyList<CookProgressDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProgress(string dishId, CancellationToken ct)
        => Ok((await personal.GetProgressAsync(UserId, dishId, ct))
              .Select(p => new CookProgressDto(p.DishId, p.StepNumber, p.IsDone, p.UpdatedAt)).ToList());

    [HttpPost(ApiRoutes.Me.Sync)]
    [ProducesResponseType<SyncResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync([FromBody] SyncRequest request, CancellationToken ct)
        => Ok(await sync.ApplyAsync(UserId, request, ct));
}
```

Register `IUserProfileRepository` as `Scoped`.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.API.Tests`
Expected: PASS — 4 new, all prior green.

- [ ] **Step 7: Verify by hand and commit**

With the API running on 5080:

```bash
T=$(curl -s -X POST localhost:5080/api/v1/auth/device -H 'content-type: application/json' \
  -d '{"deviceId":"device-manual-test-0001","deviceSecret":"ssssssssssssssssssssssssssssssssssss"}' | python3 -c 'import json,sys;print(json.load(sys.stdin)["accessToken"])')
curl -s localhost:5080/api/v1/me -H "Authorization: Bearer $T"           # {"displayName":"",...}
curl -s -o /dev/null -w "%{http_code}\n" localhost:5080/api/v1/me        # 401
```

```bash
git add -A && git commit -m "feat(api): /me endpoints for profile, onboarding, saved, progress and sync

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 5: Mobile — device identity, auth session, authenticated HTTP

**Files:**
- Create: `TasteZambia.Core/Services/IDeviceIdentity.cs`, `Services/IAuthSession.cs`, `Services/AuthSession.cs`, `Data/Http/AuthenticatedHandler.cs`
- Create: `TasteZambia.Mobile/Services/SecureDeviceIdentity.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/Services/AuthenticatedHandlerTests.cs`

**Interfaces:**
- Produces:
  - `DeviceCredentials(string Id, string Secret)`
  - `IDeviceIdentity.GetOrCreateAsync(CancellationToken) → DeviceCredentials` — creates once, returns the same pair forever after
  - `IAuthSession`: `string? AccessToken`, `Task EnsureSignedInAsync(CancellationToken)`, `Task<bool> RefreshAsync(CancellationToken)`
  - `AuthenticatedHandler : DelegatingHandler` — attaches `Bearer`, and on a 401 refreshes once and retries once
- Consumes: `ApiRoutes.Auth.*`, `DeviceAuthRequest`, `RefreshRequest`, `AuthTokensDto`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Json;
using TasteZambia.Core.Data.Http;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public class AuthenticatedHandlerTests
{
    private sealed class FakeSession : IAuthSession
    {
        public string? AccessToken { get; set; } = "token-1";
        public int Refreshes;
        public Task EnsureSignedInAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<bool> RefreshAsync(CancellationToken ct) { Refreshes++; AccessToken = "token-2"; return Task.FromResult(true); }
    }

    /// <summary>Answers 401 to the first token it sees and 200 to any other.</summary>
    private sealed class ScriptedServer : HttpMessageHandler
    {
        public List<string?> SeenTokens { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var token = request.Headers.Authorization?.Parameter;
            SeenTokens.Add(token);
            return Task.FromResult(new HttpResponseMessage(token == "token-1" ? HttpStatusCode.Unauthorized : HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task AttachesTheBearerToken()
    {
        var session = new FakeSession(); var server = new ScriptedServer { };
        session.AccessToken = "token-2";
        var client = new HttpClient(new AuthenticatedHandler(session) { InnerHandler = server });

        var response = await client.GetAsync("http://api/anything");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["token-2"], server.SeenTokens);
    }

    [Fact]
    public async Task On401_RefreshesOnceAndRetriesOnce()
    {
        var session = new FakeSession(); var server = new ScriptedServer();
        var client = new HttpClient(new AuthenticatedHandler(session) { InnerHandler = server });

        var response = await client.GetAsync("http://api/anything");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, session.Refreshes);
        Assert.Equal(["token-1", "token-2"], server.SeenTokens);
    }

    [Fact]
    public async Task IfRefreshFails_The401IsReturnedNotLooped()
    {
        var session = new FakeSession(); var server = new ScriptedServer();
        var failing = new AuthenticatedHandler(new FailingSession()) { InnerHandler = server };
        var client = new HttpClient(failing);

        var response = await client.GetAsync("http://api/anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Single(server.SeenTokens);
    }

    private sealed class FailingSession : IAuthSession
    {
        public string? AccessToken => "token-1";
        public Task EnsureSignedInAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<bool> RefreshAsync(CancellationToken ct) => Task.FromResult(false);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~AuthenticatedHandlerTests`
Expected: FAIL.

- [ ] **Step 3: Write the Core interfaces and handler**

`TasteZambia.Core/Services/IDeviceIdentity.cs`:

```csharp
namespace TasteZambia.Core.Services;

public sealed record DeviceCredentials(string Id, string Secret);

/// <summary>
/// The anonymous account's credentials, generated once per install and kept in secure
/// storage. The same pair signs in the same account for as long as the app is installed.
/// </summary>
public interface IDeviceIdentity
{
    Task<DeviceCredentials> GetOrCreateAsync(CancellationToken ct = default);
}
```

`TasteZambia.Core/Services/IAuthSession.cs`:

```csharp
namespace TasteZambia.Core.Services;

public interface IAuthSession
{
    string? AccessToken { get; }

    /// <summary>Signs in with the device credentials if there is no usable token yet.</summary>
    Task EnsureSignedInAsync(CancellationToken ct = default);

    /// <summary>Rotates the refresh token. Returns false if the server rejected it, in which case the caller re-signs-in.</summary>
    Task<bool> RefreshAsync(CancellationToken ct = default);
}
```

`TasteZambia.Core/Services/AuthSession.cs`:

```csharp
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Services;

/// <summary>
/// Holds the current tokens. The refresh token lives in secure storage through
/// <see cref="ILocalStore"/>'s secure sibling; the access token is memory-only.
/// Uses a plain HttpClient with NO AuthenticatedHandler - auth calls must never
/// recurse into the handler that depends on them.
/// </summary>
public sealed class AuthSession(HttpClient authClient, IDeviceIdentity device, ISecureStore secure) : IAuthSession
{
    private const string RefreshKey = "auth.refresh";
    private readonly SemaphoreSlim _gate = new(1, 1);

    public string? AccessToken { get; private set; }
    private DateTimeOffset _accessExpires;

    public async Task EnsureSignedInAsync(CancellationToken ct = default)
    {
        if (AccessToken is not null && _accessExpires > DateTimeOffset.UtcNow.AddMinutes(1))
            return;

        await _gate.WaitAsync(ct);
        try
        {
            if (AccessToken is not null && _accessExpires > DateTimeOffset.UtcNow.AddMinutes(1))
                return;

            // Prefer a refresh: it does not need the device secret over the wire.
            if (await secure.GetAsync(RefreshKey) is { } refresh && await TryRefreshAsync(refresh, ct))
                return;

            var creds = await device.GetOrCreateAsync(ct);
            var response = await authClient.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest(creds.Id, creds.Secret), ct);
            response.EnsureSuccessStatusCode();
            await AdoptAsync((await response.Content.ReadFromJsonAsync<AuthTokensDto>(ct))!);
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var refresh = await secure.GetAsync(RefreshKey);
            if (refresh is not null && await TryRefreshAsync(refresh, ct)) return true;

            // Refresh rejected (rotated elsewhere, expired). Fall back to a full sign-in.
            AccessToken = null;
            await EnsureSignedInAsyncUnlocked(ct);
            return AccessToken is not null;
        }
        catch (HttpRequestException) { return false; }
        finally { _gate.Release(); }
    }

    private async Task EnsureSignedInAsyncUnlocked(CancellationToken ct)
    {
        var creds = await device.GetOrCreateAsync(ct);
        var response = await authClient.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest(creds.Id, creds.Secret), ct);
        if (!response.IsSuccessStatusCode) return;
        await AdoptAsync((await response.Content.ReadFromJsonAsync<AuthTokensDto>(ct))!);
    }

    private async Task<bool> TryRefreshAsync(string refresh, CancellationToken ct)
    {
        var response = await authClient.PostAsJsonAsync(ApiRoutes.Auth.Refresh, new RefreshRequest(refresh), ct);
        if (!response.IsSuccessStatusCode) return false;
        await AdoptAsync((await response.Content.ReadFromJsonAsync<AuthTokensDto>(ct))!);
        return true;
    }

    private async Task AdoptAsync(AuthTokensDto tokens)
    {
        AccessToken = tokens.AccessToken;
        _accessExpires = tokens.AccessExpiresAt;
        await secure.SetAsync(RefreshKey, tokens.RefreshToken);
    }
}
```

This needs one more Core interface, `ISecureStore` (string get/set/remove) — add it to `IDeviceIdentity.cs`:

```csharp
/// <summary>Secure key-value storage. Implemented over SecureStorage on device.</summary>
public interface ISecureStore
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    void Remove(string key);
}
```

`TasteZambia.Core/Data/Http/AuthenticatedHandler.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Data.Http;

/// <summary>
/// Puts the bearer token on every request. On a 401, refreshes once and retries once;
/// a second 401 is returned as-is. Never loops.
/// </summary>
public sealed class AuthenticatedHandler(IAuthSession session) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await session.EnsureSignedInAsync(ct);
        Attach(request);

        var response = await base.SendAsync(request, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        if (!await session.RefreshAsync(ct))
            return response;

        response.Dispose();
        var retry = await CloneAsync(request, ct);
        Attach(retry);
        return await base.SendAsync(retry, ct);
    }

    private void Attach(HttpRequestMessage request)
    {
        if (session.AccessToken is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage original, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (var h in original.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        if (original.Content is not null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync(ct);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var h in original.Content.Headers) clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        return clone;
    }
}
```

- [ ] **Step 4: Write the Mobile implementations and registrations**

`TasteZambia.Mobile/Services/SecureDeviceIdentity.cs`:

```csharp
using System.Security.Cryptography;
using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

public sealed class SecureStore : ISecureStore
{
    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);
    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);
    public void Remove(string key) => SecureStorage.Default.Remove(key);
}

public sealed class SecureDeviceIdentity(ISecureStore secure) : IDeviceIdentity
{
    private const string IdKey = "device.id", SecretKey = "device.secret";

    public async Task<DeviceCredentials> GetOrCreateAsync(CancellationToken ct = default)
    {
        var id = await secure.GetAsync(IdKey);
        var secret = await secure.GetAsync(SecretKey);
        if (id is not null && secret is not null)
            return new DeviceCredentials(id, secret);

        // Lowercase hex fits the server's [a-z0-9-] rule; 48 random bytes is well past the 32-char minimum.
        id = "device-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(12));
        secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await secure.SetAsync(IdKey, id);
        await secure.SetAsync(SecretKey, secret);
        return new DeviceCredentials(id, secret);
    }
}
```

In `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<ISecureStore, SecureStore>();
builder.Services.AddSingleton<IDeviceIdentity, SecureDeviceIdentity>();

// The auth client has NO AuthenticatedHandler - it must not depend on the session it creates.
builder.Services.AddHttpClient("auth", Api);
builder.Services.AddSingleton<IAuthSession>(sp => new AuthSession(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("auth"),
    sp.GetRequiredService<IDeviceIdentity>(),
    sp.GetRequiredService<ISecureStore>()));
builder.Services.AddTransient<AuthenticatedHandler>();

static void Api(HttpClient c) => c.BaseAddress = new Uri(ArchiveApiOptions.BaseUrl);
builder.Services.AddHttpClient<IDishRepository, HttpDishRepository>(Api).AddHttpMessageHandler<AuthenticatedHandler>();
// ... same .AddHttpMessageHandler<AuthenticatedHandler>() on the other four archive clients.
```

The archive endpoints are anonymous today, but attaching the handler now means the moment any of them requires auth nothing on the app side changes.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.Core.Tests`
Expected: PASS — 3 new, 108 prior.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(mobile): device identity, auth session and authenticated HTTP handler

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 6: Mobile — onboarding persists, locally and to the account

**Files:**
- Create: `TasteZambia.Core/Services/ILocalStore.cs`, `TasteZambia.Mobile/Services/PreferencesLocalStore.cs`
- Rewrite: `TasteZambia.Core/Services/OnboardingService.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`, `TasteZambia.Mobile/App.xaml.cs`
- Test: `TasteZambia.Core.Tests/Services/OnboardingPersistenceTests.cs`; update `OnboardingTests.cs`

**Interfaces:**
- Produces:
  - `ILocalStore`: `T? Get<T>(string key)`, `void Set<T>(string key, T value)`, `void Remove(string key)` — JSON documents by key
  - `OnboardingService(ILocalStore local, HttpClient api)` — same `IOnboardingService` surface; `IsComplete` and `Current` read from the local store on construction; `Complete()` writes local **first**, then fires-and-forgets `PUT /me/onboarding`
  - `InMemoryLocalStore` in tests

- [ ] **Step 1: Write the failing test**

```csharp
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public sealed class InMemoryLocalStore : ILocalStore
{
    private readonly Dictionary<string, string> _docs = [];
    public T? Get<T>(string key) => _docs.TryGetValue(key, out var json) ? System.Text.Json.JsonSerializer.Deserialize<T>(json) : default;
    public void Set<T>(string key, T value) => _docs[key] = System.Text.Json.JsonSerializer.Serialize(value);
    public void Remove(string key) => _docs.Remove(key);
}

public class OnboardingPersistenceTests
{
    private static HttpClient NoNetwork() => new(new HttpClientHandler()) { BaseAddress = new Uri("http://localhost:1") };

    [Fact]
    public void FreshStore_IsNotComplete()
    {
        var sut = new OnboardingService(new InMemoryLocalStore(), NoNetwork());
        Assert.False(sut.IsComplete);
        Assert.Equal("English", sut.Current.Language);
    }

    [Fact]
    public void Complete_SurvivesANewServiceInstance()
    {
        var store = new InMemoryLocalStore();
        var first = new OnboardingService(store, NoNetwork());
        first.Complete(new OnboardingChoices { Language = "Bemba", Who = "I live abroad", Tastes = ["quick"] });

        var second = new OnboardingService(store, NoNetwork());   // a relaunch

        Assert.True(second.IsComplete);
        Assert.Equal("Bemba", second.Current.Language);
        Assert.Equal(["quick"], second.Current.Tastes);
    }

    [Fact]
    public void Complete_DoesNotThrowWhenTheServerIsUnreachable()
    {
        var sut = new OnboardingService(new InMemoryLocalStore(), NoNetwork());
        var ex = Record.Exception(() => sut.Complete(new OnboardingChoices()));
        Assert.Null(ex);
        Assert.True(sut.IsComplete);   // local first: the server being down changes nothing
    }
}
```

Existing `OnboardingTests.cs` constructs `new OnboardingService()` — update every call to `new OnboardingService(new InMemoryLocalStore(), NoNetwork())`.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~Onboarding`
Expected: FAIL — `ILocalStore` does not exist.

- [ ] **Step 3: Write `ILocalStore` and rewrite the service**

`TasteZambia.Core/Services/ILocalStore.cs`:

```csharp
namespace TasteZambia.Core.Services;

/// <summary>
/// Small JSON documents by key. Backed by Preferences on device. If it ever needs to
/// hold more than a few hundred rows, the replacement is SQLite behind this same interface.
/// </summary>
public interface ILocalStore
{
    T? Get<T>(string key);
    void Set<T>(string key, T value);
    void Remove(string key);
}
```

`OnboardingService.cs` — keep the three option lists exactly as they are; replace the state:

```csharp
public sealed class OnboardingService : IOnboardingService
{
    private const string Key = "onboarding";
    private readonly ILocalStore _local;
    private readonly HttpClient _api;

    public OnboardingService(ILocalStore local, HttpClient api)
    {
        _local = local;
        _api = api;
        var stored = local.Get<StoredOnboarding>(Key);
        IsComplete = stored?.IsComplete ?? false;
        Current = stored?.Choices ?? new OnboardingChoices();
    }

    public bool IsComplete { get; private set; }
    public OnboardingChoices Current { get; private set; }

    /// <summary>Local first, always. The account copy is best-effort and never blocks.</summary>
    public void Complete(OnboardingChoices choices)
    {
        Current = choices;
        IsComplete = true;
        _local.Set(Key, new StoredOnboarding(true, choices));

        _ = PushAsync(choices);
    }

    private async Task PushAsync(OnboardingChoices c)
    {
        try
        {
            await _api.PutAsJsonAsync(ApiRoutes.Me.Onboarding,
                new OnboardingChoicesDto(c.Language, c.Who, [.. c.Tastes], c.OfflineEnabled, c.StoryNotifications, true));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Offline. The local copy is authoritative for this device; SyncScheduler retries later.
        }
    }

    private sealed record StoredOnboarding(bool IsComplete, OnboardingChoices Choices);

    // Languages / VisitorKinds / Tastes unchanged.
}
```

`OnboardingChoices` is a class with `HashSet<string> Tastes` — `System.Text.Json` round-trips it. Confirm with the persistence test.

- [ ] **Step 4: Write the Mobile store and register**

`TasteZambia.Mobile/Services/PreferencesLocalStore.cs`:

```csharp
using System.Text.Json;
using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

public sealed class PreferencesLocalStore : ILocalStore
{
    private const string Prefix = "tz.";

    public T? Get<T>(string key)
    {
        var json = Preferences.Default.Get<string?>(Prefix + key, null);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public void Set<T>(string key, T value) => Preferences.Default.Set(Prefix + key, JsonSerializer.Serialize(value));
    public void Remove(string key) => Preferences.Default.Remove(Prefix + key);
}
```

In `MauiProgram.cs`, replace the onboarding registration:

```csharp
builder.Services.AddSingleton<ILocalStore, PreferencesLocalStore>();
builder.Services.AddHttpClient("me", Api).AddHttpMessageHandler<AuthenticatedHandler>();
builder.Services.AddSingleton<IOnboardingService>(sp => new OnboardingService(
    sp.GetRequiredService<ILocalStore>(),
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("me")));
```

`App.CreateWindow` already branches on `IsComplete`. Nothing else changes: **onboarding no longer repeats on every launch.**

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.Core.Tests`
Expected: PASS.

- [ ] **Step 6: Verify on device and commit**

Deploy. Walk onboarding to "Start exploring". Force-stop and relaunch: **Home, not Splash.** Then with the API running:

```bash
docker exec tastezambia-db psql -U tastezambia -d tastezambia -tAc \
  'select "Language","Who","IsComplete" from onboarding_choices;'
```
shows the row the phone pushed.

```bash
git add -A && git commit -m "feat(mobile): onboarding persists locally and to the account

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 7: Mobile — offline-first favourites and cook progress with sync

**Files:**
- Create: `TasteZambia.Core/Services/PersonalStore.cs`, `Services/PersonalSyncService.cs`, `TasteZambia.Mobile/Services/SyncScheduler.cs`
- Rewrite: `TasteZambia.Core/Services/FavouritesService.cs`, `Services/CookingProgressService.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`, `App.xaml.cs`
- Test: `TasteZambia.Core.Tests/Services/PersonalStoreTests.cs`; update `ServiceTests.cs` constructors

**Interfaces:**
- Produces:
  - `PersonalState { Dictionary<string, SavedEntry> Saved; Dictionary<string, ProgressEntry> Progress; List<SyncChangeDto> Outbox; }` with `SavedEntry(bool IsSaved, DateTimeOffset At)` and `ProgressEntry(bool IsDone, DateTimeOffset At)` keyed `"{dishId}"` and `"{dishId}:{step}"`
  - `PersonalStore(ILocalStore local, TimeProvider clock)`: `bool IsSaved(dishId)`, `void SetSaved(dishId, bool)`, `bool IsDone(dishId, step)`, `void SetDone(dishId, step, bool)`, `IReadOnlyList<SyncChangeDto> DrainOutbox()`, `void Apply(SyncResponse)`, `event EventHandler<string>? Changed`
  - `FavouritesService(PersonalStore store)` and `CookingProgressService(PersonalStore store)` — same public interfaces as today, delegating
  - `IPersonalSyncService.SyncAsync(CancellationToken) → bool` (Core): drains the outbox, `POST /me/sync`, applies the response; on failure re-queues and returns false
- Consumes: `ILocalStore`, `SyncChangeDto`, `SyncRequest`, `SyncResponse`, `ApiRoutes.Me.Sync`

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.Extensions.Time.Testing;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Contracts.Me;

namespace TasteZambia.Core.Tests.Services;

public class PersonalStoreTests
{
    private static (PersonalStore store, FakeTimeProvider clock) Sut(ILocalStore? local = null)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero));
        return (new PersonalStore(local ?? new InMemoryLocalStore(), clock), clock);
    }

    [Fact]
    public void IfisashiIsSavedByDefault_MatchingTheDesign()
    {
        var (store, _) = Sut();
        Assert.True(store.IsSaved("ifisashi"));
        Assert.False(store.IsSaved("chikanda"));
    }

    [Fact]
    public void SetSaved_WritesLocallyAndQueuesAStampedChange()
    {
        var (store, clock) = Sut();
        store.SetSaved("chikanda", true);

        Assert.True(store.IsSaved("chikanda"));
        var change = Assert.Single(store.DrainOutbox());
        Assert.Equal(SyncChangeDto.Saved, change.Kind);
        Assert.Equal("chikanda", change.DishId);
        Assert.True(change.Value);
        Assert.Equal(clock.GetUtcNow(), change.At);
    }

    [Fact]
    public void State_SurvivesANewStoreOverTheSameLocalStore()
    {
        var local = new InMemoryLocalStore();
        var (first, _) = Sut(local);
        first.SetDone("ifisashi", 2, true);

        var (second, _) = Sut(local);
        Assert.True(second.IsDone("ifisashi", 2));
        Assert.Single(second.DrainOutbox());   // the outbox persisted too
    }

    [Fact]
    public void Apply_ReplacesLocalStateWithTheServers()
    {
        var (store, clock) = Sut();
        store.SetSaved("chikanda", true);
        store.DrainOutbox();

        store.Apply(new SyncResponse(
            [new("delele", true, clock.GetUtcNow()), new("ifisashi", false, clock.GetUtcNow())],
            [new("nshima", 1, true, clock.GetUtcNow())],
            clock.GetUtcNow()));

        Assert.True(store.IsSaved("delele"));
        Assert.False(store.IsSaved("ifisashi"));   // server said unsaved; server is the merged truth
        Assert.False(store.IsSaved("chikanda"));   // not in the response: gone
        Assert.True(store.IsDone("nshima", 1));
    }

    [Fact]
    public void Apply_DoesNotDiscardChangesMadeAfterTheDrain()
    {
        var (store, clock) = Sut();
        var drained = store.DrainOutbox();
        store.SetSaved("kapenta", true);   // user tapped while the request was in flight

        store.Apply(new SyncResponse([], [], clock.GetUtcNow()));

        Assert.True(store.IsSaved("kapenta"));      // local change kept ...
        Assert.Single(store.DrainOutbox());         // ... and still queued for the next sync
    }

    [Fact]
    public void Changed_FiresWithTheDishId()
    {
        var (store, _) = Sut();
        string? raised = null;
        store.Changed += (_, id) => raised = id;
        store.SetDone("delele", 3, true);
        Assert.Equal("delele", raised);
    }
}
```

Add `Microsoft.Extensions.TimeProvider.Testing` to `TasteZambia.Core.Tests`.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FullyQualifiedName~PersonalStoreTests`
Expected: FAIL.

- [ ] **Step 3: Write `PersonalStore`**

```csharp
using TasteZambia.Shared.Contracts.Me;

namespace TasteZambia.Core.Services;

public sealed record SavedEntry(bool IsSaved, DateTimeOffset At);
public sealed record ProgressEntry(bool IsDone, DateTimeOffset At);

public sealed class PersonalState
{
    public Dictionary<string, SavedEntry> Saved { get; set; } = [];
    public Dictionary<string, ProgressEntry> Progress { get; set; } = [];
    public List<SyncChangeDto> Outbox { get; set; } = [];
}

/// <summary>
/// The device's copy of the account's personal data, plus an outbox of changes not yet
/// acknowledged by the server. Every write lands here first and is stamped with the
/// clock at the moment of the tap; that stamp is what the server compares on sync.
/// </summary>
public sealed class PersonalStore
{
    private const string Key = "personal";
    private readonly ILocalStore _local;
    private readonly TimeProvider _clock;
    private PersonalState _state;

    public event EventHandler<string>? Changed;

    public PersonalStore(ILocalStore local, TimeProvider clock)
    {
        _local = local;
        _clock = clock;
        _state = local.Get<PersonalState>(Key) ?? Seed();
    }

    // The design's starting state: ifisashi saved. Stamped at epoch so any real tap outranks it.
    private static PersonalState Seed() => new()
    {
        Saved = { ["ifisashi"] = new SavedEntry(true, DateTimeOffset.UnixEpoch) },
    };

    public bool IsSaved(string dishId) => _state.Saved.TryGetValue(dishId, out var e) && e.IsSaved;
    public bool IsDone(string dishId, int step) => _state.Progress.TryGetValue($"{dishId}:{step}", out var e) && e.IsDone;

    public void SetSaved(string dishId, bool isSaved)
    {
        var at = _clock.GetUtcNow();
        _state.Saved[dishId] = new SavedEntry(isSaved, at);
        _state.Outbox.Add(new SyncChangeDto(SyncChangeDto.Saved, dishId, null, isSaved, at));
        Persist(dishId);
    }

    public void SetDone(string dishId, int step, bool isDone)
    {
        var at = _clock.GetUtcNow();
        _state.Progress[$"{dishId}:{step}"] = new ProgressEntry(isDone, at);
        _state.Outbox.Add(new SyncChangeDto(SyncChangeDto.Progress, dishId, step, isDone, at));
        Persist(dishId);
    }

    /// <summary>Takes everything queued. The caller re-queues on failure via <see cref="Requeue"/>.</summary>
    public IReadOnlyList<SyncChangeDto> DrainOutbox()
    {
        var batch = _state.Outbox.ToList();
        _state.Outbox.Clear();
        _local.Set(Key, _state);
        return batch;
    }

    public void Requeue(IReadOnlyList<SyncChangeDto> batch)
    {
        _state.Outbox.InsertRange(0, batch);
        _local.Set(Key, _state);
    }

    /// <summary>
    /// Adopts the server's merged state. Anything the user did AFTER the drain is still
    /// in the outbox and is re-applied on top, so an in-flight tap is never lost.
    /// </summary>
    public void Apply(SyncResponse server)
    {
        var pending = _state.Outbox.ToList();

        _state.Saved = server.Saved.ToDictionary(s => s.DishId, s => new SavedEntry(s.IsSaved, s.UpdatedAt));
        _state.Progress = server.Progress.ToDictionary(p => $"{p.DishId}:{p.StepNumber}", p => new ProgressEntry(p.IsDone, p.UpdatedAt));

        foreach (var c in pending)
        {
            if (c.Kind == SyncChangeDto.Saved) _state.Saved[c.DishId] = new SavedEntry(c.Value, c.At);
            else if (c.Step is { } step) _state.Progress[$"{c.DishId}:{step}"] = new ProgressEntry(c.Value, c.At);
        }

        _local.Set(Key, _state);
        Changed?.Invoke(this, "");
    }

    private void Persist(string dishId)
    {
        _local.Set(Key, _state);
        Changed?.Invoke(this, dishId);
    }
}
```

- [ ] **Step 4: Rewrite the two services over it**

`FavouritesService.cs`:

```csharp
public sealed class FavouritesService(PersonalStore store) : IFavouritesService
{
    public event EventHandler<string>? Changed
    {
        add => store.Changed += value;
        remove => store.Changed -= value;
    }

    public bool IsSaved(string dishId) => store.IsSaved(dishId);
    public void Toggle(string dishId) => store.SetSaved(dishId, !store.IsSaved(dishId));
}
```

`CookingProgressService.cs`:

```csharp
public sealed class CookingProgressService(PersonalStore store) : ICookingProgressService
{
    public event EventHandler<string>? Changed
    {
        add => store.Changed += value;
        remove => store.Changed -= value;
    }

    public bool IsDone(string dishId, int stepNumber) => store.IsDone(dishId, stepNumber);
    public void Toggle(string dishId, int stepNumber) => store.SetDone(dishId, stepNumber, !store.IsDone(dishId, stepNumber));
    public int CompletedCount(string dishId, IEnumerable<int> stepNumbers) => stepNumbers.Count(s => store.IsDone(dishId, s));
}
```

Update every test that does `new FavouritesService()` / `new CookingProgressService()` to pass `new PersonalStore(new InMemoryLocalStore(), TimeProvider.System)`. Their assertions are unchanged — the interfaces did not move.

- [ ] **Step 5: Write the sync service and scheduler**

`TasteZambia.Core/Services/PersonalSyncService.cs`:

```csharp
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Services;

public interface IPersonalSyncService
{
    /// <summary>Pushes queued changes and adopts the server's state. False means it will be retried.</summary>
    Task<bool> SyncAsync(CancellationToken ct = default);
}

public sealed class PersonalSyncService(PersonalStore store, HttpClient api) : IPersonalSyncService
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<bool> SyncAsync(CancellationToken ct = default)
    {
        if (!await _gate.WaitAsync(0, ct)) return false;   // one at a time; the next scheduler tick catches up
        var batch = store.DrainOutbox();
        try
        {
            var response = await api.PostAsJsonAsync(ApiRoutes.Me.Sync, new SyncRequest(batch), ct);
            if (!response.IsSuccessStatusCode) { store.Requeue(batch); return false; }
            store.Apply((await response.Content.ReadFromJsonAsync<SyncResponse>(ct))!);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            store.Requeue(batch);
            return false;
        }
        finally { _gate.Release(); }
    }
}
```

`TasteZambia.Mobile/Services/SyncScheduler.cs`:

```csharp
using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

/// <summary>Syncs on launch, on resume, and two seconds after the last local change.</summary>
public sealed class SyncScheduler(IPersonalSyncService sync, PersonalStore store) : IDisposable
{
    private CancellationTokenSource? _debounce;

    public void Start()
    {
        store.Changed += OnChanged;
        _ = sync.SyncAsync();
    }

    public void OnResumed() => _ = sync.SyncAsync();

    private void OnChanged(object? sender, string _)
    {
        _debounce?.Cancel();
        var cts = _debounce = new CancellationTokenSource();
        _ = Task.Delay(TimeSpan.FromSeconds(2), cts.Token).ContinueWith(
            t => { if (!t.IsCanceled) _ = sync.SyncAsync(); }, TaskScheduler.Default);
    }

    public void Dispose() { store.Changed -= OnChanged; _debounce?.Cancel(); }
}
```

Register in `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<PersonalStore>(sp => new PersonalStore(sp.GetRequiredService<ILocalStore>(), TimeProvider.System));
builder.Services.AddSingleton<IFavouritesService, FavouritesService>();
builder.Services.AddSingleton<ICookingProgressService, CookingProgressService>();
builder.Services.AddSingleton<IPersonalSyncService>(sp => new PersonalSyncService(
    sp.GetRequiredService<PersonalStore>(),
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("me")));
builder.Services.AddSingleton<SyncScheduler>();
```

In `App.CreateWindow`, after the window is built: `_services.GetRequiredService<SyncScheduler>().Start();` and override `OnResume()` to call `OnResumed()`.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.Core.Tests`
Expected: PASS.

- [ ] **Step 7: Verify offline-first on device and commit**

1. With the API up, deploy, heart Chikanda. Within ~2 s: `psql ... -c 'select "DishId","IsSaved" from saved_dishes;'` shows it.
2. **Stop the API.** Heart Delele. The heart fills instantly, no error.
3. Force-stop and relaunch the app: Delele is still hearted (local store).
4. Start the API. Within seconds of resume, `saved_dishes` shows Delele.

```bash
git add -A && git commit -m "feat(mobile): offline-first favourites and cook progress with account sync

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 8: Mobile — Profile from the account

**Files:**
- Create: `TasteZambia.Core/Data/Http/HttpProfileRepository.cs`
- Modify: `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.API.Tests/Features/RepositoryParityTests.cs` (one more)

**Interfaces:**
- Produces: `HttpProfileRepository(HttpClient me, IFavouritesService favourites, ICookingProgressService progress) : IProfileRepository`
  - `GetAsync` → `GET /me`, mapped to `UserProfile`; counts come from the local `PersonalStore` (favourites = saved count, cooked = distinct dishes with any step done). `ContributedCount` and `PreservedCount` stay `3` and `4` until Stage 3/4.
  - `GetCollectionsAsync` → the four `RecipeCollection`s with **live counts** ("{n} recipes")
  - `GetContributionsAsync` → still `SeedData.Contributions` (Stage 3)

- [ ] **Step 1: Write the failing test**

Append to `RepositoryParityTests.cs`:

```csharp
[Fact]
public async Task Profile_ReadsTheAccountAndCountsFromTheLocalStore()
{
    var store = new PersonalStore(new TasteZambia.Core.Tests.Services.InMemoryLocalStore(), TimeProvider.System);
    store.SetSaved("chikanda", true);            // ifisashi + chikanda = 2 saved
    store.SetDone("nshima", 1, true);

    var tokens = await (await _client.PostAsJsonAsync(ApiRoutes.Auth.Device,
        new DeviceAuthRequest($"device-{Guid.NewGuid():N}", new string('s', 40)))).Content.ReadFromJsonAsync<AuthTokensDto>();
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
    await _client.PutAsJsonAsync(ApiRoutes.Me.Profile, new UpdateProfileRequest("Chanda Mwaba", "Kitwe, Copperbelt", "Bemba, English"));

    var repo = new HttpProfileRepository(_client, new FavouritesService(store), new CookingProgressService(store));
    var profile = await repo.GetAsync();

    Assert.Equal("Chanda Mwaba", profile.Name);
    Assert.Equal(2, profile.FavouriteCount);
    Assert.Equal(1, profile.CookedCount);

    var collections = await repo.GetCollectionsAsync();
    Assert.Equal("2 recipes", collections[0].CountLabel);
}
```

The API test project needs a reference to `TasteZambia.Core.Tests` for `InMemoryLocalStore`, or move `InMemoryLocalStore` into `TasteZambia.Core` as a public `InMemoryLocalStore` (it is useful at runtime too, for previews). **Move it into Core** — `TasteZambia.Core/Services/InMemoryLocalStore.cs` — and delete the test copy.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter Profile_ReadsTheAccount`
Expected: FAIL.

- [ ] **Step 3: Write the repository**

```csharp
using System.Net.Http.Json;
using TasteZambia.Core.Data;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Data.Http;

public sealed class HttpProfileRepository(HttpClient me, PersonalStore store) : IProfileRepository
{
    private static readonly string[] AllDishIds = ["ifisashi","nshima","chikanda","kapenta","inkoko","kandolo","munkoyo","delele"];

    public async Task<UserProfile> GetAsync(CancellationToken ct = default)
    {
        var p = await me.GetFromJsonAsync<ProfileDto>(ApiRoutes.Me.Profile, ct) ?? new ProfileDto("", "", "", null);

        return new UserProfile
        {
            Name = p.DisplayName.Length > 0 ? p.DisplayName : "Taste Zambia reader",
            Location = p.Location,
            Languages = p.Languages,
            AvatarAsset = p.AvatarAsset ?? "avatar_chanda.png",
            FavouriteCount = SavedCount(),
            CookedCount = CookedCount(),
            ContributedCount = 3,   // Stage 3
            PreservedCount = 4,     // Stage 4
        };
    }

    public Task<IReadOnlyList<RecipeCollection>> GetCollectionsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<RecipeCollection>>(
        [
            new("My Favourite Zambian Foods", Plural(SavedCount(), "recipe"), "#A3452A"),
            new("Recipes I Want to Try",      "9 recipes",                    "#C07F1E"),
            new("Recipes I've Cooked",        Plural(CookedCount(), "recipe"), "#2F6A4D"),
            new("My Family Recipes",          "4 preserved",                  "#17402F"),
        ]);

    public Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Contributions);

    private int SavedCount() => AllDishIds.Count(store.IsSaved);
    private int CookedCount() => AllDishIds.Count(d => Enumerable.Range(1, 12).Any(s => store.IsDone(d, s)));
    private static string Plural(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";
}
```

Adjust the test to construct `new HttpProfileRepository(_client, store)`.

Register: `builder.Services.AddHttpClient<IProfileRepository, HttpProfileRepository>(Api).AddHttpMessageHandler<AuthenticatedHandler>();` — and remove the `InMemoryProfileRepository` registration. `HttpProfileRepository` also needs `PersonalStore` injected; typed HttpClient registration resolves extra constructor parameters from DI.

- [ ] **Step 4: Run the tests and verify on device**

Run: `dotnet test TasteZambia.API.Tests && dotnet test TasteZambia.Core.Tests`
Expected: PASS.

Deploy. Profile shows the counts moving as you heart dishes and tick steps. The name is "Taste Zambia reader" until Stage 3 gives the profile an edit screen.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(mobile): profile reads the account, counts from the local store

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage.** Stage 2 in the backend plan asked for: Identity + JWT + refresh rotation (Task 1), roles (Task 1), `UserProfile`/`SavedDish`/`CookedEntry`/`OnboardingChoices` tables (Task 3), the `/auth/*` and `/me/*` endpoints (Tasks 2, 4), and making `IFavouritesService`/`ICookingProgressService` real (Task 7), unblocking mobile Profile (Task 8). `WishlistEntry` is deliberately **not** built: nothing in the app can add to the wishlist yet, so a table for it is YAGNI; "Want to Try" keeps its seeded count until a screen writes to it. The two recorded decisions — anonymous accounts, offline-first — shape Tasks 5–7.

**Placeholder scan.** None. Every step has its code. "Stage 3"/"Stage 4" comments mark counts that later stages replace, with the interim value stated.

**Type consistency.** `SyncChangeDto`'s `Kind` constants (`"saved"`, `"progress"`) are used by name in Tasks 3, 4 and 7. `OnboardingChoices.Tastes` is `HashSet<string>` in Core and comma-joined `string` in the API entity; the DTO carries `IReadOnlyList<string>` and both sides convert at their edge. `PersonalStore` is registered as a concrete singleton and both services and the sync service take it directly — no interface, because nothing needs to substitute it except tests, which construct it over `InMemoryLocalStore`.

**Three risks for the executor.**
1. **`IdentityDbContext` reorders `OnModelCreating`.** If `base.OnModelCreating` stays after `ApplyConfigurationsFromAssembly`, the migration silently omits the Identity tables and every auth test fails with "relation AspNetUsers does not exist".
2. **The auth `HttpClient` must not carry `AuthenticatedHandler`.** The handler calls `EnsureSignedInAsync`, which calls the auth client; if that client also has the handler, sign-in recurses. Task 5 names the client `"auth"` and registers it without the handler for this reason.
3. **`SecureStorage` on Android needs no extra setup, but on a device that has never unlocked, `SetAsync` can throw.** `SecureDeviceIdentity` is only called after the app is on screen, so this does not arise in practice; if it ever does, the exception surfaces through `LoadOnceView`'s log rather than crashing.
