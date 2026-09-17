using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

// Identity and JWT. Accounts are anonymous and device-bound: the app registers a
// user whose name is a generated device id and whose password is a generated secret.
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.Section)
    .Validate(o => o.SigningKey is { Length: >= 32 }, "Jwt:SigningKey must be set and at least 32 characters.")
    .ValidateOnStart();
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

var app = builder.Build();

// Development only. Production runs migrations as a deliberate deploy step -
// never let a scaling event race two instances into the same migration.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<TasteZambiaDbContext>();
    await db.Database.MigrateAsync();
    await ArchiveSeeder.SeedAsync(db);
    await RoleSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
}

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
app.MapControllers();

app.Run();

// WebApplicationFactory needs a reachable entry point.
public partial class Program;
