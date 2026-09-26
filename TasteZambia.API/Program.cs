using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using TasteZambia.API.Auth;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Data.Seed;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.API.Media;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TasteZambiaDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Archive")));

// Scoped, not singleton - they hold a DbContext.
builder.Services.AddScoped<IDishRepository, DishRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<IRegionRepository, RegionRepository>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IArchiveVersionService, ArchiveVersionService>();
builder.Services.AddScoped<IPersonalDataRepository, PersonalDataRepository>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IPersonalSyncService, PersonalSyncService>();
builder.Services.AddScoped<IContributionRepository, ContributionRepository>();
builder.Services.AddScoped<IContributionService, ContributionService>();
builder.Services.AddScoped<IFamilyAccessService, FamilyAccessService>();
builder.Services.AddScoped<IFamilyRepository, FamilyRepository>();
builder.Services.AddScoped<IFamilyService, FamilyService>();
builder.Services.AddScoped<IMediaService, MediaService>();

// Identity and JWT. Accounts are anonymous and device-bound: the app registers a
// user whose name is a generated device id and whose password is a generated secret.
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.Section)
    .Validate(o => o.SigningKey is { Length: >= 32 }, "Jwt:SigningKey must be set and at least 32 characters.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IMediaStore>(_ =>
    new FileSystemMediaStore(builder.Configuration["Media:Root"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "media-dev")));
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
// Bound at runtime, not here: configuration layers added later (the test host's, for one)
// must still be able to supply the section.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((o, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Behind nginx-proxy: without this the app sees the proxy's address and http,
// so client IPs and generated absolute URLs are both wrong.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // The only hop is the proxy on this Docker network, whose address is not
    // known ahead of time; clearing these accepts it. Nothing else can reach
    // the container, because it publishes no host port.
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Production must not inherit a development secret. A signing key left at its
// sample value would let anyone who has read this repository mint tokens.
// Scoped to Production: the test host supplies its database directly rather
// than through a connection string, so a broader check fails the suite.
if (app.Environment.IsProduction())
{
    var jwt = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
    if (jwt.SigningKey.Contains("dev-only", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException(
            "Jwt__SigningKey is still the development value. Set a real one before starting in Production.");

    if (string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Archive")))
        throw new InvalidOperationException("ConnectionStrings__Archive is not set.");

    if (app.Configuration.GetSection("Reviewer").Exists())
        throw new InvalidOperationException(
            "The Reviewer section seeds an account with a known password. Remove it outside Development; "
            + "grant the role with PUT /api/v1/admin/users/{userName}/roles instead.");
}

// One-shot migrate-and-seed, run as a deliberate deploy step:
//   docker compose run --rm api dotnet TasteZambia.API.dll --migrate
// Never on ordinary startup outside Development - two instances scaling up
// would race each other into the same migration.
if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<TasteZambiaDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Migrate");

    log.LogInformation("Applying migrations");
    await db.Database.MigrateAsync();

    log.LogInformation("Seeding the editorial archive and roles");
    await ArchiveSeeder.SeedAsync(db);
    await RoleSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());

    log.LogInformation("Done");
    return;
}

// Development only. Production runs migrations as a deliberate deploy step -
// never let a scaling event race two instances into the same migration.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<TasteZambiaDbContext>();
    await db.Database.MigrateAsync();
    await ArchiveSeeder.SeedAsync(db);
    await RoleSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
    await ReviewerSeeder.SeedAsync(app.Configuration, scope.ServiceProvider.GetRequiredService<UserManager<ArchiveUser>>(), db);
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Interactive API reference at /scalar, generated from the OpenAPI document.
    app.MapScalarApiReference(o => o.WithTitle("Taste Zambia Archive API"));
}

// No UseHttpsRedirection: Kestrel serves HTTP inside the container and TLS
// terminates at the ingress. Leaving it on breaks container health checks.
app.UseAuthentication();
app.UseAuthorization();
// Liveness for the container and the proxy. Deliberately anonymous and cheap:
// it says the process is up and can reach its database, and nothing else.
app.MapGet("/health", async (TasteZambiaDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "healthy" })
        : Results.Problem("The archive database is unreachable.", statusCode: StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapControllers();

app.Run();

// WebApplicationFactory needs a reachable entry point.
public partial class Program;
