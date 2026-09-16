using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TasteZambiaDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Archive")));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
// WebApplicationFactory needs a reachable entry point.
public partial class Program;
