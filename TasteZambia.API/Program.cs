using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TasteZambia.API.Common.Endpoints;
using TasteZambia.API.Data;
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
app.MapArchiveEndpoints();

app.Run();

// WebApplicationFactory needs a reachable entry point.
public partial class Program;
