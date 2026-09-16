# Taste Zambia Backend API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the Taste Zambia API so the mobile app can swap its seeded in-memory repositories for HTTP ones with no View, ViewModel or service changes — then grow into contributions, media and the family archive without rework.

**Architecture:** One EF Core schema. Horizontal layers underneath, vertical slices on top.
`Features/ → Services/ → Repositories/ → DbContext`. Dependencies point **down only**; a
feature never touches `DbContext` and no feature reaches sideways into another. That single
rule is what keeps the door open to splitting the schema or lifting a feature group into its
own host later, without editing callers.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core 10 + PostgreSQL, `TasteZambia.Shared` for contracts, xUnit + `WebApplicationFactory` for tests, Docker Compose for local infrastructure.

**Spec:** `Docs/Mobile app design project/Taste Zambia.dc.html` — the archive content the API serves.
**Companion:** `Docs/plans/2026-09-08-mobile-ui-implementation.md` — Task 3 there defines the six repository interfaces this API must satisfy.

---

## Staging

Scope is deliberately cut for v1. **Stage 1 is the whole deliverable of this plan** and is
specified task by task. Stages 2–5 are scoped, not stepped — they get their own plans once
Stage 1 is real.

| Stage | Delivers | Unblocks |
|---|---|---|
| **1 — Read-only archive** ← *this plan* | Every read endpoint, Postgres, seeded content, ETags | Mobile Tasks 6–14 run against a real API |
| 2 — Identity and the personal layer | Auth, profile, collections, favourites, cook progress | Mobile Tasks 17, 22 |
| 3 — Contributions and review | Draft → submit → review → changes → publish | Mobile Tasks 15, 20 |
| 4 — Media and the family archive | Photo/audio blobs, family access control | Mobile Tasks 16, 21 |
| 5 — Transcription and delta sync | Transcription queue + `?since=` cursors and tombstones | True offline |

### Why this is the right cut

Stage 1 serves **content that is public by definition** — a cultural archive. That means no
auth, no per-user state, no writes, and therefore no identity work standing between you and
a working API. It is also the majority of the app: nine of the eleven core screens plus
Regions and Culture are pure reads.

**The one thing pulled forward from Stage 5 is ETag support**, because it is nearly free now
(`ETag` + `If-None-Match` on every read) and expensive to retrofit. Full delta sync then
layers on top of the same version column rather than replacing anything.

---

## Global Constraints

### The dependency rule
```
Features/  →  Services/  →  Repositories/  →  Data/TasteZambiaDbContext
```
- A **Feature** owns one HTTP endpoint: its route, request binding, authorisation, and the mapping of a service result to a `Shared` DTO. It holds no business logic and never injects `DbContext`.
- A **Service** owns domain logic reusable across features — searching, filtering, ordering, rules. It never touches HTTP types.
- A **Repository** owns data access for one aggregate. It never applies business rules and never returns `IQueryable` past its own boundary.
- Reaching *up* a layer, or sideways between features, is a review failure.

### Contracts
- Every type crossing the wire lives in `TasteZambia.Shared/Contracts/`. Entities never leave the API.
- `TasteZambia.Shared` must not reference EF Core, ASP.NET or MAUI. It is already wired to both sides.
- Routes come from `ApiRoutes` — never a string literal in a feature.
- DTO changes are **additive**. Never remove or rename a field on `v1`; add a `v2` route instead. The mobile app compiles against these types, so a removal is a client build break.

### Data
- PostgreSQL 16 (`postgres:16-alpine`), one database, one schema, one `DbContext`.
  *Revised during execution:* the plan said 17, but 16-alpine was already local and the
  17 pull stalled; nothing in Stage 1 distinguishes them.
- **Host port is 5434, not 5432.** A native PostgreSQL 16 owns 5432 on this machine and
  another project's container owns 5433. Inside Compose the API still reaches `db:5432`.
- All ids are `string` slugs matching the design (`ifisashi`, `chibwabwa`) — they are stable, human-readable and already used by the mobile seed data.
- Every table carries `RowVersion` (`xmin` mapped as concurrency token) and `UpdatedAt` (`timestamptz`). These two columns are what Stage 5's delta sync will read; nothing else needs to change later.
- Content is seeded from the design canvas verbatim. **Do not paraphrase archive copy** — it is the cultural record.

### API conventions
- Minimal APIs, one endpoint per file, registered by assembly scan through `IEndpoint`.
- Errors are RFC 9457 `ProblemDetails`.
- Every collection response carries `ETag`; every handler honours `If-None-Match` and returns `304` on a match.
- All reads are `AsNoTracking`.
- Kestrel serves HTTP inside the container; TLS terminates at the ingress. `UseHttpsRedirection` is removed — it breaks container health checks.

### .NET 10 SDK gotcha
`dotnet sln add/remove` writes a stray newline between the UTF-8 BOM and the solution header,
which makes the IDE reject the solution. **After every `dotnet sln` mutation**, run:

```bash
python3 - <<'PY'
import pathlib
p = pathlib.Path("TasteZambia.sln"); b = p.read_bytes(); BOM = b"\xef\xbb\xbf"
for sep in (b"\r\n", b"\n"):
    if b.startswith(BOM + sep):
        p.write_bytes(BOM + b[len(BOM)+len(sep):]); print("repaired"); break
else: print("clean")
PY
```

---

## File Structure

### `TasteZambia.Shared` (exists)
| Path | Holds |
|---|---|
| `Enums/ArchiveEnums.cs` | `PrivacyLevel`, `ContributionStatus`, `ReviewState`, `TitleLanguage` *(done)* |
| `Routes/ApiRoutes.cs` | Every path *(done — extended in Task 2)* |
| `Validation/FieldLimits.cs` | Shared field limits *(done)* |
| `Contracts/Dishes/` | `DishDto`, `RecipeDto`, `RecipeIngredientDto`, `CookingStepDto`, `MethodNarrativeDto`, `RegionalVariationDto`, `ContributorDto` |
| `Contracts/Ingredients/` | `IngredientDto`, `LocalNameDto` |
| `Contracts/Regions/` | `ProvinceDto` |
| `Contracts/Culture/` | `ArticleDto`, `ArticleBlockDto`, `AudioNarrationDto` |
| `Contracts/Common/` | `CategoryDto`, `PagedResult<T>` |

### `TasteZambia.API`
| Path | Holds |
|---|---|
| `Data/TasteZambiaDbContext.cs` | The single context |
| `Data/Entities/` | `Dish`, `Recipe`, `RecipeIngredient`, `CookingStep`, `MethodNarrative`, `RegionalVariation`, `RegionalVariationEntry`, `Ingredient`, `IngredientLocalName`, `IngredientUsage`, `Province`, `ProvinceFood`, `ProvinceIngredient`, `Category`, `Article`, `ArticleBlock`, `ArticleRelatedDish` |
| `Data/Configurations/` | One `IEntityTypeConfiguration<T>` per entity |
| `Data/Seed/ArchiveSeeder.cs` | The design content, verbatim |
| `Repositories/` | `IDishRepository`, `IIngredientRepository`, `IRegionRepository`, `IArticleRepository`, `ICategoryRepository` + implementations |
| `Services/` | `ICatalogService`, `IArchiveVersionService` + implementations |
| `Features/Dishes/` | `GetDishes/`, `GetDishById/`, `GetRecipe/`, `SearchDishes/` |
| `Features/Ingredients/` | `GetIngredients/`, `GetIngredientByKey/` |
| `Features/Regions/` | `GetRegions/` |
| `Features/Culture/` | `GetArticles/`, `GetArticleById/` |
| `Features/Categories/` | `GetCategories/` |
| `Common/Endpoints/` | `IEndpoint`, `EndpointExtensions` |
| `Common/Http/` | `ETagResults`, `ProblemDetailsSetup` |
| `Program.cs` | Composition root |

### `TasteZambia.API.Tests` (new, `net10.0`, xUnit)
Integration tests over `WebApplicationFactory` against Postgres via Testcontainers, plus unit tests for `CatalogService`.

---

# Stage 1 — Read-only archive API

## Task 1: Infrastructure — Postgres, EF Core, test project

**Files:**
- Modify: `compose.yaml`, `TasteZambia.API/TasteZambia.API.csproj`, `TasteZambia.API/appsettings.Development.json`, `TasteZambia.sln`
- Create: `TasteZambia.API.Tests/TasteZambia.API.Tests.csproj`

**Interfaces:**
- Consumes: nothing.
- Produces: a running Postgres on `localhost:5432`, EF Core + Npgsql wired, a test project that can spin the API up.

- [ ] **Step 1: Add Postgres to compose**

Replace `compose.yaml`:

```yaml
services:
  db:
    image: postgres:16-alpine
    container_name: tastezambia-db
    environment:
      POSTGRES_DB: tastezambia
      POSTGRES_USER: tastezambia
      POSTGRES_PASSWORD: localdev
    ports:
      - "5434:5432"   # 5432 is the machine's native Postgres
    volumes:
      - tastezambia-pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U tastezambia -d tastezambia"]
      interval: 5s
      timeout: 5s
      retries: 10

  tastezambia.api:
    image: tastezambia.api
    build:
      context: .
      dockerfile: TasteZambia.API/Dockerfile
    depends_on:
      db:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Archive: "Host=db;Port=5432;Database=tastezambia;Username=tastezambia;Password=localdev"
    ports:
      - "8080:8080"

volumes:
  tastezambia-pgdata:
```

- [ ] **Step 2: Verify Postgres comes up**

Run: `docker compose up -d db && docker compose ps`
Expected: `tastezambia-db` reports `healthy` within ~15s. If Docker is not running, start it — the rest of this task depends on the database.

> `docker` is not on this shell's PATH; it lives at `/usr/local/bin/docker`. If the daemon is
> down, `open -a Docker` starts Docker Desktop.

- [ ] **Step 3: Add the packages**

```bash
cd "/Users/zimbadev/Documents/Workspace/Maui Projects/TasteZambia"
export PATH="/usr/local/share/dotnet:$PATH"
dotnet add TasteZambia.API package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add TasteZambia.API package Microsoft.EntityFrameworkCore.Design
dotnet tool install --global dotnet-ef   # skip if already installed
```

- [ ] **Step 4: Add the connection string**

`TasteZambia.API/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "ConnectionStrings": {
    "Archive": "Host=localhost;Port=5434;Database=tastezambia;Username=tastezambia;Password=localdev"
  }
}
```

The password is a local development credential only. Production supplies
`ConnectionStrings__Archive` through the environment — never commit a real one.

- [ ] **Step 5: Create the test project**

```bash
dotnet new xunit -n TasteZambia.API.Tests -o TasteZambia.API.Tests -f net10.0
rm -f TasteZambia.API.Tests/UnitTest1.cs
dotnet add TasteZambia.API.Tests reference TasteZambia.API TasteZambia.Shared
dotnet add TasteZambia.API.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add TasteZambia.API.Tests package Testcontainers.PostgreSql
dotnet sln TasteZambia.sln add TasteZambia.API.Tests/TasteZambia.API.Tests.csproj
```

Then **repair the solution header** — see the SDK gotcha in Global Constraints. Verify with:

```bash
xxd TasteZambia.sln | head -1     # must start ef bb bf 4d69 ("...Micr")
```

The API needs a public `Program` for `WebApplicationFactory`. Append to `Program.cs`:

```csharp
public partial class Program;
```

- [ ] **Step 6: Verify**

Run: `dotnet build TasteZambia.sln`
Expected: build succeeds, 0 errors. `dotnet test TasteZambia.API.Tests` reports 0 tests, exit 0.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "chore(api): add Postgres, EF Core and the API test project

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 2: Shared contracts

Every DTO the archive returns. These are the types the mobile app will bind to, so their
shape is a commitment.

**Files:**
- Create: `TasteZambia.Shared/Contracts/Common/CategoryDto.cs`, `Contracts/Dishes/DishContracts.cs`, `Contracts/Ingredients/IngredientContracts.cs`, `Contracts/Regions/ProvinceDto.cs`, `Contracts/Culture/ArticleContracts.cs`
- Modify: `TasteZambia.Shared/Routes/ApiRoutes.cs`
- Test: `TasteZambia.API.Tests/Contracts/SerializationTests.cs`

**Interfaces:**
- Produces: every DTO below. Field names are the JSON contract — `System.Text.Json` camel-cases them, so `LocalName` is `localName` on the wire.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Text.Json;
using TasteZambia.Shared.Contracts.Dishes;

namespace TasteZambia.API.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DishDto_RoundTripsAsCamelCase()
    {
        var dish = new DishDto
        {
            Id = "ifisashi",
            LocalName = "Ifisashi",
            EnglishName = "Groundnut and greens relish",
            Region = "Pan-Zambian",
            TimeLabel = "45 min",
            Difficulty = "Easy",
            Description = "Leafy greens simmered in pounded groundnuts.",
            ImageAsset = "ifisashi.png",
            PhotoNeededCaption = "photo needed",
        };

        var json = JsonSerializer.Serialize(dish, Web);

        Assert.Contains("\"localName\":\"Ifisashi\"", json);
        Assert.Contains("\"englishName\"", json);

        var back = JsonSerializer.Deserialize<DishDto>(json, Web)!;
        Assert.Equal("ifisashi", back.Id);
        Assert.Equal("Pan-Zambian", back.Region);
    }

    [Fact]
    public void DishDto_WithoutPhoto_SerialisesNullImageAsset()
    {
        var dish = new DishDto
        {
            Id = "kapenta", LocalName = "Kapenta", EnglishName = "Dried lake sardines",
            Region = "Luapula", TimeLabel = "25 min", Difficulty = "Easy",
            Description = "Small dried fish.", ImageAsset = null,
            PhotoNeededCaption = "photo: fried kapenta with tomato",
        };

        var back = JsonSerializer.Deserialize<DishDto>(JsonSerializer.Serialize(dish, Web), Web)!;

        Assert.Null(back.ImageAsset);
        Assert.Equal("photo: fried kapenta with tomato", back.PhotoNeededCaption);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~SerializationTests`
Expected: FAIL — `DishDto` does not exist.

- [ ] **Step 3: Write the dish contracts**

`TasteZambia.Shared/Contracts/Dishes/DishContracts.cs`:

```csharp
namespace TasteZambia.Shared.Contracts.Dishes;

public sealed record DishDto
{
    public required string Id { get; init; }
    public required string LocalName { get; init; }
    public required string EnglishName { get; init; }
    public required string Region { get; init; }
    public required string TimeLabel { get; init; }
    public required string Difficulty { get; init; }
    public required string Description { get; init; }

    /// <summary>Resource image name, or null while photography is still missing.</summary>
    public string? ImageAsset { get; init; }

    /// <summary>Caption for the striped placeholder shown when ImageAsset is null.</summary>
    public string PhotoNeededCaption { get; init; } = "photo needed";

    public string? PrepTime { get; init; }
    public string? CookTime { get; init; }
}

public sealed record ContributorDto(string Name, string Location, string? AvatarAsset);

public sealed record RecipeIngredientDto
{
    /// <summary>Set when this line links to an archived ingredient; null for plain items like Salt.</summary>
    public string? IngredientKey { get; init; }
    public required string DisplayName { get; init; }
    public required string DisplaySubtitle { get; init; }
    public required string Quantity { get; init; }
}

public sealed record CookingStepDto(int Number, string Title, string Body);

public sealed record MethodNarrativeDto(string Heading, IReadOnlyList<string> Paragraphs);

public sealed record RegionalVariationDto(string Place, string Description);

public sealed record RecipeDto
{
    public required DishDto Dish { get; init; }
    public required string Subtitle { get; init; }
    public bool IsVerified { get; init; }
    public required IReadOnlyList<string> CulturalContext { get; init; }
    public required IReadOnlyList<RecipeIngredientDto> Ingredients { get; init; }
    public required IReadOnlyList<CookingStepDto> Steps { get; init; }
    public required MethodNarrativeDto TraditionalMethod { get; init; }
    public required MethodNarrativeDto ModernMethod { get; init; }
    public required IReadOnlyList<RegionalVariationDto> Variations { get; init; }
    public required ContributorDto Contributor { get; init; }
}
```

- [ ] **Step 4: Write the remaining contracts**

`Contracts/Ingredients/IngredientContracts.cs`:

```csharp
namespace TasteZambia.Shared.Contracts.Ingredients;

public sealed record LocalNameDto(string Language, string Name);

public sealed record IngredientDto
{
    public required string Key { get; init; }
    public required string LocalName { get; init; }
    public required string EnglishName { get; init; }
    public required string Description { get; init; }
    public required string WhereFound { get; init; }
    public required string TraditionalPreparation { get; init; }
    public required IReadOnlyList<LocalNameDto> LocalNames { get; init; }

    /// <summary>Languages whose name for this ingredient is not yet recorded.</summary>
    public required string PendingLanguages { get; init; }

    public required IReadOnlyList<string> UsedInDishIds { get; init; }
    public string? ImageAsset { get; init; }
}
```

`Contracts/Regions/ProvinceDto.cs`:

```csharp
namespace TasteZambia.Shared.Contracts.Regions;

public sealed record ProvinceDto
{
    public required string Name { get; init; }
    public required string Seat { get; init; }
    public required string Blurb { get; init; }
    public required string CookingTradition { get; init; }
    public required IReadOnlyList<string> SignatureFoods { get; init; }
    public required IReadOnlyList<string> CommonIngredients { get; init; }
}
```

`Contracts/Culture/ArticleContracts.cs`:

```csharp
namespace TasteZambia.Shared.Contracts.Culture;

public enum ArticleBlockKind { Lede = 0, Paragraph = 1, PullQuote = 2 }

public sealed record ArticleBlockDto(ArticleBlockKind Kind, string Text);

public sealed record AudioNarrationDto(string Label, string Duration, double Progress);

public sealed record ArticleDto
{
    public required string Id { get; init; }
    public required string Kicker { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required string Meta { get; init; }
    public string? Lede { get; init; }
    public bool IsLead { get; init; }
    public string? ImageAsset { get; init; }
    public string PhotoNeededCaption { get; init; } = "photo needed";
    public IReadOnlyList<ArticleBlockDto> Body { get; init; } = [];
    public IReadOnlyList<string> RelatedDishIds { get; init; } = [];
    public AudioNarrationDto? Audio { get; init; }
}
```

`Contracts/Common/CategoryDto.cs`:

```csharp
namespace TasteZambia.Shared.Contracts.Common;

public sealed record CategoryDto(string Name, string? ImageAsset, int Order);
```

- [ ] **Step 5: Extend `ApiRoutes`**

Add to `ApiRoutes`:

```csharp
public static class Categories
{
    public const string Collection = $"{Root}/categories";
}
```

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~SerializationTests`
Expected: PASS, 2 tests.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(shared): add archive read contracts

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 3: Entities, DbContext and the migration

**Files:**
- Create: `TasteZambia.API/Data/Entities/*.cs`, `Data/Configurations/*.cs`, `Data/TasteZambiaDbContext.cs`
- Modify: `TasteZambia.API/Program.cs`
- Test: `TasteZambia.API.Tests/Data/SchemaTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `TasteZambiaDbContext` with `DbSet`s for `Dishes`, `Recipes`, `Ingredients`, `Provinces`, `Categories`, `Articles`; every entity carrying `UpdatedAt` and a concurrency token.

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;

namespace TasteZambia.API.Tests.Data;

public class SchemaTests
{
    private static TasteZambiaDbContext InMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TasteZambiaDbContext>()
            .UseInMemoryDatabase($"schema-{Guid.NewGuid()}")
            .Options;
        return new TasteZambiaDbContext(options);
    }

    [Fact]
    public void Context_ExposesEverySetTheArchiveNeeds()
    {
        using var db = InMemoryContext();

        Assert.NotNull(db.Dishes);
        Assert.NotNull(db.Recipes);
        Assert.NotNull(db.Ingredients);
        Assert.NotNull(db.Provinces);
        Assert.NotNull(db.Categories);
        Assert.NotNull(db.Articles);
    }

    [Fact]
    public void SavingAnEntity_StampsUpdatedAt()
    {
        using var db = InMemoryContext();

        db.Dishes.Add(new Entities.Dish
        {
            Id = "ifisashi", LocalName = "Ifisashi", EnglishName = "Groundnut and greens relish",
            Region = "Pan-Zambian", TimeLabel = "45 min", Difficulty = "Easy",
            Description = "Leafy greens simmered in pounded groundnuts.",
        });
        db.SaveChanges();

        var saved = db.Dishes.Single();
        Assert.True(saved.UpdatedAt > DateTimeOffset.MinValue);
    }
}
```

> `UseInMemoryDatabase` needs `Microsoft.EntityFrameworkCore.InMemory` on the test project.
> It is used only for schema-shape tests; every behavioural test in Task 6 runs against real
> Postgres via Testcontainers, because the in-memory provider does not honour relational
> semantics.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~SchemaTests`
Expected: FAIL — `TasteZambiaDbContext` does not exist.

- [ ] **Step 3: Write the entities**

`TasteZambia.API/Data/Entities/Archive.cs` — all read-model entities in one file; they change
together and are meaningless apart.

```csharp
namespace TasteZambia.API.Data.Entities;

/// <summary>Every archive row carries when it last changed. Stage 5's delta sync reads this.</summary>
public abstract class ArchiveEntity
{
    public DateTimeOffset UpdatedAt { get; set; }
}

public class Dish : ArchiveEntity
{
    public required string Id { get; set; }
    public required string LocalName { get; set; }
    public required string EnglishName { get; set; }
    public required string Region { get; set; }
    public required string TimeLabel { get; set; }
    public required string Difficulty { get; set; }
    public required string Description { get; set; }
    public string? ImageAsset { get; set; }
    public string PhotoNeededCaption { get; set; } = "photo needed";
    public string? PrepTime { get; set; }
    public string? CookTime { get; set; }
    public int SortOrder { get; set; }

    public Recipe? Recipe { get; set; }
}

public class Recipe : ArchiveEntity
{
    public int Id { get; set; }
    public required string DishId { get; set; }
    public Dish Dish { get; set; } = null!;

    public required string Subtitle { get; set; }
    public bool IsVerified { get; set; }

    public required string ContributorName { get; set; }
    public required string ContributorLocation { get; set; }
    public string? ContributorAvatarAsset { get; set; }

    public List<RecipeParagraph> CulturalContext { get; set; } = [];
    public List<RecipeIngredient> Ingredients { get; set; } = [];
    public List<CookingStep> Steps { get; set; } = [];
    public List<MethodNarrative> Methods { get; set; } = [];
    public List<RegionalVariation> Variations { get; set; } = [];
}

public class RecipeParagraph
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int SortOrder { get; set; }
    public required string Text { get; set; }
}

public class RecipeIngredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Null for plain items (Salt, Water) that have no archive entry.</summary>
    public string? IngredientKey { get; set; }
    public required string DisplayName { get; set; }
    public required string DisplaySubtitle { get; set; }
    public required string Quantity { get; set; }
}

public class CookingStep
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
}

public enum MethodKind { Traditional = 0, Modern = 1 }

public class MethodNarrative
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public MethodKind Kind { get; set; }
    public required string Heading { get; set; }
    public List<MethodParagraph> Paragraphs { get; set; } = [];
}

public class MethodParagraph
{
    public int Id { get; set; }
    public int MethodNarrativeId { get; set; }
    public int SortOrder { get; set; }
    public required string Text { get; set; }
}

public class RegionalVariation
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int SortOrder { get; set; }
    public required string Place { get; set; }
    public required string Description { get; set; }
}

public class Ingredient : ArchiveEntity
{
    public required string Key { get; set; }
    public required string LocalName { get; set; }
    public required string EnglishName { get; set; }
    public required string Description { get; set; }
    public required string WhereFound { get; set; }
    public required string TraditionalPreparation { get; set; }
    public required string PendingLanguages { get; set; }
    public string? ImageAsset { get; set; }
    public int SortOrder { get; set; }

    public List<IngredientLocalName> LocalNames { get; set; } = [];
    public List<IngredientUsage> Usages { get; set; } = [];
}

public class IngredientLocalName
{
    public int Id { get; set; }
    public required string IngredientKey { get; set; }
    public int SortOrder { get; set; }
    public required string Language { get; set; }
    public required string Name { get; set; }
}

/// <summary>Which dishes an ingredient appears in. Ordered as the archive lists them.</summary>
public class IngredientUsage
{
    public int Id { get; set; }
    public required string IngredientKey { get; set; }
    public required string DishId { get; set; }
    public int SortOrder { get; set; }
}

public class Province : ArchiveEntity
{
    public required string Name { get; set; }
    public required string Seat { get; set; }
    public required string Blurb { get; set; }
    public required string CookingTradition { get; set; }
    public int SortOrder { get; set; }

    public List<ProvinceFood> Foods { get; set; } = [];
    public List<ProvinceIngredient> CommonIngredients { get; set; } = [];
}

public class ProvinceFood
{
    public int Id { get; set; }
    public required string ProvinceName { get; set; }
    public int SortOrder { get; set; }

    /// <summary>The archive's own name for the food — may not match a seeded dish.</summary>
    public required string Name { get; set; }

    /// <summary>English subtitle for foods with no dish entry (e.g. Katapa).</summary>
    public string? FallbackSubtitle { get; set; }
}

public class ProvinceIngredient
{
    public int Id { get; set; }
    public required string ProvinceName { get; set; }
    public int SortOrder { get; set; }
    public required string Name { get; set; }
}

public class Category : ArchiveEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? ImageAsset { get; set; }
    public int SortOrder { get; set; }
}

public enum ArticleBlockKind { Lede = 0, Paragraph = 1, PullQuote = 2 }

public class Article : ArchiveEntity
{
    public required string Id { get; set; }
    public required string Kicker { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public required string Meta { get; set; }
    public string? Lede { get; set; }
    public bool IsLead { get; set; }
    public string? ImageAsset { get; set; }
    public string PhotoNeededCaption { get; set; } = "photo needed";
    public int SortOrder { get; set; }

    public string? AudioLabel { get; set; }
    public string? AudioDuration { get; set; }
    public double? AudioProgress { get; set; }

    public List<ArticleBlock> Body { get; set; } = [];
    public List<ArticleRelatedDish> RelatedDishes { get; set; } = [];
}

public class ArticleBlock
{
    public int Id { get; set; }
    public required string ArticleId { get; set; }
    public int SortOrder { get; set; }
    public ArticleBlockKind Kind { get; set; }
    public required string Text { get; set; }
}

public class ArticleRelatedDish
{
    public int Id { get; set; }
    public required string ArticleId { get; set; }
    public required string DishId { get; set; }
    public int SortOrder { get; set; }
}
```

- [ ] **Step 4: Write the DbContext**

`TasteZambia.API/Data/TasteZambiaDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data;

public class TasteZambiaDbContext(DbContextOptions<TasteZambiaDbContext> options)
    : DbContext(options)
{
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(TasteZambiaDbContext).Assembly);
        base.OnModelCreating(b);
    }

    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAll, CancellationToken ct = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAll, ct);
    }

    /// <summary>
    /// Every archive row records when it last changed. Stage 5's delta sync reads this
    /// column, so it must be stamped on every write without exception.
    /// </summary>
    private void StampTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ArchiveEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
```

- [ ] **Step 5: Write the configurations**

`TasteZambia.API/Data/Configurations/ArchiveConfigurations.cs`. Slug keys, ordered
children, cascade deletes, and the `xmin` concurrency token Postgres gives free:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Configurations;

public class DishConfiguration : IEntityTypeConfiguration<Dish>
{
    public void Configure(EntityTypeBuilder<Dish> e)
    {
        e.ToTable("dishes");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasMaxLength(64);
        e.Property(x => x.LocalName).HasMaxLength(120).IsRequired();
        e.Property(x => x.EnglishName).HasMaxLength(160).IsRequired();
        e.Property(x => x.Region).HasMaxLength(80).IsRequired();
        e.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        e.Property(x => x.PhotoNeededCaption).HasMaxLength(200);
        e.HasIndex(x => x.SortOrder);

        // Postgres system column: free optimistic concurrency, no extra column to maintain.
        e.UseXminAsConcurrencyToken();

        e.HasOne(x => x.Recipe)
         .WithOne(r => r.Dish)
         .HasForeignKey<Recipe>(r => r.DishId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> e)
    {
        e.ToTable("recipes");
        e.HasKey(x => x.Id);
        e.Property(x => x.DishId).HasMaxLength(64).IsRequired();
        e.HasIndex(x => x.DishId).IsUnique();
        e.UseXminAsConcurrencyToken();

        e.HasMany(x => x.CulturalContext).WithOne().HasForeignKey(p => p.RecipeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Ingredients).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Steps).WithOne().HasForeignKey(s => s.RecipeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Methods).WithOne().HasForeignKey(m => m.RecipeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Variations).WithOne().HasForeignKey(v => v.RecipeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> e)
    {
        e.ToTable("ingredients");
        e.HasKey(x => x.Key);
        e.Property(x => x.Key).HasMaxLength(64);
        e.Property(x => x.LocalName).HasMaxLength(120).IsRequired();
        e.Property(x => x.EnglishName).HasMaxLength(160).IsRequired();
        e.HasIndex(x => x.SortOrder);
        e.UseXminAsConcurrencyToken();

        e.HasMany(x => x.LocalNames).WithOne().HasForeignKey(l => l.IngredientKey).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Usages).WithOne().HasForeignKey(u => u.IngredientKey).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> e)
    {
        e.ToTable("provinces");
        e.HasKey(x => x.Name);
        e.Property(x => x.Name).HasMaxLength(80);
        e.HasIndex(x => x.SortOrder);
        e.UseXminAsConcurrencyToken();

        e.HasMany(x => x.Foods).WithOne().HasForeignKey(f => f.ProvinceName).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.CommonIngredients).WithOne().HasForeignKey(i => i.ProvinceName).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> e)
    {
        e.ToTable("articles");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasMaxLength(64);
        e.Property(x => x.Title).HasMaxLength(200).IsRequired();
        e.HasIndex(x => x.SortOrder);
        e.UseXminAsConcurrencyToken();

        e.HasMany(x => x.Body).WithOne().HasForeignKey(bl => bl.ArticleId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.RelatedDishes).WithOne().HasForeignKey(r => r.ArticleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> e)
    {
        e.ToTable("categories");
        e.HasKey(x => x.Id);
        e.Property(x => x.Name).HasMaxLength(120).IsRequired();
        e.HasIndex(x => x.SortOrder);
        e.UseXminAsConcurrencyToken();
    }
}
```

Child tables (`RecipeParagraph`, `CookingStep`, `MethodNarrative`, `MethodParagraph`,
`RegionalVariation`, `RecipeIngredient`, `IngredientLocalName`, `IngredientUsage`,
`ProvinceFood`, `ProvinceIngredient`, `ArticleBlock`, `ArticleRelatedDish`) each get a small
configuration in the same file: `ToTable(snake_case)`, `HasKey(x => x.Id)`, and an index on
`(ParentId, SortOrder)`. They carry no `UpdatedAt` — their parent's timestamp covers them.

- [ ] **Step 6: Register the context and create the migration**

In `Program.cs`, above `var app = builder.Build();`:

```csharp
builder.Services.AddDbContext<TasteZambiaDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Archive")));
```

Then:

```bash
export PATH="/usr/local/share/dotnet:$PATH"
docker compose up -d db
dotnet ef migrations add InitialArchive --project TasteZambia.API
dotnet ef database update --project TasteZambia.API
```

- [ ] **Step 7: Verify the schema landed**

```bash
docker exec -it tastezambia-db psql -U tastezambia -d tastezambia -c "\dt"
```

Expected: `dishes`, `recipes`, `ingredients`, `provinces`, `categories`, `articles` and the
child tables. Confirm `dishes` has an `xmin`-backed concurrency token by checking the
migration did **not** add a `RowVersion` column — Postgres supplies it as a system column.

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~SchemaTests`
Expected: PASS, 2 tests.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat(api): add archive entities, DbContext and initial migration

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 4: Seed the archive

The content is already transcribed verbatim in the mobile plan
(`2026-09-08-mobile-ui-implementation.md`, Task 3, `SeedData.cs`). **Port it, do not
re-transcribe from the canvas** — a second transcription is a second chance to introduce
drift in cultural copy, and the mobile version is already the reviewed one.

**Files:**
- Create: `TasteZambia.API/Data/Seed/ArchiveSeeder.cs`
- Modify: `TasteZambia.API/Program.cs`
- Test: `TasteZambia.API.Tests/Data/SeederTests.cs`

**Interfaces:**
- Consumes: `TasteZambiaDbContext`.
- Produces: `ArchiveSeeder.SeedAsync(TasteZambiaDbContext, CancellationToken)` — idempotent; running it twice leaves the same rows.

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Seed;

namespace TasteZambia.API.Tests.Data;

[Collection(nameof(DatabaseCollection))]
public class SeederTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Seed_LoadsTheEightDishesAndNineIngredients()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);

        Assert.Equal(8, await db.Dishes.CountAsync());
        Assert.Equal(9, await db.Ingredients.CountAsync());
        Assert.Equal(10, await db.Provinces.CountAsync());
        Assert.Equal(8, await db.Categories.CountAsync());
        Assert.Equal(6, await db.Articles.CountAsync());
    }

    [Fact]
    public async Task Seed_IsIdempotent()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);

        Assert.Equal(8, await db.Dishes.CountAsync());
    }

    [Fact]
    public async Task Ifisashi_HasItsFullRecipe()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);

        var recipe = await db.Recipes
            .Include(r => r.Ingredients).Include(r => r.Steps)
            .Include(r => r.Methods).ThenInclude(m => m.Paragraphs)
            .Include(r => r.Variations).Include(r => r.CulturalContext)
            .SingleAsync(r => r.DishId == "ifisashi");

        Assert.Equal(6, recipe.Ingredients.Count);
        Assert.Equal(4, recipe.Steps.Count);
        Assert.Equal(4, recipe.Variations.Count);
        Assert.Equal(3, recipe.CulturalContext.Count);
        Assert.Equal(2, recipe.Methods.Count);
        Assert.All(recipe.Methods, m => Assert.Equal(3, m.Paragraphs.Count));
        Assert.True(recipe.IsVerified);
    }

    [Fact]
    public async Task FiveDishesStillAwaitPhotographyAndSayWhatIsNeeded()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);

        var unphotographed = await db.Dishes.Where(d => d.ImageAsset == null).ToListAsync();

        Assert.Equal(5, unphotographed.Count);
        Assert.All(unphotographed, d => Assert.StartsWith("photo:", d.PhotoNeededCaption));
    }

    [Fact]
    public async Task Chibwabwa_LinksToThreeDishesInArchiveOrder()
    {
        await using var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);

        var usages = await db.Ingredients
            .Where(i => i.Key == "chibwabwa")
            .SelectMany(i => i.Usages.OrderBy(u => u.SortOrder))
            .Select(u => u.DishId)
            .ToListAsync();

        Assert.Equal(["ifisashi", "nshima", "delele"], usages);
    }
}
```

- [ ] **Step 2: Write the Postgres test fixture**

`TasteZambia.API.Tests/DatabaseFixture.cs` — one container for the whole test run:

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using Testcontainers.PostgreSql;

namespace TasteZambia.API.Tests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("tastezambia_test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = NewContext();
        await db.Database.MigrateAsync();
    }

    /// <summary>A fresh context per test, all against the one container.</summary>
    public TasteZambiaDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<TasteZambiaDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new TasteZambiaDbContext(options);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition(nameof(DatabaseCollection))]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
```

Because tests share one database, **`SeedAsync` must be idempotent** — which the test above
asserts directly.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~SeederTests`
Expected: FAIL — `ArchiveSeeder` does not exist. Docker must be running.

- [ ] **Step 4: Write the seeder**

`TasteZambia.API/Data/Seed/ArchiveSeeder.cs`. Structure:

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Seed;

public static class ArchiveSeeder
{
    public static async Task SeedAsync(TasteZambiaDbContext db, CancellationToken ct = default)
    {
        if (await db.Dishes.AnyAsync(ct)) return;   // idempotent

        db.Dishes.AddRange(BuildDishes());
        db.Ingredients.AddRange(BuildIngredients());
        db.Provinces.AddRange(BuildProvinces());
        db.Categories.AddRange(BuildCategories());
        db.Articles.AddRange(BuildArticles());
        await db.SaveChangesAsync(ct);

        db.Recipes.Add(BuildIfisashiRecipe());
        await db.SaveChangesAsync(ct);
    }

    private static List<Dish> BuildDishes() =>
    [
        new() { Id = "ifisashi", LocalName = "Ifisashi", EnglishName = "Groundnut and greens relish",
                Region = "Pan-Zambian", TimeLabel = "45 min", Difficulty = "Easy",
                ImageAsset = "ifisashi.png", PrepTime = "20 min", CookTime = "30 min", SortOrder = 0,
                Description = "Leafy greens simmered in pounded groundnuts until the sauce thickens and the oil rises. Eaten with nshima across the country." },
        // … the remaining seven, ported verbatim from the mobile plan's SeedData.Dishes,
        //    preserving order (SortOrder 0-7) and every PhotoNeededCaption.
    ];

    // BuildIngredients, BuildProvinces, BuildCategories, BuildArticles and
    // BuildIfisashiRecipe port the corresponding members of the mobile plan's SeedData.
}
```

**Porting rules:**
- `SortOrder` reproduces array order — the design's ordering is editorial, not incidental.
- `IngredientUsage.SortOrder` preserves `UsedInDishIds` order; the ingredient detail screen renders them in that order.
- Only `ifisashi` gets a `Recipe`; only `chibwabwa` has an image. That matches the canvas and is recorded in the mobile plan's Known Gaps.
- `Katapa` on Luapula/Northern has `FallbackSubtitle = "Cassava leaf relish"`; every other `ProvinceFood` leaves it null and resolves through `Dishes`.
- Only the Nshima article has `Body` blocks and audio.

- [ ] **Step 5: Run seeding at startup in Development**

In `Program.cs`, after `var app = builder.Build();`:

```csharp
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<TasteZambiaDbContext>();
    await db.Database.MigrateAsync();
    await ArchiveSeeder.SeedAsync(db);
}
```

Migrating on startup is right for Development only. Production runs migrations as a
deliberate deploy step — never let a scaling event race two instances into the same
migration.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~SeederTests`
Expected: PASS, 5 tests.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(api): seed the archive from the design content

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 5: Repositories and services

The two layers under the slices. **Repositories fetch; services decide.** The repository
interfaces mirror the mobile plan's Task 3 deliberately — same names, same shapes — so the
two sides stay legible to one another.

**Files:**
- Create: `TasteZambia.API/Repositories/ArchiveRepositories.cs`
- Create: `TasteZambia.API/Services/CatalogService.cs`, `Services/ArchiveVersionService.cs`
- Modify: `TasteZambia.API/Program.cs`
- Test: `TasteZambia.API.Tests/Services/CatalogServiceTests.cs`

**Interfaces:**
- Produces:
  - `IDishRepository`: `GetAllAsync`, `GetByIdAsync`, `GetRecipeAsync`
  - `IIngredientRepository`: `GetAllAsync`, `GetByKeyAsync`
  - `IRegionRepository`, `IArticleRepository`, `ICategoryRepository`: `GetAllAsync` (+ `GetByIdAsync` where the design has a detail screen)
  - `ICatalogService`: `SearchAsync(string query, string filter, CancellationToken)`
  - `IArchiveVersionService`: `Task<string> GetETagAsync(string resource, CancellationToken)`

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data.Seed;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;

namespace TasteZambia.API.Tests.Services;

[Collection(nameof(DatabaseCollection))]
public class CatalogServiceTests(DatabaseFixture fixture)
{
    private async Task<CatalogService> SutAsync()
    {
        var db = fixture.NewContext();
        await ArchiveSeeder.SeedAsync(db, CancellationToken.None);
        return new CatalogService(new DishRepository(db));
    }

    [Fact]
    public async Task EmptyQuery_ReturnsEveryDishInArchiveOrder()
    {
        var results = await (await SutAsync()).SearchAsync("", "All");

        Assert.Equal(8, results.Count);
        Assert.Equal("ifisashi", results[0].Id);
    }

    [Fact]
    public async Task Query_MatchesLocalNameCaseInsensitively()
    {
        var results = await (await SutAsync()).SearchAsync("IFISASHI", "All");
        Assert.Single(results);
    }

    [Fact]
    public async Task Query_AlsoMatchesEnglishNameRegionAndDescription()
    {
        var sut = await SutAsync();

        Assert.Equal("chikanda", (await sut.SearchAsync("orchid", "All"))[0].Id);
        Assert.Equal("kapenta", (await sut.SearchAsync("luapula", "All"))[0].Id);
        Assert.Equal("delele", (await sut.SearchAsync("bicarbonate", "All"))[0].Id);
    }

    [Fact]
    public async Task Query_IsTrimmed()
    {
        var sut = await SutAsync();
        var padded = await sut.SearchAsync("   nshima   ", "All");
        var exact = await sut.SearchAsync("nshima", "All");

        // "nshima" legitimately matches two dishes: Nshima itself, and Ifisashi,
        // whose description reads "Eaten with nshima across the country."
        // Trimming means the padded query behaves identically to the exact one.
        Assert.Equal(exact.Select(d => d.Id), padded.Select(d => d.Id));
        Assert.Equal(2, padded.Count);
    }

    [Fact]
    public async Task Query_WithNoMatch_ReturnsEmpty()
    {
        Assert.Empty(await (await SutAsync()).SearchAsync("sushi", "All"));
    }
}
```

These assertions are intentionally identical to the mobile `CatalogServiceTests`. The same
search must behave the same on both sides, or swapping the repository changes what users see.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~CatalogServiceTests`
Expected: FAIL — `DishRepository` does not exist.

- [ ] **Step 3: Write the repositories**

`TasteZambia.API/Repositories/ArchiveRepositories.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Repositories;

public interface IDishRepository
{
    Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default);
    Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Recipe?> GetRecipeAsync(string dishId, CancellationToken ct = default);
}

public sealed class DishRepository(TasteZambiaDbContext db) : IDishRepository
{
    public async Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default)
        => await db.Dishes.AsNoTracking().OrderBy(d => d.SortOrder).ToListAsync(ct);

    public Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default)
        => db.Dishes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Recipe?> GetRecipeAsync(string dishId, CancellationToken ct = default)
        => db.Recipes
            .AsNoTracking()
            .Include(r => r.Dish)
            .Include(r => r.CulturalContext.OrderBy(p => p.SortOrder))
            .Include(r => r.Ingredients.OrderBy(i => i.SortOrder))
            .Include(r => r.Steps.OrderBy(s => s.Number))
            .Include(r => r.Methods).ThenInclude(m => m.Paragraphs.OrderBy(p => p.SortOrder))
            .Include(r => r.Variations.OrderBy(v => v.SortOrder))
            .FirstOrDefaultAsync(r => r.DishId == dishId, ct);
}

public interface IIngredientRepository
{
    Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken ct = default);
    Task<Ingredient?> GetByKeyAsync(string key, CancellationToken ct = default);
}

public sealed class IngredientRepository(TasteZambiaDbContext db) : IIngredientRepository
{
    public async Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken ct = default)
        => await db.Ingredients.AsNoTracking()
            .Include(i => i.LocalNames.OrderBy(l => l.SortOrder))
            .Include(i => i.Usages.OrderBy(u => u.SortOrder))
            .OrderBy(i => i.SortOrder)
            .ToListAsync(ct);

    public Task<Ingredient?> GetByKeyAsync(string key, CancellationToken ct = default)
        => db.Ingredients.AsNoTracking()
            .Include(i => i.LocalNames.OrderBy(l => l.SortOrder))
            .Include(i => i.Usages.OrderBy(u => u.SortOrder))
            .FirstOrDefaultAsync(i => i.Key == key, ct);
}

public interface IRegionRepository
{
    Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken ct = default);
}

public sealed class RegionRepository(TasteZambiaDbContext db) : IRegionRepository
{
    public async Task<IReadOnlyList<Province>> GetAllAsync(CancellationToken ct = default)
        => await db.Provinces.AsNoTracking()
            .Include(p => p.Foods.OrderBy(f => f.SortOrder))
            .Include(p => p.CommonIngredients.OrderBy(i => i.SortOrder))
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);
}

public interface IArticleRepository
{
    Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default);
    Task<Article?> GetByIdAsync(string id, CancellationToken ct = default);
}

public sealed class ArticleRepository(TasteZambiaDbContext db) : IArticleRepository
{
    /// <summary>List view: metadata only. Body blocks are fetched per article.</summary>
    public async Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default)
        => await db.Articles.AsNoTracking().OrderBy(a => a.SortOrder).ToListAsync(ct);

    public Task<Article?> GetByIdAsync(string id, CancellationToken ct = default)
        => db.Articles.AsNoTracking()
            .Include(a => a.Body.OrderBy(b => b.SortOrder))
            .Include(a => a.RelatedDishes.OrderBy(r => r.SortOrder))
            .FirstOrDefaultAsync(a => a.Id == id, ct);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
}

public sealed class CategoryRepository(TasteZambiaDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => await db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync(ct);
}
```

Note `ArticleRepository.GetAllAsync` deliberately omits `Body` — the Culture list screen
shows only kicker, title and byline, and pulling every essay's prose to render six rows
would be waste.

- [ ] **Step 4: Write the services**

`TasteZambia.API/Services/CatalogService.cs`:

```csharp
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;

namespace TasteZambia.API.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<Dish>> SearchAsync(string query, string filter, CancellationToken ct = default);
}

public sealed class CatalogService(IDishRepository dishes) : ICatalogService
{
    public async Task<IReadOnlyList<Dish>> SearchAsync(
        string query, string filter, CancellationToken ct = default)
    {
        var all = await dishes.GetAllAsync(ct);
        var q = query.Trim();

        if (q.Length == 0)
            return all;

        // Local name, English name, region and description are one haystack,
        // matching the mobile client exactly.
        return all
            .Where(d => $"{d.LocalName} {d.EnglishName} {d.Region} {d.Description}"
                .Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
```

`filter` is accepted and not yet applied — the design's chips change appearance without
narrowing results. Keeping it in the signature now means neither the endpoint nor the mobile
ViewModel changes when filtering lands.

> **Scaling note.** This filters in memory over 8 rows, which is correct at this size and
> keeps parity with the client. Past a few hundred dishes, move the predicate into the
> repository as a Postgres `tsvector` full-text index. That is a repository change only —
> `ICatalogService`'s signature and every caller stay put.

`TasteZambia.API/Services/ArchiveVersionService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;

namespace TasteZambia.API.Services;

public interface IArchiveVersionService
{
    Task<string> GetETagAsync(string resource, CancellationToken ct = default);
}

/// <summary>
/// An ETag per resource, derived from the newest UpdatedAt in that table.
/// Cheap now, and the same column Stage 5's delta sync will page on.
/// </summary>
public sealed class ArchiveVersionService(TasteZambiaDbContext db) : IArchiveVersionService
{
    public async Task<string> GetETagAsync(string resource, CancellationToken ct = default)
    {
        DateTimeOffset? latest = resource switch
        {
            "dishes"      => await db.Dishes.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct),
            "ingredients" => await db.Ingredients.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct),
            "regions"     => await db.Provinces.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct),
            "articles"    => await db.Articles.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct),
            "categories"  => await db.Categories.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unknown archive resource"),
        };

        return $"\"{resource}-{latest?.ToUnixTimeMilliseconds() ?? 0}\"";
    }
}
```

- [ ] **Step 5: Register everything**

In `Program.cs`:

```csharp
builder.Services.AddScoped<IDishRepository, DishRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<IRegionRepository, RegionRepository>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IArchiveVersionService, ArchiveVersionService>();
```

Scoped, not singleton — they hold a `DbContext`.

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~CatalogServiceTests`
Expected: PASS, 5 tests.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(api): add archive repositories and catalog/version services

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 6: Feature slices and the endpoint pipeline

Every endpoint is one folder: route, handler, mapping. Nothing else in the API knows it
exists — the assembly scan finds it.

**Files:**
- Create: `TasteZambia.API/Common/Endpoints/IEndpoint.cs`, `Common/Endpoints/EndpointExtensions.cs`, `Common/Http/ETagResults.cs`
- Create: `TasteZambia.API/Features/Dishes/{GetDishes,GetDishById,GetRecipe,SearchDishes}/Endpoint.cs`
- Create: `TasteZambia.API/Features/Ingredients/{GetIngredients,GetIngredientByKey}/Endpoint.cs`
- Create: `TasteZambia.API/Features/{Regions/GetRegions,Culture/GetArticles,Culture/GetArticleById,Categories/GetCategories}/Endpoint.cs`
- Create: `TasteZambia.API/Features/Mapping/ArchiveMappings.cs`
- Modify: `TasteZambia.API/Program.cs`
- Test: `TasteZambia.API.Tests/Features/ArchiveEndpointTests.cs`

**Interfaces:**
- Produces: `IEndpoint` (`static abstract void Map(IEndpointRouteBuilder)`), `EndpointExtensions.MapArchiveEndpoints()`, `ETagResults.OkWithETag<T>(...)`, and the nine endpoints below.

| Method | Route | Returns |
|---|---|---|
| GET | `/api/v1/dishes` | `IReadOnlyList<DishDto>` |
| GET | `/api/v1/dishes/{id}` | `DishDto` · 404 |
| GET | `/api/v1/dishes/{id}/recipe` | `RecipeDto` · 404 |
| GET | `/api/v1/dishes/search?q=&filter=` | `IReadOnlyList<DishDto>` |
| GET | `/api/v1/ingredients` | `IReadOnlyList<IngredientDto>` |
| GET | `/api/v1/ingredients/{key}` | `IngredientDto` · 404 |
| GET | `/api/v1/regions` | `IReadOnlyList<ProvinceDto>` |
| GET | `/api/v1/articles` | `IReadOnlyList<ArticleDto>` |
| GET | `/api/v1/articles/{id}` | `ArticleDto` · 404 |
| GET | `/api/v1/categories` | `IReadOnlyList<CategoryDto>` |

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Dishes;
using TasteZambia.Shared.Contracts.Ingredients;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Features;

[Collection(nameof(DatabaseCollection))]
public class ArchiveEndpointTests(ApiFactory factory)
{
    private HttpClient Client => factory.CreateClient();

    [Fact]
    public async Task GetDishes_ReturnsEightInArchiveOrder()
    {
        var dishes = await Client.GetFromJsonAsync<List<DishDto>>(ApiRoutes.Dishes.Collection);

        Assert.NotNull(dishes);
        Assert.Equal(8, dishes!.Count);
        Assert.Equal("ifisashi", dishes[0].Id);
        Assert.Equal("Ifisashi", dishes[0].LocalName);
    }

    [Fact]
    public async Task GetDishes_SendsAnETagAndHonoursIfNoneMatch()
    {
        var first = await Client.GetAsync(ApiRoutes.Dishes.Collection);
        var etag = first.Headers.ETag;

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.NotNull(etag);

        var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Dishes.Collection);
        request.Headers.IfNoneMatch.Add(etag!);
        var second = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
    }

    [Fact]
    public async Task GetRecipe_ReturnsTheFullIfisashiRecipe()
    {
        var recipe = await Client.GetFromJsonAsync<RecipeDto>("/api/v1/dishes/ifisashi/recipe");

        Assert.NotNull(recipe);
        Assert.Equal("Traditional Zambian vegetable dish", recipe!.Subtitle);
        Assert.True(recipe.IsVerified);
        Assert.Equal(6, recipe.Ingredients.Count);
        Assert.Equal(4, recipe.Steps.Count);
        Assert.Equal(3, recipe.CulturalContext.Count);
        Assert.Equal("Over charcoal, in a clay pot", recipe.TraditionalMethod.Heading);
        Assert.Equal("In a flat you rent abroad", recipe.ModernMethod.Heading);
        Assert.Equal("Chanda M.", recipe.Contributor.Name);
    }

    [Fact]
    public async Task GetRecipe_ForADishWithoutOne_Returns404()
    {
        var response = await Client.GetAsync("/api/v1/dishes/kapenta/recipe");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SearchDishes_NarrowsAndIsCaseInsensitive()
    {
        var results = await Client.GetFromJsonAsync<List<DishDto>>(
            $"{ApiRoutes.Dishes.Search}?q=ORCHID");

        Assert.Single(results!);
        Assert.Equal("chikanda", results![0].Id);
    }

    [Fact]
    public async Task SearchDishes_WithNoMatch_ReturnsEmptyNot404()
    {
        var response = await Client.GetAsync($"{ApiRoutes.Dishes.Search}?q=sushi");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<List<DishDto>>())!);
    }

    [Fact]
    public async Task GetIngredient_CarriesLocalNamesAndUsagesInOrder()
    {
        var ingredient = await Client.GetFromJsonAsync<IngredientDto>("/api/v1/ingredients/chibwabwa");

        Assert.NotNull(ingredient);
        Assert.Equal("Pumpkin leaves", ingredient!.EnglishName);
        Assert.Single(ingredient.LocalNames);
        Assert.Equal("Bemba, Nyanja", ingredient.LocalNames[0].Language);
        Assert.Equal(["ifisashi", "nshima", "delele"], ingredient.UsedInDishIds);
        Assert.Contains("Tonga", ingredient.PendingLanguages);
    }

    [Fact]
    public async Task GetIngredient_Unknown_Returns404WithProblemDetails()
    {
        var response = await Client.GetAsync("/api/v1/ingredients/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetRegions_ReturnsTenWithNorthernAtIndexSix()
    {
        var provinces = await Client.GetFromJsonAsync<List<TasteZambia.Shared.Contracts.Regions.ProvinceDto>>(
            ApiRoutes.Regions.Collection);

        Assert.Equal(10, provinces!.Count);
        Assert.Equal("Northern", provinces[6].Name);
        Assert.Equal("Kasama", provinces[6].Seat);
        Assert.Equal(3, provinces[6].SignatureFoods.Count);
    }

    [Fact]
    public async Task GetArticle_ReturnsBodyBlocksAndAudio()
    {
        var article = await Client.GetFromJsonAsync<TasteZambia.Shared.Contracts.Culture.ArticleDto>(
            "/api/v1/articles/nshima");

        Assert.NotNull(article);
        Assert.True(article!.IsLead);
        Assert.Equal(6, article.Body.Count);
        Assert.NotNull(article.Audio);
        Assert.Equal("Listen in Bemba", article.Audio!.Label);
        Assert.Equal(["nshima"], article.RelatedDishIds);
    }
}
```

- [ ] **Step 2: Write the API factory**

`TasteZambia.API.Tests/ApiFactory.cs`:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Seed;

namespace TasteZambia.API.Tests;

public sealed class ApiFactory(DatabaseFixture fixture) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<TasteZambiaDbContext>));
            services.AddDbContext<TasteZambiaDbContext>(o => o.UseNpgsql(fixture.ConnectionString));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TasteZambiaDbContext>();
            db.Database.Migrate();
            ArchiveSeeder.SeedAsync(db).GetAwaiter().GetResult();
        });
    }
}
```

Register it on the collection so it shares the one container:

```csharp
[CollectionDefinition(nameof(DatabaseCollection))]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>, ICollectionFixture<ApiFactory>;
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~ArchiveEndpointTests`
Expected: FAIL — every route 404s; no endpoints are mapped yet.

- [ ] **Step 4: Write the endpoint pipeline**

`Common/Endpoints/IEndpoint.cs`:

```csharp
namespace TasteZambia.API.Common.Endpoints;

/// <summary>One HTTP endpoint. Implementations are discovered by assembly scan.</summary>
public interface IEndpoint
{
    static abstract void Map(IEndpointRouteBuilder app);
}
```

`Common/Endpoints/EndpointExtensions.cs`:

```csharp
using System.Reflection;

namespace TasteZambia.API.Common.Endpoints;

public static class EndpointExtensions
{
    /// <summary>
    /// Finds every IEndpoint in this assembly and maps it. Adding a feature folder is
    /// therefore all it takes to add a route - no central registry to edit and forget.
    /// </summary>
    public static IEndpointRouteBuilder MapArchiveEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.IsAssignableTo(typeof(IEndpoint)));

        foreach (var type in endpoints)
        {
            var map = type.GetMethod(nameof(IEndpoint.Map),
                BindingFlags.Public | BindingFlags.Static);
            map?.Invoke(null, [app]);
        }

        return app;
    }
}
```

`Common/Http/ETagResults.cs`:

```csharp
using Microsoft.Net.Http.Headers;

namespace TasteZambia.API.Common.Http;

public static class ETagResults
{
    /// <summary>
    /// Returns 304 when the client already holds this version, otherwise 200 with the ETag.
    /// Every read endpoint goes through here so conditional GETs work uniformly.
    /// </summary>
    public static IResult OkWithETag<T>(HttpContext http, string etag, T payload)
    {
        var incoming = http.Request.Headers.IfNoneMatch.ToString();

        if (!string.IsNullOrEmpty(incoming) && incoming == etag)
            return Results.StatusCode(StatusCodes.Status304NotModified);

        http.Response.Headers[HeaderNames.ETag] = etag;
        http.Response.Headers[HeaderNames.CacheControl] = "private, max-age=0, must-revalidate";
        return Results.Ok(payload);
    }
}
```

- [ ] **Step 5: Write the mappings**

`Features/Mapping/ArchiveMappings.cs` — entity → DTO in one place, so no slice invents its
own shape:

```csharp
using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Contracts.Common;
using TasteZambia.Shared.Contracts.Culture;
using TasteZambia.Shared.Contracts.Dishes;
using TasteZambia.Shared.Contracts.Ingredients;
using TasteZambia.Shared.Contracts.Regions;

namespace TasteZambia.API.Features.Mapping;

public static class ArchiveMappings
{
    public static DishDto ToDto(this Dish d) => new()
    {
        Id = d.Id, LocalName = d.LocalName, EnglishName = d.EnglishName,
        Region = d.Region, TimeLabel = d.TimeLabel, Difficulty = d.Difficulty,
        Description = d.Description, ImageAsset = d.ImageAsset,
        PhotoNeededCaption = d.PhotoNeededCaption,
        PrepTime = d.PrepTime, CookTime = d.CookTime,
    };

    public static RecipeDto ToDto(this Recipe r) => new()
    {
        Dish = r.Dish.ToDto(),
        Subtitle = r.Subtitle,
        IsVerified = r.IsVerified,
        CulturalContext = [.. r.CulturalContext.OrderBy(p => p.SortOrder).Select(p => p.Text)],
        Ingredients = [.. r.Ingredients.OrderBy(i => i.SortOrder).Select(i => new RecipeIngredientDto
        {
            IngredientKey = i.IngredientKey, DisplayName = i.DisplayName,
            DisplaySubtitle = i.DisplaySubtitle, Quantity = i.Quantity,
        })],
        Steps = [.. r.Steps.OrderBy(s => s.Number).Select(s => new CookingStepDto(s.Number, s.Title, s.Body))],
        TraditionalMethod = r.Methods.Single(m => m.Kind == MethodKind.Traditional).ToDto(),
        ModernMethod = r.Methods.Single(m => m.Kind == MethodKind.Modern).ToDto(),
        Variations = [.. r.Variations.OrderBy(v => v.SortOrder).Select(v => new RegionalVariationDto(v.Place, v.Description))],
        Contributor = new ContributorDto(r.ContributorName, r.ContributorLocation, r.ContributorAvatarAsset),
    };

    private static MethodNarrativeDto ToDto(this MethodNarrative m)
        => new(m.Heading, [.. m.Paragraphs.OrderBy(p => p.SortOrder).Select(p => p.Text)]);

    public static IngredientDto ToDto(this Ingredient i) => new()
    {
        Key = i.Key, LocalName = i.LocalName, EnglishName = i.EnglishName,
        Description = i.Description, WhereFound = i.WhereFound,
        TraditionalPreparation = i.TraditionalPreparation,
        PendingLanguages = i.PendingLanguages, ImageAsset = i.ImageAsset,
        LocalNames = [.. i.LocalNames.OrderBy(l => l.SortOrder).Select(l => new LocalNameDto(l.Language, l.Name))],
        UsedInDishIds = [.. i.Usages.OrderBy(u => u.SortOrder).Select(u => u.DishId)],
    };

    public static ProvinceDto ToDto(this Province p) => new()
    {
        Name = p.Name, Seat = p.Seat, Blurb = p.Blurb,
        CookingTradition = p.CookingTradition,
        SignatureFoods = [.. p.Foods.OrderBy(f => f.SortOrder).Select(f => f.Name)],
        CommonIngredients = [.. p.CommonIngredients.OrderBy(i => i.SortOrder).Select(i => i.Name)],
    };

    public static ArticleDto ToDto(this Article a) => new()
    {
        Id = a.Id, Kicker = a.Kicker, Title = a.Title, Author = a.Author, Meta = a.Meta,
        Lede = a.Lede, IsLead = a.IsLead, ImageAsset = a.ImageAsset,
        PhotoNeededCaption = a.PhotoNeededCaption,
        Body = [.. a.Body.OrderBy(b => b.SortOrder)
                        .Select(b => new ArticleBlockDto((Shared.Contracts.Culture.ArticleBlockKind)b.Kind, b.Text))],
        RelatedDishIds = [.. a.RelatedDishes.OrderBy(r => r.SortOrder).Select(r => r.DishId)],
        Audio = a.AudioLabel is null
            ? null
            : new AudioNarrationDto(a.AudioLabel, a.AudioDuration ?? "", a.AudioProgress ?? 0),
    };

    public static CategoryDto ToDto(this Category c) => new(c.Name, c.ImageAsset, c.SortOrder);
}
```

- [ ] **Step 6: Write the slices**

Each is one small file. `Features/Dishes/GetDishes/Endpoint.cs`:

```csharp
using TasteZambia.API.Common.Endpoints;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Features.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Features.Dishes.GetDishes;

public sealed class Endpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Dishes.Collection, Handle)
           .WithName("GetDishes")
           .WithTags("Dishes")
           .Produces<IReadOnlyList<Shared.Contracts.Dishes.DishDto>>();

    private static async Task<IResult> Handle(
        HttpContext http,
        IDishRepository dishes,
        IArchiveVersionService versions,
        CancellationToken ct)
    {
        var etag = await versions.GetETagAsync("dishes", ct);
        var payload = (await dishes.GetAllAsync(ct)).Select(d => d.ToDto()).ToList();

        return ETagResults.OkWithETag(http, etag, payload);
    }
}
```

`Features/Dishes/GetRecipe/Endpoint.cs`:

```csharp
using TasteZambia.API.Common.Endpoints;
using TasteZambia.API.Features.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Features.Dishes.GetRecipe;

public sealed class Endpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Dishes.Recipe, Handle)
           .WithName("GetRecipe")
           .WithTags("Dishes")
           .Produces<Shared.Contracts.Dishes.RecipeDto>()
           .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(string id, IDishRepository dishes, CancellationToken ct)
    {
        var recipe = await dishes.GetRecipeAsync(id, ct);

        return recipe is null
            ? Results.Problem(
                title: "Recipe not found",
                detail: $"No recipe has been recorded for '{id}' yet.",
                statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(recipe.ToDto());
    }
}
```

`Features/Dishes/SearchDishes/Endpoint.cs`:

```csharp
using TasteZambia.API.Common.Endpoints;
using TasteZambia.API.Features.Mapping;
using TasteZambia.API.Services;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Features.Dishes.SearchDishes;

public sealed class Endpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Dishes.Search, Handle)
           .WithName("SearchDishes")
           .WithTags("Dishes")
           .Produces<IReadOnlyList<Shared.Contracts.Dishes.DishDto>>();

    private static async Task<IResult> Handle(
        string? q, string? filter, ICatalogService catalog, CancellationToken ct)
    {
        var results = await catalog.SearchAsync(q ?? "", filter ?? "All", ct);
        return Results.Ok(results.Select(d => d.ToDto()).ToList());
    }
}
```

The remaining six follow the same two shapes — collection endpoints use
`ETagResults.OkWithETag`, detail endpoints return `Results.Problem(404)` when the row is
missing. `GetArticles` maps with `.ToDto()` on the metadata-only entities, so `Body` comes
back empty by design.

- [ ] **Step 7: Wire the pipeline**

`Program.cs` — replace the controller scaffolding:

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
// … DbContext, repositories and services as registered in Tasks 3 and 5 …

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapArchiveEndpoints();
app.Run();

public partial class Program;
```

Remove `AddControllers`, `MapControllers`, `UseAuthorization` and `UseHttpsRedirection`, and
delete `Controllers/WeatherForecastController.cs` and `WeatherForecast.cs`. TLS terminates
at the ingress; leaving `UseHttpsRedirection` on breaks container health checks.

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test TasteZambia.API.Tests --filter FullyQualifiedName~ArchiveEndpointTests`
Expected: PASS, 10 tests.

- [ ] **Step 9: Verify by hand**

```bash
docker compose up -d --build
curl -s localhost:8080/api/v1/dishes | python3 -m json.tool | head -20
curl -si localhost:8080/api/v1/dishes | grep -i etag
curl -s localhost:8080/api/v1/dishes/ifisashi/recipe | python3 -m json.tool | head -30
curl -si localhost:8080/api/v1/ingredients/nope | head -3
```

Expected: eight dishes with `localName` first; an `ETag` header; the full Ifisashi recipe;
a `404` carrying `application/problem+json`.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "feat(api): add archive read endpoints with ETag support

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 7: Point the mobile app at the API

Stage 1's payoff. The mobile plan's Task 3 defined repository interfaces; this replaces the
seeded implementations with HTTP ones. **No View, ViewModel or service changes.**

**Files:**
- Create: `TasteZambia.Core/Data/Http/HttpArchiveRepositories.cs`, `Data/Http/ArchiveApiOptions.cs`
- Modify: `TasteZambia.Core/TasteZambia.Core.csproj` (reference `TasteZambia.Shared`), `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/Data/HttpRepositoryTests.cs`

- [ ] **Step 1: Map Shared DTOs to the mobile domain models**

`TasteZambia.Core` already has its own `Dish`, `Ingredient`, `Province` models (mobile plan
Task 2). Add a mapping layer rather than replacing them — the mobile models carry
presentation helpers (`MetaLabel`, `HasPhoto`) that do not belong in a wire contract.

```csharp
using TasteZambia.Core.Models;
using TasteZambia.Shared.Contracts.Dishes;

namespace TasteZambia.Core.Data.Http;

internal static class DtoMappings
{
    public static Dish ToModel(this DishDto d) => new()
    {
        Id = d.Id, LocalName = d.LocalName, EnglishName = d.EnglishName,
        Region = d.Region, TimeLabel = d.TimeLabel, Difficulty = d.Difficulty,
        Description = d.Description, ImageAsset = d.ImageAsset,
        PhotoNeededCaption = d.PhotoNeededCaption,
        PrepTime = d.PrepTime, CookTime = d.CookTime,
    };
}
```

- [ ] **Step 2: Write the HTTP repositories**

```csharp
using System.Net.Http.Json;
using TasteZambia.Core.Models;
using TasteZambia.Shared.Contracts.Dishes;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Data.Http;

public sealed class HttpDishRepository(HttpClient http) : IDishRepository
{
    public async Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken ct = default)
    {
        var dtos = await http.GetFromJsonAsync<List<DishDto>>(ApiRoutes.Dishes.Collection, ct);
        return dtos?.Select(d => d.ToModel()).ToList() ?? [];
    }

    public async Task<Dish?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var dto = await http.GetFromJsonAsync<DishDto>(
            ApiRoutes.Dishes.ById.Replace("{id}", Uri.EscapeDataString(id)), ct);
        return dto?.ToModel();
    }

    // GetRecipeAsync maps RecipeDto to the Core RecipeDetail the same way.
}
```

- [ ] **Step 3: Swap the registration**

In `MauiProgram.cs`, the *only* change Stage 1 requires of the app:

```csharp
builder.Services.AddHttpClient<IDishRepository, HttpDishRepository>(c =>
    c.BaseAddress = new Uri(ArchiveApiOptions.BaseUrl));
// … the same for the other five repositories …
```

`ArchiveApiOptions.BaseUrl` needs a per-platform default — the Android emulator reaches the
host at `http://10.0.2.2:8080`, the iOS simulator at `http://localhost:8080`:

```csharp
public static class ArchiveApiOptions
{
    public static string BaseUrl => DeviceInfo.Platform == DevicePlatform.Android
        ? "http://10.0.2.2:8080"
        : "http://localhost:8080";
}
```

Android also blocks cleartext HTTP by default. For local development add
`android:usesCleartextTraffic="true"` to the debug manifest only — never ship it.

- [ ] **Step 4: Verify end to end**

With `docker compose up -d` running, launch the app. Home, Explore, Recipe, Ingredients,
Ingredient, Regions, Culture and Story must render identically to the seeded build. Keep
`InMemory*Repository` registered in `TasteZambia.Core.Tests` so the 73 mobile tests continue
to run offline.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(mobile): read the archive from the API

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

# Stages 2–5 — scoped, not yet stepped

Each gets its own plan once Stage 1 is running. Recorded here so Stage 1's decisions stay
honest about what they have to support.

## Stage 2 — Identity and the personal layer

**Delivers:** the first authenticated user, and everything hanging off one.

- ASP.NET Core Identity on the same DbContext, JWT bearer tokens, refresh rotation.
- Roles the design implies: `contributor` (default), `reviewer` (archive team), `admin`.
- New tables: `AspNetUsers` et al., `UserProfile`, `SavedDish`, `WishlistEntry`, `CookedEntry`, `CookProgress`, `OnboardingChoices`.
- Endpoints: `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `GET|PUT /me`, `GET|PUT /me/onboarding`, `GET /me/collections/{saved|wishlist|cooked}`, `PUT /me/saved/{dishId}`, `PUT /me/progress/{dishId}/{step}`.
- **Unblocks** mobile Tasks 17 and 22, and makes `IFavouritesService` / `ICookingProgressService` real rather than in-memory.

**Decision to make first:** whether cook progress and favourites sync per-device or per-account. The design implies per-account (a heart set on Home shows on Explore), but offline cooking means a device must be able to toggle while disconnected and reconcile later. Last-write-wins on `UpdatedAt` is almost certainly right here — losing a heart toggle is not worth a merge UI.

## Stage 3 — Contributions and review

**Delivers:** the six-screen Share lifecycle.

- Tables: `Contribution`, `ContributionDraft`, `ContributionPhoto`, `ReviewAssignment`, `ReviewStep`, `FlaggedField`.
- The `ContributionStatus` state machine, enforced in `ContributionService` — not in a feature, because both the contributor endpoints and the reviewer endpoints must obey it: `Draft → InReview → (ChangesRequested → InReview)* → Published`, with `Withdrawn` reachable from any pre-published state.
- Endpoints: `GET|POST|PUT /me/drafts`, `POST /me/drafts/{id}/submit`, `GET /me/contributions/{id}/timeline`, and reviewer-only `GET /review/queue`, `POST /review/{id}/request-changes`, `POST /review/{id}/publish`.
- **Unblocks** mobile Tasks 15 and 20.

**Decision to make first:** whether publishing a contribution creates a real `Dish` row in the archive or a separate `CommunityDish`. Merging into `Dish` is simpler to read and matches the design's promise that a published recipe is *in the archive* — but it means the seeded editorial content and user content share a table and need a `Provenance` column.

## Stage 4 — Media and the family archive

**Delivers:** photos, audio, and the private family tier.

- Blob storage behind `IMediaStore` — local disk in Development, S3 or Azure Blob in production. The interface is what Stage 1's `ImageAsset` string columns become.
- Tables: `MediaAsset`, `FamilyRecipe`, `FamilyMember`, `FamilyNote`, `FamilyInvite`.
- Access control is the hard part: a family recipe is visible to its owner plus accepted members, and flips to public only on the owner's action. This must be enforced in a service, applied to every query — never in a feature.
- Endpoints: `POST /media` (multipart), `GET /media/{id}`, `GET|POST /me/family-recipes`, `POST /family-recipes/{id}/invite`, `POST /family-recipes/{id}/notes`, `PUT /family-recipes/{id}/privacy`.
- **Unblocks** mobile Tasks 16 and 21.

**Decision to make first:** whether audio is stored as one blob or chunked for streaming. A 12:40 recording is roughly 12 MB at a sane bitrate — one blob with range requests is fine and much simpler.

## Stage 5 — Transcription and delta sync

**Delivers:** the "Transcription pending" workflow, and genuine offline.

- A background queue (`Channel<T>` in-process first; a real broker only when a second instance exists) driving `Pending → Transcribing → AwaitingApproval → Approved`.
- The transcript is written by a human on the archive team, then approved by the contributor — the design is explicit that the family approves the text before it attaches.
- Delta sync: `GET /api/v1/sync/changes?since={cursor}` returning created/updated rows and a tombstone list, paging on the `UpdatedAt` column Stage 1 already stamps. Add a `DeletedRow` table at this point — soft deletes are what make tombstones possible, and retrofitting them after real deletions have happened loses history.
- **Unblocks** the offline promise on the onboarding screen.

**Decision to make first:** cursor shape. `UpdatedAt` alone is not safe under concurrent writes sharing a millisecond; a composite `(UpdatedAt, Id)` cursor is.

---

## Self-Review

**Scope coverage.** Stage 1 satisfies all five read-side repository interfaces the mobile
plan's Task 3 defines (`IDishRepository`, `IIngredientRepository`, `IRegionRepository`,
`IArticleRepository`, `ICategoryRepository`). `IProfileRepository` is deliberately excluded —
it needs a user, which is Stage 2. That is the one mobile repository still on seed data after
this plan, and mobile Task 17 (Profile) is the one screen still fed from `SeedData`.

**The dependency rule holds.** Every feature in Task 6 injects only repositories, services
and `HttpContext`. No feature references `TasteZambiaDbContext`. `CatalogService` takes
`IDishRepository`, not the context. `ArchiveVersionService` is the single deliberate
exception — it needs `MAX(UpdatedAt)` across tables, and pushing that into five repositories
to avoid one direct dependency would be worse. Flagged here so a reviewer sees it was a
decision, not an oversight.

**Placeholder scan.** Task 4's seeder is the one place with an explicit "port the rest"
instruction rather than 300 lines of literal content. That is deliberate: the content already
exists, transcribed and reviewed, in the mobile plan's `SeedData.cs`, and copying it a second
time into this document doubles the chance of drift in cultural copy. The porting *rules*
(ordering, which rows get recipes, the Katapa fallback) are stated in full.

**Type consistency.** `ArticleBlockKind` exists in both `TasteZambia.Shared.Contracts.Culture`
and `TasteZambia.API.Data.Entities`, cast between in `ArchiveMappings`. That duplication is
intentional — the entity enum is a storage concern and the DTO enum is a wire contract, and
fusing them would drag `Shared` into the persistence model. The cast is safe because both
declare the same three members with the same explicit values; a reviewer should check that
invariant if either changes.

**Three risks flagged for the executor.**
1. **`dotnet sln` corrupts the solution header** on SDK 10.0.201. Task 1 Step 5 adds a project; run the repair immediately after, and verify with `xxd`.
2. **`ApiFactory` builds a service provider inside `ConfigureServices`** to seed. That works but constructs a second container; if it causes trouble, move seeding into `IAsyncLifetime` on the collection fixture instead.
3. **`CatalogService.SearchAsync` loads all dishes and filters in memory.** Correct and parity-preserving at 8 rows; becomes wrong somewhere in the hundreds. The fix is a `tsvector` index inside `DishRepository`, and the service signature does not change.
