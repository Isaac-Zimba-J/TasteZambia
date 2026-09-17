# Stage 3 — Contributions and Review

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the six-screen Share lifecycle real: a recipe drafted on the phone is submitted to the archive team, reviewed, sent back with questions, resubmitted, and published into the archive as a real dish credited to its contributor.

**Architecture:** One `Contribution` aggregate on the API (header + ingredients + steps + review events + flagged fields) driven by a state machine in `ContributionService` that both the contributor and reviewer controllers must go through. Publishing writes a real `Dish` + `Recipe` into the archive with `Provenance = Community`, so Explore, search, regions, ETags and Stage 5 sync see it for free. On the phone, **drafts are local** (in `ILocalStore`, like Stage 2's personal data); the server only hears about a contribution when it is submitted. Everything after submission (timeline, questions, resubmit, withdraw, published stats) reads and writes the account.

**Tech Stack:** Same as Stages 1–2. No new packages.

**Spec:** `Docs/plans/2026-09-09-backend-api-plan.md` — "Stage 3 — Contributions and review", plus the decisions below.
**Builds on:** Stage 2 (`ICurrentUser`, `[Authorize]`, roles, `AuthenticatedHandler`, `ILocalStore`, the `"me"` HttpClient).

## Decisions recorded

1. **Published contributions become `Dish` rows** (user's choice). `Dish` gains `Provenance` (`Editorial | Community`) and `ContributionId`. Seeded content is `Editorial`.
2. **Drafts are local, not server-side.** The backend plan sketched `GET|POST|PUT /me/drafts`; nothing in the design needs a draft on two devices, and a local draft works with no signal (the user's standing "write locally first" preference). Only submission touches the API. If cross-device drafts are ever wanted they become one more `/me/sync` kind.
3. **No `ReviewAssignment` table.** The reviewer's name is recorded on each `ReviewEvent`; the design shows "Reviewed by Namakau Sitali" as a line of history, not an assignment to manage.
4. **Photos wait for Stage 4.** `ContributionDraft.PhotoPaths` stays client-only; `ContributionPhoto` is not created here.
5. **A reviewer account is seeded in Development** from `appsettings.Development.json` so the whole loop can be driven from Scalar on this machine. Production assigns the role through the admin endpoint.

## Global Constraints

### Carried from Stages 1–2
- Dependency rule: `Controllers/ → Services/ → Repositories/ → DbContext`. State transitions live **only** in `ContributionService`; controllers never set `Status`.
- Wire types live in `TasteZambia.Shared/Contracts/`; changes are additive. Validation attributes go on record **constructor parameters**, not `[property:]` (MVC ignores the latter on positional records).
- No `[Produces("application/json")]`. Problem details for errors. Invalid state transitions are **409 Conflict**.
- `TasteZambia.Core` references nothing MAUI. Seed/design copy is never paraphrased.
- Repair the `.sln` header after any `dotnet sln` command.

### New for Stage 3
- **The state machine** (enforced in `ContributionService`, tested exhaustively in Task 2):
  `Draft → InReview` (submit) · `InReview → ChangesRequested` (request-changes) · `ChangesRequested → InReview` (resubmit) · `InReview → Published` (publish) · any of `{InReview, ChangesRequested} → Withdrawn` (withdraw). Nothing leaves `Published` or `Withdrawn`. `Draft` never exists on the server — a submission arrives as `InReview`.
- **Reviewer routes require the `reviewer` role** (`[Authorize(Roles = "reviewer")]`); contributor routes require only a signed-in user and only ever touch that user's own contributions (404 for anyone else's, never 403 — do not leak existence).
- **Publishing is idempotent per contribution**: a second publish is a 409, never a second dish.
- **Dish ids are slugs** of the local name, lowercase `[a-z0-9-]`, made unique with `-2`, `-3`… on collision.
- **Timestamps come from `TimeProvider`**, never `DateTimeOffset.UtcNow` in services (tests pin the clock).

---

## File Structure

### `TasteZambia.Shared`
| Path | Holds |
|---|---|
| `Enums/ArchiveEnums.cs` | `+ Provenance`, `+ ReviewEventKind` |
| `Contracts/Contributions/ContributionContracts.cs` | `SubmitContributionRequest`, `ContributionIngredientDto`, `ContributionSummaryDto`, `ContributionDetailDto`, `ReviewEventDto`, `FlaggedFieldDto`, `ResubmitRequest`, `FlagAnswerDto` |
| `Contracts/Review/ReviewContracts.cs` | `ReviewQueueItemDto`, `RequestChangesRequest`, `FlagRequestDto`, `PublishResultDto` |
| `Contracts/Dishes/DishContracts.cs` | `DishDto` gains `Provenance` |
| `Routes/ApiRoutes.cs` | `Me.Contributions`, `Me.ContributionById`, `Me.Resubmit`, `Me.Withdraw`; `Review.Queue`, `Review.RequestChanges`, `Review.Publish`; `Admin.UserRoles` |

### `TasteZambia.API`
| Path | Holds |
|---|---|
| `Data/Entities/Contributions.cs` | `Contribution`, `ContributionIngredient`, `ContributionStep`, `ReviewEvent`, `FlaggedField` |
| `Data/Entities/Archive.cs` | `Dish.Provenance`, `Dish.ContributionId` |
| `Data/Configurations/ContributionConfigurations.cs` | Tables, keys, cascades |
| `Data/Seed/ReviewerSeeder.cs` | Development reviewer account from config |
| `Migrations/*_Contributions` | |
| `Repositories/ContributionRepository.cs` | `IContributionRepository` |
| `Services/ContributionService.cs` | `IContributionService` — the state machine and publishing |
| `Services/InvalidContributionTransitionException.cs` | → 409 |
| `Common/Http/ControllerResultExtensions.cs` | `+ ConflictProblem` |
| `Controllers/MeContributionsController.cs` | contributor routes |
| `Controllers/ReviewController.cs` | reviewer routes |
| `Controllers/AdminController.cs` | `PUT /admin/users/{userName}/roles` |
| `Mapping/ContributionMappings.cs` | entity → DTO |

### `TasteZambia.Core`
| Path | Holds |
|---|---|
| `Models/ContributionDraft.cs` | `+ LocalDraft` (id, edited-at, draft), `RecipeDraft.Completion(draft)` |
| `Services/DraftStore.cs` | Local drafts over `ILocalStore` |
| `Services/ContributionService.cs` | **Rewritten**: drafts from `DraftStore`, everything else over HTTP |
| `Data/Http/HttpProfileRepository.cs` | `GetContributionsAsync` → `GET /me/contributions` |
| `ViewModels/ShareViewModel.cs` | Draft autosave; submit posts |
| `ViewModels/ShareLifecycleViewModels.cs` | Review/Changes/Published bound to a real contribution |

### Tests
| Path | Covers |
|---|---|
| `TasteZambia.API.Tests/Services/ContributionStateMachineTests.cs` | Every legal and illegal transition; publish creates the dish; slug uniqueness |
| `TasteZambia.API.Tests/Controllers/ContributionEndpointTests.cs` | Submit, list, detail, resubmit, withdraw, other-user 404, 409 |
| `TasteZambia.API.Tests/Controllers/ReviewEndpointTests.cs` | Role gate, queue, request-changes, publish → visible in `/dishes` |
| `TasteZambia.Core.Tests/Services/DraftStoreTests.cs` | Persistence, completion %, ordering |
| `TasteZambia.Core.Tests/ViewModels/ShareLifecycleTests.cs` | Updated for the async service |

---

## Task 1: Schema — contribution entities, `Dish.Provenance`, migration

**Files:**
- Modify: `TasteZambia.Shared/Enums/ArchiveEnums.cs`, `TasteZambia.API/Data/Entities/Archive.cs`, `TasteZambia.API/Data/TasteZambiaDbContext.cs`, `TasteZambia.API/Data/Configurations/ArchiveConfigurations.cs`
- Create: `TasteZambia.API/Data/Entities/Contributions.cs`, `TasteZambia.API/Data/Configurations/ContributionConfigurations.cs`
- Test: `TasteZambia.API.Tests/Data/SchemaTests.cs` (add one)

**Interfaces:**
- Produces:
  - `enum Provenance { Editorial = 0, Community = 1 }`; `enum ReviewEventKind { Submitted, Read, ChangesRequested, Resubmitted, Published, Withdrawn }`
  - `Dish.Provenance` (default `Editorial`), `Dish.ContributionId` (`Guid?`)
  - `Contribution { Guid Id; string UserId; ContributionStatus Status; string LocalName, EnglishDescription, Province, MealType, Language, Origin, CulturalSignificance, TraditionalMethod, TaughtBy, TaughtByOrigin; bool CreditTeacher; string ContributorName, ContributorLocation; DateTimeOffset SubmittedAt, UpdatedAt; string? PublishedDishId; List<ContributionIngredient> Ingredients; List<ContributionStep> Steps; List<ReviewEvent> Events; List<FlaggedField> Flags }`
  - `ContributionIngredient { int Id; Guid ContributionId; int SortOrder; string? IngredientKey; string DisplayName, DisplaySubtitle, Quantity }`
  - `ContributionStep { int Id; Guid ContributionId; int SortOrder; string Text }`
  - `ReviewEvent { int Id; Guid ContributionId; ReviewEventKind Kind; DateTimeOffset At; string? Actor; string? Note }`
  - `FlaggedField { int Id; Guid ContributionId; string Field, Question, CurrentValue; string? Answer; DateTimeOffset? AnsweredAt }`
  - `DbSet<Contribution> Contributions`

- [ ] **Step 1: Write the failing test**

Append to `SchemaTests.cs` (keep its existing style — it uses `fixture.NewContext()`):

```csharp
[Fact]
public async Task Contribution_RoundTripsWithChildrenAndDishProvenanceDefaultsToEditorial()
{
    await using var db = fixture.NewContext();
    var user = new ArchiveUser { UserName = $"device-{Guid.NewGuid():N}" };
    db.Users.Add(user);

    var c = new Contribution
    {
        UserId = user.Id, Status = ContributionStatus.InReview,
        LocalName = "Chibwabwa na Mbalala", EnglishDescription = "Pumpkin leaves cooked with pounded groundnuts and nothing else",
        Province = "Northern", MealType = "Relish", ContributorName = "Chanda Mwaba", ContributorLocation = "Kitwe",
        SubmittedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
        Ingredients = { new() { SortOrder = 0, IngredientKey = "chibwabwa", DisplayName = "Chibwabwa", DisplaySubtitle = "Pumpkin leaves", Quantity = "2 bundles" } },
        Steps = { new() { SortOrder = 0, Text = "Shred the leaves fine and rinse them twice." } },
        Events = { new() { Kind = ReviewEventKind.Submitted, At = DateTimeOffset.UtcNow } },
    };
    db.Contributions.Add(c);
    await db.SaveChangesAsync();

    await using var read = fixture.NewContext();
    var back = await read.Contributions.Include(x => x.Ingredients).Include(x => x.Steps).Include(x => x.Events).Include(x => x.Flags)
        .SingleAsync(x => x.Id == c.Id);
    Assert.Single(back.Ingredients);
    Assert.Single(back.Steps);
    Assert.Single(back.Events);
    Assert.Empty(back.Flags);

    var dish = await read.Dishes.FirstAsync();
    Assert.Equal(Provenance.Editorial, dish.Provenance);
    Assert.Null(dish.ContributionId);
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter Contribution_RoundTrips`
Expected: FAIL — `Contribution` does not exist.

- [ ] **Step 3: Add the enums and the `Dish` columns**

`ArchiveEnums.cs` — append:

```csharp
/// <summary>Where a dish came from. Seeded editorial content, or a published community contribution.</summary>
public enum Provenance
{
    Editorial = 0,
    Community = 1,
}

/// <summary>One line of a contribution's review history.</summary>
public enum ReviewEventKind
{
    Submitted = 0,
    Read = 1,
    ChangesRequested = 2,
    Resubmitted = 3,
    Published = 4,
    Withdrawn = 5,
}
```

`Archive.cs` — on `Dish`, after `SortOrder`:

```csharp
    public Provenance Provenance { get; set; } = Provenance.Editorial;
    /// <summary>Set when this dish was published from a community contribution.</summary>
    public Guid? ContributionId { get; set; }
```

(add `using TasteZambia.Shared.Enums;` at the top of `Archive.cs`.)

In `DishConfiguration` (ArchiveConfigurations.cs) add: `e.Property(x => x.Provenance).HasConversion<int>(); e.HasIndex(x => x.ContributionId).IsUnique().HasFilter("\"ContributionId\" IS NOT NULL");`

- [ ] **Step 4: Write the entities**

`TasteZambia.API/Data/Entities/Contributions.cs`:

```csharp
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Data.Entities;

/// <summary>
/// A recipe submitted for the public archive. Arrives as InReview (drafts live on the
/// phone), moves through the state machine in ContributionService, and on Published
/// becomes a Dish + Recipe with Provenance.Community.
/// </summary>
public class Contribution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public ContributionStatus Status { get; set; }

    public required string LocalName { get; set; }
    public required string EnglishDescription { get; set; }
    public required string Province { get; set; }
    public string MealType { get; set; } = "";
    public string Language { get; set; } = "";
    public string Origin { get; set; } = "";
    public string CulturalSignificance { get; set; } = "";
    public string TraditionalMethod { get; set; } = "";
    public string TaughtBy { get; set; } = "";
    public string TaughtByOrigin { get; set; } = "";
    public bool CreditTeacher { get; set; } = true;

    // Snapshot of the profile at submission: the credit line must not change if the profile does.
    public required string ContributorName { get; set; }
    public required string ContributorLocation { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? PublishedDishId { get; set; }

    public List<ContributionIngredient> Ingredients { get; set; } = [];
    public List<ContributionStep> Steps { get; set; } = [];
    public List<ReviewEvent> Events { get; set; } = [];
    public List<FlaggedField> Flags { get; set; } = [];
}

public class ContributionIngredient
{
    public int Id { get; set; }
    public Guid ContributionId { get; set; }
    public int SortOrder { get; set; }
    public string? IngredientKey { get; set; }
    public required string DisplayName { get; set; }
    public required string DisplaySubtitle { get; set; }
    public required string Quantity { get; set; }
}

public class ContributionStep
{
    public int Id { get; set; }
    public Guid ContributionId { get; set; }
    public int SortOrder { get; set; }
    public required string Text { get; set; }
}

public class ReviewEvent
{
    public int Id { get; set; }
    public Guid ContributionId { get; set; }
    public ReviewEventKind Kind { get; set; }
    public DateTimeOffset At { get; set; }
    /// <summary>The reviewer's display name for review events; null for contributor actions.</summary>
    public string? Actor { get; set; }
    public string? Note { get; set; }
}

/// <summary>A question from the reviewer about one field. Answered by the contributor on resubmit.</summary>
public class FlaggedField
{
    public int Id { get; set; }
    public Guid ContributionId { get; set; }
    public required string Field { get; set; }
    public required string Question { get; set; }
    public required string CurrentValue { get; set; }
    public string? Answer { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
}
```

`TasteZambia.API/Data/Configurations/ContributionConfigurations.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Configurations;

public class ContributionConfiguration : IEntityTypeConfiguration<Contribution>
{
    public void Configure(EntityTypeBuilder<Contribution> e)
    {
        e.ToTable("contributions");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.UserId, x.SubmittedAt });
        e.HasIndex(x => x.Status);
        e.Property(x => x.Status).HasConversion<int>();
        e.Property(x => x.LocalName).HasMaxLength(120);
        e.Property(x => x.EnglishDescription).HasMaxLength(400);
        e.Property(x => x.Province).HasMaxLength(40);
        e.Property(x => x.MealType).HasMaxLength(40);
        e.Property(x => x.Language).HasMaxLength(40);
        e.Property(x => x.ContributorName).HasMaxLength(120);
        e.Property(x => x.ContributorLocation).HasMaxLength(120);
        e.Property(x => x.PublishedDishId).HasMaxLength(80);
        e.HasOne<ArchiveUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Ingredients).WithOne().HasForeignKey(x => x.ContributionId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Steps).WithOne().HasForeignKey(x => x.ContributionId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.ContributionId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Flags).WithOne().HasForeignKey(x => x.ContributionId).OnDelete(DeleteBehavior.Cascade);
        e.UseXminConcurrency();
    }
}

public class ContributionIngredientConfiguration : IEntityTypeConfiguration<ContributionIngredient>
{
    public void Configure(EntityTypeBuilder<ContributionIngredient> e)
    {
        e.ToTable("contribution_ingredients");
        e.Property(x => x.IngredientKey).HasMaxLength(64);
        e.Property(x => x.DisplayName).HasMaxLength(120);
        e.Property(x => x.DisplaySubtitle).HasMaxLength(120);
        e.Property(x => x.Quantity).HasMaxLength(80);
    }
}

public class ContributionStepConfiguration : IEntityTypeConfiguration<ContributionStep>
{
    public void Configure(EntityTypeBuilder<ContributionStep> e) => e.ToTable("contribution_steps");
}

public class ReviewEventConfiguration : IEntityTypeConfiguration<ReviewEvent>
{
    public void Configure(EntityTypeBuilder<ReviewEvent> e)
    {
        e.ToTable("review_events");
        e.Property(x => x.Kind).HasConversion<int>();
        e.Property(x => x.Actor).HasMaxLength(120);
        e.HasIndex(x => new { x.ContributionId, x.At });
    }
}

public class FlaggedFieldConfiguration : IEntityTypeConfiguration<FlaggedField>
{
    public void Configure(EntityTypeBuilder<FlaggedField> e)
    {
        e.ToTable("flagged_fields");
        e.Property(x => x.Field).HasMaxLength(80);
    }
}
```

`ConcurrencyExtensions` is `internal static` in `ArchiveConfigurations.cs` — same assembly, fine.

Add to the context: `public DbSet<Contribution> Contributions => Set<Contribution>();`

- [ ] **Step 5: Migrate and run the test**

```bash
export PATH="/usr/local/share/dotnet:$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add Contributions --project TasteZambia.API
dotnet ef database update --project TasteZambia.API
dotnet test TasteZambia.API.Tests
```
Expected: all green (59 + 1). Check the migration adds `Provenance` with default `0` so seeded rows stay Editorial.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(api): contribution entities, review events, Dish.Provenance

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 2: The state machine and publishing

**Files:**
- Create: `TasteZambia.API/Repositories/ContributionRepository.cs`, `TasteZambia.API/Services/ContributionService.cs`, `TasteZambia.API/Services/InvalidContributionTransitionException.cs`
- Create: `TasteZambia.Shared/Contracts/Contributions/ContributionContracts.cs` (request side only for now)
- Modify: `Program.cs` (register)
- Test: `TasteZambia.API.Tests/Services/ContributionStateMachineTests.cs`

**Interfaces:**
- Produces:
  - `SubmitContributionRequest(string LocalName, string EnglishDescription, string Province, string MealType, string Language, IReadOnlyList<ContributionIngredientDto> Ingredients, IReadOnlyList<string> Steps, string Origin, string CulturalSignificance, string TraditionalMethod, string TaughtBy, string TaughtByOrigin, bool CreditTeacher)` — `LocalName` `[Required, MaxLength(120)]`, `Province` `[Required]`, `Steps` `[MinLength(1)]`
  - `ContributionIngredientDto(string? IngredientKey, string DisplayName, string DisplaySubtitle, string Quantity)`
  - `FlagAnswerDto(int FlagId, string Answer)`; `FlagRequestDto(string Field, string Question, string CurrentValue)`
  - `IContributionRepository`: `Task<Contribution?> GetAsync(Guid id, ct)` (full graph, tracked), `Task<Contribution?> GetForUserAsync(Guid id, string userId, ct)`, `Task<IReadOnlyList<Contribution>> ListForUserAsync(string userId, ct)`, `Task<IReadOnlyList<Contribution>> ListByStatusAsync(ContributionStatus status, ct)`, `void Add(Contribution)`, `Task<bool> DishIdExistsAsync(string id, ct)`, `Task SaveChangesAsync(ct)`
  - `IContributionService`:
    - `Task<Contribution> SubmitAsync(string userId, SubmitContributionRequest request, ct)` → `InReview`, `Submitted` event, contributor snapshot from `IUserProfileRepository`
    - `Task<Contribution> RequestChangesAsync(Guid id, string reviewerName, string note, IReadOnlyList<FlagRequestDto> flags, ct)` → `ChangesRequested`
    - `Task<Contribution> ResubmitAsync(Guid id, string userId, IReadOnlyList<FlagAnswerDto> answers, ct)` → `InReview`; every open flag must be answered (400 otherwise via `ArgumentException`)
    - `Task<Contribution> PublishAsync(Guid id, string reviewerName, ct)` → `Published`, creates `Dish` + `Recipe`, sets `PublishedDishId`
    - `Task<Contribution> WithdrawAsync(Guid id, string userId, ct)` → `Withdrawn`
    - `Task MarkReadAsync(Guid id, string reviewerName, ct)` → adds a `Read` event once (idempotent), no status change
  - `InvalidContributionTransitionException(ContributionStatus from, string action)`
  - Contributor name when the profile has none: `"A Taste Zambia contributor"`

- [ ] **Step 1: Write the failing tests**

```csharp
using Microsoft.Extensions.Time.Testing;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Contracts.Review;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Tests.Services;

[Collection(nameof(DatabaseCollection))]
public class ContributionStateMachineTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

    private async Task<(ContributionService svc, string userId, FakeTimeProvider clock)> SutAsync()
    {
        var db = fixture.NewContext();
        var user = new ArchiveUser { UserName = $"device-{Guid.NewGuid():N}" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var clock = new FakeTimeProvider(T0);
        return (new ContributionService(new ContributionRepository(db), new UserProfileRepository(db), clock), user.Id, clock);
    }

    private static SubmitContributionRequest Chibwabwa() => new(
        "Chibwabwa na Mbalala", "Pumpkin leaves cooked with pounded groundnuts and nothing else", "Northern", "Relish", "",
        [new("chibwabwa", "Chibwabwa", "Pumpkin leaves", "2 bundles"), new(null, "Salt", "Mucele", "To taste")],
        ["Shred the leaves fine and rinse them twice.", "Pound the groundnuts until the oil starts to show."],
        "Cooked in Mungwi and the villages around Kasama.", "This is the relish cooked when there is no money for meat.",
        "Clay pot on charcoal.", "", "", true);

    [Fact]
    public async Task Submit_ArrivesInReviewWithASubmittedEvent()
    {
        var (svc, uid, _) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);

        Assert.Equal(ContributionStatus.InReview, c.Status);
        Assert.Equal(T0, c.SubmittedAt);
        Assert.Single(c.Events, e => e.Kind == ReviewEventKind.Submitted);
        Assert.Equal("A Taste Zambia contributor", c.ContributorName);   // profile has no name yet
        Assert.Equal(2, c.Ingredients.Count);
        Assert.Equal(2, c.Steps.Count);
    }

    [Fact]
    public async Task RequestChanges_ThenResubmit_ThenPublish_IsTheHappyPath()
    {
        var (svc, uid, clock) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);

        clock.Advance(TimeSpan.FromDays(1));
        c = await svc.RequestChangesAsync(c.Id, "Namakau Sitali", "Two things I want to get right.",
            [new("Local name", "Is this the Mungwi name or the Kasama town name?", "Chibwabwa na Mbalala")], default);
        Assert.Equal(ContributionStatus.ChangesRequested, c.Status);
        var flag = Assert.Single(c.Flags);

        clock.Advance(TimeSpan.FromDays(1));
        c = await svc.ResubmitAsync(c.Id, uid, [new(flag.Id, "Mungwi specifically.")], default);
        Assert.Equal(ContributionStatus.InReview, c.Status);
        Assert.Equal("Mungwi specifically.", c.Flags.Single().Answer);

        clock.Advance(TimeSpan.FromDays(1));
        c = await svc.PublishAsync(c.Id, "Namakau Sitali", default);
        Assert.Equal(ContributionStatus.Published, c.Status);
        Assert.Equal("chibwabwa-na-mbalala", c.PublishedDishId);
        Assert.Equal([ReviewEventKind.Submitted, ReviewEventKind.ChangesRequested, ReviewEventKind.Resubmitted, ReviewEventKind.Published],
            c.Events.OrderBy(e => e.At).Select(e => e.Kind));
    }

    [Fact]
    public async Task Publish_CreatesACommunityDishWithARecipe()
    {
        var (svc, uid, _) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);
        await svc.PublishAsync(c.Id, "Namakau Sitali", default);

        await using var db = fixture.NewContext();
        var dish = await db.Dishes.SingleAsync(d => d.Id == "chibwabwa-na-mbalala");
        Assert.Equal(Provenance.Community, dish.Provenance);
        Assert.Equal(c.Id, dish.ContributionId);
        Assert.Equal("Northern", dish.Region);

        var recipe = await new DishRepository(db).GetRecipeAsync(dish.Id);
        Assert.NotNull(recipe);
        Assert.True(recipe!.IsVerified);
        Assert.Equal(2, recipe.Ingredients.Count);
        Assert.Equal(2, recipe.Steps.Count);
        Assert.Equal("Shred the leaves fine and rinse them twice.", recipe.Steps[0].Body);
    }

    [Fact]
    public async Task Publish_Twice_Is409NotASecondDish()
    {
        var (svc, uid, _) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);
        await svc.PublishAsync(c.Id, "Namakau Sitali", default);

        await Assert.ThrowsAsync<InvalidContributionTransitionException>(() => svc.PublishAsync(c.Id, "Namakau Sitali", default));

        await using var db = fixture.NewContext();
        Assert.Equal(1, await db.Dishes.CountAsync(d => d.ContributionId == c.Id));
    }

    [Fact]
    public async Task Publish_WithACollidingName_GetsANumberedSlug()
    {
        var (svc, uid, _) = await SutAsync();
        var a = await svc.SubmitAsync(uid, Chibwabwa(), default);
        var b = await svc.SubmitAsync(uid, Chibwabwa(), default);
        await svc.PublishAsync(a.Id, "R", default);
        var second = await svc.PublishAsync(b.Id, "R", default);

        Assert.Equal("chibwabwa-na-mbalala-2", second.PublishedDishId);
    }

    [Fact]
    public async Task Resubmit_WithAnUnansweredFlag_IsRejected()
    {
        var (svc, uid, _) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);
        c = await svc.RequestChangesAsync(c.Id, "R", "note", [new("Local name", "q", "v"), new("Cooking step 2", "q2", "v2")], default);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.ResubmitAsync(c.Id, uid, [new(c.Flags[0].Id, "only one")], default));
    }

    [Theory]
    [InlineData("requestChanges", ContributionStatus.ChangesRequested)]
    [InlineData("publish", ContributionStatus.ChangesRequested)]
    [InlineData("resubmit", ContributionStatus.InReview)]
    [InlineData("withdraw", ContributionStatus.Published)]
    [InlineData("requestChanges", ContributionStatus.Withdrawn)]
    public async Task IllegalTransitions_Throw(string action, ContributionStatus from)
    {
        var (svc, uid, _) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);
        await ForceStatusAsync(c.Id, from);

        Task Act() => action switch
        {
            "requestChanges" => svc.RequestChangesAsync(c.Id, "R", "n", [], default),
            "publish" => svc.PublishAsync(c.Id, "R", default),
            "resubmit" => svc.ResubmitAsync(c.Id, uid, [], default),
            "withdraw" => svc.WithdrawAsync(c.Id, uid, default),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };
        await Assert.ThrowsAsync<InvalidContributionTransitionException>(Act);
    }

    [Fact]
    public async Task Withdraw_FromInReviewAndFromChangesRequested_Works()
    {
        var (svc, uid, _) = await SutAsync();
        var a = await svc.SubmitAsync(uid, Chibwabwa(), default);
        Assert.Equal(ContributionStatus.Withdrawn, (await svc.WithdrawAsync(a.Id, uid, default)).Status);

        var b = await svc.SubmitAsync(uid, Chibwabwa(), default);
        await svc.RequestChangesAsync(b.Id, "R", "n", [], default);
        Assert.Equal(ContributionStatus.Withdrawn, (await svc.WithdrawAsync(b.Id, uid, default)).Status);
    }

    [Fact]
    public async Task MarkRead_AddsOneReadEventOnly()
    {
        var (svc, uid, _) = await SutAsync();
        var c = await svc.SubmitAsync(uid, Chibwabwa(), default);
        await svc.MarkReadAsync(c.Id, "Namakau Sitali", default);
        await svc.MarkReadAsync(c.Id, "Namakau Sitali", default);

        await using var db = fixture.NewContext();
        Assert.Equal(1, await db.Set<ReviewEvent>().CountAsync(e => e.ContributionId == c.Id && e.Kind == ReviewEventKind.Read));
    }

    private async Task ForceStatusAsync(Guid id, ContributionStatus status)
    {
        await using var db = fixture.NewContext();
        var c = await db.Contributions.SingleAsync(x => x.Id == id);
        c.Status = status;
        await db.SaveChangesAsync();
    }
}
```

Add `Microsoft.Extensions.TimeProvider.Testing` to `TasteZambia.API.Tests`.

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test TasteZambia.API.Tests --filter ContributionStateMachineTests`
Expected: FAIL.

- [ ] **Step 3: Write the contracts (request side)**

`TasteZambia.Shared/Contracts/Contributions/ContributionContracts.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Shared.Contracts.Contributions;

public sealed record ContributionIngredientDto(
    string? IngredientKey,
    [Required, MaxLength(120)] string DisplayName,
    [MaxLength(120)] string DisplaySubtitle,
    [MaxLength(80)] string Quantity);

/// <summary>A draft leaving the phone. Arrives on the server already InReview.</summary>
public sealed record SubmitContributionRequest(
    [Required, MaxLength(120)] string LocalName,
    [Required, MaxLength(400)] string EnglishDescription,
    [Required, MaxLength(40)] string Province,
    [MaxLength(40)] string MealType,
    [MaxLength(40)] string Language,
    IReadOnlyList<ContributionIngredientDto> Ingredients,
    [MinLength(1)] IReadOnlyList<string> Steps,
    string Origin,
    string CulturalSignificance,
    string TraditionalMethod,
    string TaughtBy,
    string TaughtByOrigin,
    bool CreditTeacher);

public sealed record FlagAnswerDto(int FlagId, [Required] string Answer);
public sealed record ResubmitRequest(IReadOnlyList<FlagAnswerDto> Answers);

public sealed record ReviewEventDto(ReviewEventKind Kind, DateTimeOffset At, string? Actor, string? Note);
public sealed record FlaggedFieldDto(int Id, string Field, string Question, string CurrentValue, string? Answer);

public sealed record ContributionSummaryDto(
    Guid Id, string LocalName, string Province, ContributionStatus Status,
    DateTimeOffset SubmittedAt, DateTimeOffset UpdatedAt, string? PublishedDishId);

public sealed record ContributionDetailDto(
    Guid Id, string LocalName, string EnglishDescription, string Province, ContributionStatus Status,
    DateTimeOffset SubmittedAt, string? PublishedDishId, string ContributorName, string ContributorLocation,
    string TaughtBy, string TaughtByOrigin, bool CreditTeacher,
    IReadOnlyList<ReviewEventDto> Events, IReadOnlyList<FlaggedFieldDto> Flags);
```

`TasteZambia.Shared/Contracts/Review/ReviewContracts.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Shared.Contracts.Review;

public sealed record FlagRequestDto([Required, MaxLength(80)] string Field, [Required] string Question, string CurrentValue);

public sealed record RequestChangesRequest([Required] string Note, IReadOnlyList<FlagRequestDto> Flags);

public sealed record ReviewQueueItemDto(
    Guid Id, string LocalName, string Province, string ContributorName, ContributionStatus Status,
    DateTimeOffset SubmittedAt, int OpenFlags);

public sealed record PublishResultDto(Guid ContributionId, string DishId);
```

- [ ] **Step 4: Write the repository, exception and service**

`Repositories/ContributionRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Repositories;

public interface IContributionRepository
{
    /// <summary>Tracked, full graph. For the service's transitions.</summary>
    Task<Contribution?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Contribution?> GetForUserAsync(Guid id, string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Contribution>> ListForUserAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Contribution>> ListByStatusAsync(ContributionStatus status, CancellationToken ct = default);
    void Add(Contribution contribution);
    Task<bool> DishIdExistsAsync(string dishId, CancellationToken ct = default);
    void AddDish(Dish dish);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class ContributionRepository(TasteZambiaDbContext db) : IContributionRepository
{
    private IQueryable<Contribution> Graph => db.Contributions
        .Include(c => c.Ingredients.OrderBy(i => i.SortOrder))
        .Include(c => c.Steps.OrderBy(s => s.SortOrder))
        .Include(c => c.Events.OrderBy(e => e.At))
        .Include(c => c.Flags.OrderBy(f => f.Id));

    public Task<Contribution?> GetAsync(Guid id, CancellationToken ct = default)
        => Graph.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Contribution?> GetForUserAsync(Guid id, string userId, CancellationToken ct = default)
        => Graph.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

    public async Task<IReadOnlyList<Contribution>> ListForUserAsync(string userId, CancellationToken ct = default)
        => await Graph.AsNoTracking().Where(c => c.UserId == userId).OrderByDescending(c => c.SubmittedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Contribution>> ListByStatusAsync(ContributionStatus status, CancellationToken ct = default)
        => await Graph.AsNoTracking().Where(c => c.Status == status).OrderBy(c => c.SubmittedAt).ToListAsync(ct);

    public void Add(Contribution contribution) => db.Contributions.Add(contribution);
    public Task<bool> DishIdExistsAsync(string dishId, CancellationToken ct = default) => db.Dishes.AnyAsync(d => d.Id == dishId, ct);
    public void AddDish(Dish dish) => db.Dishes.Add(dish);
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
```

`Services/InvalidContributionTransitionException.cs`:

```csharp
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Services;

/// <summary>The contribution is not in a state that allows this action. Surfaces as 409 Conflict.</summary>
public sealed class InvalidContributionTransitionException(ContributionStatus from, string action)
    : Exception($"Cannot {action} a contribution that is {from}.")
{
    public ContributionStatus From { get; } = from;
    public string Action { get; } = action;
}
```

`Services/ContributionService.cs`:

```csharp
using System.Text;
using System.Text.RegularExpressions;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Contracts.Review;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Services;

public interface IContributionService
{
    Task<Contribution> SubmitAsync(string userId, SubmitContributionRequest request, CancellationToken ct);
    Task<Contribution> RequestChangesAsync(Guid id, string reviewerName, string note, IReadOnlyList<FlagRequestDto> flags, CancellationToken ct);
    Task<Contribution> ResubmitAsync(Guid id, string userId, IReadOnlyList<FlagAnswerDto> answers, CancellationToken ct);
    Task<Contribution> PublishAsync(Guid id, string reviewerName, CancellationToken ct);
    Task<Contribution> WithdrawAsync(Guid id, string userId, CancellationToken ct);
    Task MarkReadAsync(Guid id, string reviewerName, CancellationToken ct);
}

/// <summary>
/// The one place a contribution's Status changes. Contributor and reviewer controllers
/// both come through here, so neither can skip a step. Publishing writes the dish into
/// the archive itself: a published recipe IS an archive entry, not a copy of one.
/// </summary>
public sealed partial class ContributionService(
    IContributionRepository contributions,
    IUserProfileRepository profiles,
    TimeProvider clock) : IContributionService
{
    public const string AnonymousContributor = "A Taste Zambia contributor";

    public async Task<Contribution> SubmitAsync(string userId, SubmitContributionRequest r, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var profile = await profiles.GetOrCreateProfileAsync(userId, ct);

        var c = new Contribution
        {
            UserId = userId,
            Status = ContributionStatus.InReview,
            LocalName = r.LocalName.Trim(),
            EnglishDescription = r.EnglishDescription.Trim(),
            Province = r.Province, MealType = r.MealType, Language = r.Language,
            Origin = r.Origin, CulturalSignificance = r.CulturalSignificance, TraditionalMethod = r.TraditionalMethod,
            TaughtBy = r.TaughtBy, TaughtByOrigin = r.TaughtByOrigin, CreditTeacher = r.CreditTeacher,
            ContributorName = profile.DisplayName.Length > 0 ? profile.DisplayName : AnonymousContributor,
            ContributorLocation = profile.Location,
            SubmittedAt = now, UpdatedAt = now,
            Ingredients = r.Ingredients.Select((i, n) => new ContributionIngredient
            {
                SortOrder = n, IngredientKey = i.IngredientKey, DisplayName = i.DisplayName,
                DisplaySubtitle = i.DisplaySubtitle, Quantity = i.Quantity,
            }).ToList(),
            Steps = r.Steps.Select((s, n) => new ContributionStep { SortOrder = n, Text = s }).ToList(),
            Events = { new ReviewEvent { Kind = ReviewEventKind.Submitted, At = now } },
        };

        contributions.Add(c);
        await contributions.SaveChangesAsync(ct);
        return c;
    }

    public async Task<Contribution> RequestChangesAsync(Guid id, string reviewerName, string note, IReadOnlyList<FlagRequestDto> flags, CancellationToken ct)
    {
        var c = await RequireAsync(id, ct);
        Require(c, ContributionStatus.InReview, "request changes on");

        var now = clock.GetUtcNow();
        c.Status = ContributionStatus.ChangesRequested;
        c.UpdatedAt = now;
        c.Events.Add(new ReviewEvent { Kind = ReviewEventKind.ChangesRequested, At = now, Actor = reviewerName, Note = note });
        foreach (var f in flags)
            c.Flags.Add(new FlaggedField { Field = f.Field, Question = f.Question, CurrentValue = f.CurrentValue });

        await contributions.SaveChangesAsync(ct);
        return c;
    }

    public async Task<Contribution> ResubmitAsync(Guid id, string userId, IReadOnlyList<FlagAnswerDto> answers, CancellationToken ct)
    {
        var c = await contributions.GetForUserAsync(id, userId, ct) ?? throw new KeyNotFoundException();
        Require(c, ContributionStatus.ChangesRequested, "resubmit");

        var now = clock.GetUtcNow();
        var open = c.Flags.Where(f => f.Answer is null).ToList();
        foreach (var flag in open)
        {
            var answer = answers.FirstOrDefault(a => a.FlagId == flag.Id)
                ?? throw new ArgumentException($"Flag {flag.Id} ({flag.Field}) has no answer.");
            flag.Answer = answer.Answer.Trim();
            flag.AnsweredAt = now;
        }

        c.Status = ContributionStatus.InReview;
        c.UpdatedAt = now;
        c.Events.Add(new ReviewEvent { Kind = ReviewEventKind.Resubmitted, At = now });
        await contributions.SaveChangesAsync(ct);
        return c;
    }

    public async Task<Contribution> PublishAsync(Guid id, string reviewerName, CancellationToken ct)
    {
        var c = await RequireAsync(id, ct);
        Require(c, ContributionStatus.InReview, "publish");

        var now = clock.GetUtcNow();
        var dishId = await UniqueSlugAsync(c.LocalName, ct);
        contributions.AddDish(BuildDish(c, dishId, now));

        c.Status = ContributionStatus.Published;
        c.PublishedDishId = dishId;
        c.UpdatedAt = now;
        c.Events.Add(new ReviewEvent { Kind = ReviewEventKind.Published, At = now, Actor = reviewerName });
        await contributions.SaveChangesAsync(ct);
        return c;
    }

    public async Task<Contribution> WithdrawAsync(Guid id, string userId, CancellationToken ct)
    {
        var c = await contributions.GetForUserAsync(id, userId, ct) ?? throw new KeyNotFoundException();
        if (c.Status is not (ContributionStatus.InReview or ContributionStatus.ChangesRequested))
            throw new InvalidContributionTransitionException(c.Status, "withdraw");

        var now = clock.GetUtcNow();
        c.Status = ContributionStatus.Withdrawn;
        c.UpdatedAt = now;
        c.Events.Add(new ReviewEvent { Kind = ReviewEventKind.Withdrawn, At = now });
        await contributions.SaveChangesAsync(ct);
        return c;
    }

    public async Task MarkReadAsync(Guid id, string reviewerName, CancellationToken ct)
    {
        var c = await RequireAsync(id, ct);
        if (c.Events.Any(e => e.Kind == ReviewEventKind.Read)) return;
        c.Events.Add(new ReviewEvent { Kind = ReviewEventKind.Read, At = clock.GetUtcNow(), Actor = reviewerName });
        await contributions.SaveChangesAsync(ct);
    }

    private async Task<Contribution> RequireAsync(Guid id, CancellationToken ct)
        => await contributions.GetAsync(id, ct) ?? throw new KeyNotFoundException();

    private static void Require(Contribution c, ContributionStatus expected, string action)
    {
        if (c.Status != expected) throw new InvalidContributionTransitionException(c.Status, action);
    }

    // ---- Publishing: the contribution becomes an archive dish ----

    private async Task<string> UniqueSlugAsync(string name, CancellationToken ct)
    {
        var slug = Slug(name);
        if (!await contributions.DishIdExistsAsync(slug, ct)) return slug;
        for (var n = 2; ; n++)
        {
            var candidate = $"{slug}-{n}";
            if (!await contributions.DishIdExistsAsync(candidate, ct)) return candidate;
        }
    }

    private static string Slug(string name)
    {
        var lower = name.Normalize(NormalizationForm.FormD).ToLowerInvariant();
        var ascii = new string(lower.Where(ch => ch is (>= 'a' and <= 'z') or (>= '0' and <= '9') or ' ' or '-').ToArray());
        var collapsed = SlugSeparators().Replace(ascii.Trim(), "-");
        return collapsed.Length > 0 ? collapsed[..Math.Min(collapsed.Length, 64)] : "dish";
    }

    [GeneratedRegex("[ -]+")]
    private static partial Regex SlugSeparators();

    private static Dish BuildDish(Contribution c, string dishId, DateTimeOffset now)
    {
        var credit = c.CreditTeacher && c.TaughtBy.Length > 0
            ? $"As taught by {c.TaughtBy}{(c.TaughtByOrigin.Length > 0 ? $" of {c.TaughtByOrigin}" : "")}."
            : "";

        return new Dish
        {
            Id = dishId,
            LocalName = c.LocalName,
            EnglishName = c.EnglishDescription,
            Region = c.Province,
            TimeLabel = "Community recipe",
            Difficulty = "As cooked at home",
            Description = c.CulturalSignificance.Length > 0 ? c.CulturalSignificance : c.Origin,
            SortOrder = int.MaxValue,           // after the editorial archive
            Provenance = Provenance.Community,
            ContributionId = c.Id,
            UpdatedAt = now,
            Recipe = new Recipe
            {
                DishId = dishId,
                Subtitle = c.EnglishDescription,
                IsVerified = true,              // it has been through review
                ContributorName = c.ContributorName,
                ContributorLocation = c.ContributorLocation,
                UpdatedAt = now,
                CulturalContext = new[] { c.Origin, c.CulturalSignificance, credit }
                    .Where(t => t.Length > 0)
                    .Select((t, n) => new RecipeParagraph { SortOrder = n, Text = t }).ToList(),
                Ingredients = c.Ingredients.Select(i => new RecipeIngredient
                {
                    SortOrder = i.SortOrder, IngredientKey = i.IngredientKey,
                    DisplayName = i.DisplayName, DisplaySubtitle = i.DisplaySubtitle, Quantity = i.Quantity,
                }).ToList(),
                Steps = c.Steps.Select(s => new CookingStep { Number = s.SortOrder + 1, Title = $"Step {s.SortOrder + 1}", Body = s.Text }).ToList(),
                Methods = c.TraditionalMethod.Length > 0
                    ? [new MethodNarrative { Kind = MethodKind.Traditional, Heading = "The traditional way",
                        Paragraphs = { new MethodParagraph { SortOrder = 0, Text = c.TraditionalMethod } } }]
                    : [],
            },
        };
    }
}
```

Register in `Program.cs`: `AddScoped<IContributionRepository, ContributionRepository>()`, `AddScoped<IContributionService, ContributionService>()`.

- [ ] **Step 5: Run the tests**

Run: `dotnet test TasteZambia.API.Tests --filter ContributionStateMachineTests`
Expected: PASS (11 incl. the theory rows). If `Slug` mangles "Chibwabwa na Mbalala", the expected id is `chibwabwa-na-mbalala` — check before touching the test.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(api): contribution state machine and publishing into the archive

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 3: Contributor endpoints

**Files:**
- Create: `TasteZambia.API/Controllers/MeContributionsController.cs`, `TasteZambia.API/Mapping/ContributionMappings.cs`
- Modify: `TasteZambia.Shared/Routes/ApiRoutes.cs`, `TasteZambia.API/Common/Http/ControllerResultExtensions.cs`, `TasteZambia.Shared/Contracts/Dishes/DishContracts.cs` + `Mapping/ArchiveMappings.cs` (`DishDto.Provenance`)
- Test: `TasteZambia.API.Tests/Controllers/ContributionEndpointTests.cs`

**Interfaces:**
- Produces:
  - Routes: `Me.Contributions = /api/v1/me/contributions`, `Me.ContributionById = …/{id}`, `Me.Resubmit = …/{id}/resubmit`, `Me.Withdraw = …/{id}/withdraw`
  - `POST me/contributions` → 201 `ContributionDetailDto` with `Location`
  - `GET me/contributions` → `IReadOnlyList<ContributionSummaryDto>` newest first
  - `GET me/contributions/{id}` → `ContributionDetailDto` or 404
  - `POST me/contributions/{id}/resubmit` → 200 detail; 400 if a flag is unanswered; 409 if not `ChangesRequested`
  - `POST me/contributions/{id}/withdraw` → 200 detail; 409 if not withdrawable
  - `ControllerResultExtensions.ConflictProblem(this ControllerBase, string detail)`
  - `DishDto` gains `Provenance Provenance` (last positional parameter; the mobile `DtoMappings` ignores it until Task 6)

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

[Collection(nameof(DatabaseCollection))]
public class ContributionEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        _client = await _factory.SignedInClientAsync();
    }
    public Task DisposeAsync() { _client.Dispose(); _factory.Dispose(); return Task.CompletedTask; }

    public static SubmitContributionRequest Chibwabwa() => new(
        "Chibwabwa na Mbalala", "Pumpkin leaves cooked with pounded groundnuts and nothing else", "Northern", "Relish", "",
        [new("chibwabwa", "Chibwabwa", "Pumpkin leaves", "2 bundles")],
        ["Shred the leaves fine and rinse them twice.", "Pound the groundnuts until the oil starts to show."],
        "Cooked in Mungwi.", "The relish cooked when there is no money for meat.", "Clay pot on charcoal.", "", "", true);

    [Fact]
    public async Task Submit_Returns201WithTheDetail()
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<ContributionDetailDto>();
        Assert.Equal(ContributionStatus.InReview, detail!.Status);
        Assert.Single(detail.Events, e => e.Kind == ReviewEventKind.Submitted);
        Assert.EndsWith($"/me/contributions/{detail.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Submit_WithoutSteps_Is400()
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa() with { Steps = [] });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_IsNewestFirstAndOnlyMine()
    {
        await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa() with { LocalName = "First" });
        await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa() with { LocalName = "Second" });
        using var other = await _factory.SignedInClientAsync();
        await other.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa() with { LocalName = "Not mine" });

        var mine = await _client.GetFromJsonAsync<List<ContributionSummaryDto>>(ApiRoutes.Me.Contributions);
        Assert.Equal(["Second", "First"], mine!.Select(c => c.LocalName));
    }

    [Fact]
    public async Task SomeoneElsesContribution_Is404NotForbidden()
    {
        var created = await (await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa())).Content.ReadFromJsonAsync<ContributionDetailDto>();
        using var other = await _factory.SignedInClientAsync();

        var response = await other.GetAsync(ApiRoutes.Me.ContributionById.Replace("{id}", created!.Id.ToString()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_ThenWithdrawAgain_Is409ProblemDetails()
    {
        var created = await (await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa())).Content.ReadFromJsonAsync<ContributionDetailDto>();
        var route = ApiRoutes.Me.Withdraw.Replace("{id}", created!.Id.ToString());

        var first = await _client.PostAsync(route, null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(ContributionStatus.Withdrawn, (await first.Content.ReadFromJsonAsync<ContributionDetailDto>())!.Status);

        var second = await _client.PostAsync(route, null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Resubmit_WhenNothingWasRequested_Is409()
    {
        var created = await (await _client.PostAsJsonAsync(ApiRoutes.Me.Contributions, Chibwabwa())).Content.ReadFromJsonAsync<ContributionDetailDto>();
        var response = await _client.PostAsJsonAsync(ApiRoutes.Me.Resubmit.Replace("{id}", created!.Id.ToString()), new ResubmitRequest([]));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
```

Add to `ApiFactory` a helper every controller test can use:

```csharp
/// <summary>A client signed in as a brand-new device account.</summary>
public async Task<HttpClient> SignedInClientAsync(string? role = null)
{
    var client = CreateClient();
    var deviceId = $"device-{Guid.NewGuid():N}";
    var tokens = await (await client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest(deviceId, new string('s', 40))))
        .Content.ReadFromJsonAsync<AuthTokensDto>();

    if (role is not null)
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ArchiveUser>>();
        await users.AddToRoleAsync((await users.FindByNameAsync(deviceId))!, role);
        // Roles are read from the token, so sign in again to get one that carries it.
        tokens = await (await client.PostAsJsonAsync(ApiRoutes.Auth.Device, new DeviceAuthRequest(deviceId, new string('s', 40))))
            .Content.ReadFromJsonAsync<AuthTokensDto>();
    }

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
    return client;
}
```

**This requires roles in the JWT.** In `TokenService.IssueAsync`, add a `ClaimTypes.Role` claim per role — inject `UserManager<ArchiveUser>` and call `GetRolesAsync(user)`. Update `TokenServiceTests.SutAsync` to build a `UserManager` from the context (or, simpler, change `ITokenService.IssueAsync` to take `IReadOnlyList<string> roles` and have `AuthController` pass `await users.GetRolesAsync(user)`; the token tests then pass `[]`). **Do the second** — it keeps `TokenService` free of Identity.

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test TasteZambia.API.Tests --filter ContributionEndpointTests`
Expected: FAIL.

- [ ] **Step 3: Routes, result helper, mappings**

`ApiRoutes.Me` — add:

```csharp
        public const string Contributions = $"{Root}/me/contributions";
        public const string ContributionById = $"{Contributions}/{{id}}";
        public const string Resubmit = $"{Contributions}/{{id}}/resubmit";
        public const string Withdraw = $"{Contributions}/{{id}}/withdraw";
```

and new groups:

```csharp
    /// <summary>Reviewer-only. Needs the "reviewer" role.</summary>
    public static class Review
    {
        public const string Queue = $"{Root}/review/queue";
        public const string RequestChanges = $"{Root}/review/{{id}}/request-changes";
        public const string Publish = $"{Root}/review/{{id}}/publish";
    }

    /// <summary>Admin-only. Needs the "admin" role.</summary>
    public static class Admin
    {
        public const string UserRoles = $"{Root}/admin/users/{{userName}}/roles";
    }
```

`ControllerResultExtensions` — add:

```csharp
    public static IActionResult ConflictProblem(this ControllerBase c, string detail)
        => c.Problem(title: "Not allowed in the current state", detail: detail, statusCode: StatusCodes.Status409Conflict);
```

`Mapping/ContributionMappings.cs`:

```csharp
using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Contracts.Review;

namespace TasteZambia.API.Mapping;

public static class ContributionMappings
{
    public static ContributionSummaryDto ToSummary(this Contribution c)
        => new(c.Id, c.LocalName, c.Province, c.Status, c.SubmittedAt, c.UpdatedAt, c.PublishedDishId);

    public static ContributionDetailDto ToDetail(this Contribution c)
        => new(c.Id, c.LocalName, c.EnglishDescription, c.Province, c.Status, c.SubmittedAt, c.PublishedDishId,
            c.ContributorName, c.ContributorLocation, c.TaughtBy, c.TaughtByOrigin, c.CreditTeacher,
            c.Events.OrderBy(e => e.At).Select(e => new ReviewEventDto(e.Kind, e.At, e.Actor, e.Note)).ToList(),
            c.Flags.OrderBy(f => f.Id).Select(f => new FlaggedFieldDto(f.Id, f.Field, f.Question, f.CurrentValue, f.Answer)).ToList());

    public static ReviewQueueItemDto ToQueueItem(this Contribution c)
        => new(c.Id, c.LocalName, c.Province, c.ContributorName, c.Status, c.SubmittedAt, c.Flags.Count(f => f.Answer is null));
}
```

`DishDto`: append `Provenance Provenance` as the last parameter; in `ArchiveMappings.ToDto(Dish)` pass `d.Provenance`. Mobile `DtoMappings` constructs `Dish` from the DTO by named properties and will not break; `SerializationTests` that build a `DishDto` positionally need the extra argument.

- [ ] **Step 4: The controller**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Auth;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

/// <summary>The signed-in user's own contributions. Everything here is scoped to that user; nothing leaks.</summary>
[ApiController]
[Authorize]
public sealed class MeContributionsController(
    ICurrentUser me,
    IContributionRepository contributions,
    IContributionService service) : ControllerBase
{
    private string UserId => me.UserId ?? throw new UnauthorizedAccessException();

    [HttpPost(ApiRoutes.Me.Contributions)]
    [ProducesResponseType<ContributionDetailDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Submit([FromBody] SubmitContributionRequest request, CancellationToken ct)
    {
        var c = await service.SubmitAsync(UserId, request, ct);
        return Created(ApiRoutes.Me.ContributionById.Replace("{id}", c.Id.ToString()), c.ToDetail());
    }

    [HttpGet(ApiRoutes.Me.Contributions)]
    [ProducesResponseType<IReadOnlyList<ContributionSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok((await contributions.ListForUserAsync(UserId, ct)).Select(c => c.ToSummary()).ToList());

    [HttpGet(ApiRoutes.Me.ContributionById)]
    [ProducesResponseType<ContributionDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var c = await contributions.GetForUserAsync(id, UserId, ct);
        return c is null ? this.NotFoundProblem($"No contribution {id}.") : Ok(c.ToDetail());
    }

    [HttpPost(ApiRoutes.Me.Resubmit)]
    [ProducesResponseType<ContributionDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Resubmit(Guid id, [FromBody] ResubmitRequest request, CancellationToken ct)
        => Transition(() => service.ResubmitAsync(id, UserId, request.Answers, ct));

    [HttpPost(ApiRoutes.Me.Withdraw)]
    [ProducesResponseType<ContributionDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Withdraw(Guid id, CancellationToken ct)
        => Transition(() => service.WithdrawAsync(id, UserId, ct));

    private async Task<IActionResult> Transition(Func<Task<Data.Entities.Contribution>> action)
    {
        try { return Ok((await action()).ToDetail()); }
        catch (KeyNotFoundException) { return this.NotFoundProblem("No such contribution."); }
        catch (InvalidContributionTransitionException ex) { return this.ConflictProblem(ex.Message); }
        catch (ArgumentException ex) { return Problem(title: "Incomplete", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest); }
    }
}
```

Check `NotFoundProblem`'s actual signature in `ControllerResultExtensions.cs` before use and match it.

- [ ] **Step 5: Run the whole API suite**

Run: `dotnet test TasteZambia.API.Tests`
Expected: PASS. `MeEndpointTests`/`AuthEndpointTests` can switch to `SignedInClientAsync()` now, but that is optional tidy-up, not scope.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(api): contributor endpoints - submit, list, detail, resubmit, withdraw

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 4: Reviewer and admin endpoints, Development reviewer

**Files:**
- Create: `TasteZambia.API/Controllers/ReviewController.cs`, `TasteZambia.API/Controllers/AdminController.cs`, `TasteZambia.API/Data/Seed/ReviewerSeeder.cs`
- Modify: `Program.cs`, `appsettings.Development.json`
- Test: `TasteZambia.API.Tests/Controllers/ReviewEndpointTests.cs`

**Interfaces:**
- Produces:
  - `GET review/queue` → `IReadOnlyList<ReviewQueueItemDto>`: everything `InReview`, oldest first (also marks nothing — reading the queue is not "reading" a submission)
  - `POST review/{id}/request-changes` body `RequestChangesRequest` → 200 `ContributionDetailDto`; 404; 409
  - `POST review/{id}/publish` → 200 `PublishResultDto`; 409
  - Both mutating review routes call `MarkReadAsync` first, so the timeline gets its "Read by the archive team" line the first time a reviewer acts.
  - Reviewer display name = the reviewer's profile `DisplayName`, else `"The archive team"`.
  - `PUT admin/users/{userName}/roles` body `string[]` → 204; `[Authorize(Roles = "admin")]`; replaces the user's role set (only names from `RoleSeeder.Roles`).
  - Development seeds one reviewer from `Reviewer:DeviceId` / `Reviewer:DeviceSecret` / `Reviewer:DisplayName` in `appsettings.Development.json` (`device-dev-reviewer-000000`, a 40‑char secret, `"Namakau Sitali"`) with roles `reviewer` + `admin`. Skipped when the section is absent (Testing, Production).

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Contracts.Dishes;
using TasteZambia.Shared.Contracts.Review;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

[Collection(nameof(DatabaseCollection))]
public class ReviewEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _contributor = null!;
    private HttpClient _reviewer = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        _contributor = await _factory.SignedInClientAsync();
        _reviewer = await _factory.SignedInClientAsync(role: "reviewer");
    }
    public Task DisposeAsync() { _contributor.Dispose(); _reviewer.Dispose(); _factory.Dispose(); return Task.CompletedTask; }

    private async Task<ContributionDetailDto> SubmitAsync(string name = "Chibwabwa na Mbalala")
        => (await (await _contributor.PostAsJsonAsync(ApiRoutes.Me.Contributions, ContributionEndpointTests.Chibwabwa() with { LocalName = name }))
            .Content.ReadFromJsonAsync<ContributionDetailDto>())!;

    [Fact]
    public async Task AContributorCannotSeeTheQueue()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await _contributor.GetAsync(ApiRoutes.Review.Queue)).StatusCode);
    }

    [Fact]
    public async Task Queue_ListsInReviewOldestFirst()
    {
        var a = await SubmitAsync("A");
        var b = await SubmitAsync("B");

        var queue = await _reviewer.GetFromJsonAsync<List<ReviewQueueItemDto>>(ApiRoutes.Review.Queue);
        var ids = queue!.Select(q => q.Id).ToList();
        Assert.True(ids.IndexOf(a.Id) < ids.IndexOf(b.Id));
    }

    [Fact]
    public async Task RequestChanges_FlagsFieldsAndTheContributorSeesThem()
    {
        var c = await SubmitAsync();
        var response = await _reviewer.PostAsJsonAsync(ApiRoutes.Review.RequestChanges.Replace("{id}", c.Id.ToString()),
            new RequestChangesRequest("Two things I want to get right before it goes public.",
                [new("Local name", "Is this the Mungwi name?", "Chibwabwa na Mbalala")]));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var seen = await _contributor.GetFromJsonAsync<ContributionDetailDto>(ApiRoutes.Me.ContributionById.Replace("{id}", c.Id.ToString()));
        Assert.Equal(ContributionStatus.ChangesRequested, seen!.Status);
        Assert.Single(seen.Flags);
        Assert.Contains(seen.Events, e => e.Kind == ReviewEventKind.Read);            // first reviewer action marks it read
        Assert.Contains(seen.Events, e => e.Kind == ReviewEventKind.ChangesRequested && e.Note!.StartsWith("Two things"));
    }

    [Fact]
    public async Task Publish_PutsTheDishInTheArchive()
    {
        var c = await SubmitAsync("Chibwabwa na Mbalala");
        var response = await _reviewer.PostAsync(ApiRoutes.Review.Publish.Replace("{id}", c.Id.ToString()), null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PublishResultDto>();

        var dishes = await _contributor.GetFromJsonAsync<List<DishDto>>(ApiRoutes.Dishes.Collection);
        var published = Assert.Single(dishes!, d => d.Id == result!.DishId);
        Assert.Equal(Provenance.Community, published.Provenance);

        var recipe = await _contributor.GetAsync(ApiRoutes.Dishes.Recipe.Replace("{id}", result!.DishId));
        Assert.Equal(HttpStatusCode.OK, recipe.StatusCode);
    }

    [Fact]
    public async Task Publish_ChangesTheDishesETag()
    {
        var before = (await _contributor.GetAsync(ApiRoutes.Dishes.Collection)).Headers.ETag!.Tag;
        var c = await SubmitAsync();
        await _reviewer.PostAsync(ApiRoutes.Review.Publish.Replace("{id}", c.Id.ToString()), null);
        var after = (await _contributor.GetAsync(ApiRoutes.Dishes.Collection)).Headers.ETag!.Tag;
        Assert.NotEqual(before, after);
    }

    [Fact]
    public async Task Publish_OnAWithdrawnContribution_Is409()
    {
        var c = await SubmitAsync();
        await _contributor.PostAsync(ApiRoutes.Me.Withdraw.Replace("{id}", c.Id.ToString()), null);
        var response = await _reviewer.PostAsync(ApiRoutes.Review.Publish.Replace("{id}", c.Id.ToString()), null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanGrantReviewer_AndAContributorCannot()
    {
        using var admin = await _factory.SignedInClientAsync(role: "admin");
        using var newbie = await _factory.SignedInClientAsync();
        var newbieName = (await newbie.GetFromJsonAsync<ContributionDetailDto>(ApiRoutes.Me.Contributions)) is null ? "" : "";   // placeholder removed below
    }
}
```

Replace the last test's body with something that can actually see the user name — the simplest is to make `SignedInClientAsync` return `(HttpClient client, string deviceId)`. Change the helper's signature to that and update its three callers in this task and Task 3:

```csharp
    [Fact]
    public async Task Admin_CanGrantReviewer_AndAContributorCannot()
    {
        var (admin, _) = await _factory.SignedInClientAsync(role: "admin");
        var (newbie, newbieName) = await _factory.SignedInClientAsync();
        var route = ApiRoutes.Admin.UserRoles.Replace("{userName}", newbieName);

        Assert.Equal(HttpStatusCode.Forbidden, (await newbie.PutAsJsonAsync(route, new[] { "reviewer" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync(route, new[] { "reviewer" })).StatusCode);

        // A fresh token carries the new role.
        var (promoted, _) = await _factory.SignedInClientAsync(existingDeviceId: newbieName);
        Assert.Equal(HttpStatusCode.OK, (await promoted.GetAsync(ApiRoutes.Review.Queue)).StatusCode);
        admin.Dispose(); newbie.Dispose(); promoted.Dispose();
    }
```

So `SignedInClientAsync(string? role = null, string? existingDeviceId = null)` returns `(HttpClient, string deviceId)`; secret is always `new string('s', 40)`.

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test TasteZambia.API.Tests --filter ReviewEndpointTests`
Expected: FAIL.

- [ ] **Step 3: Controllers**

`Controllers/ReviewController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Auth;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Contributions;
using TasteZambia.Shared.Contracts.Review;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

/// <summary>The archive team's side of review. Every route needs the reviewer role.</summary>
[ApiController]
[Authorize(Roles = "reviewer")]
public sealed class ReviewController(
    ICurrentUser me,
    IContributionRepository contributions,
    IContributionService service,
    IUserProfileRepository profiles) : ControllerBase
{
    private const string DefaultReviewer = "The archive team";

    [HttpGet(ApiRoutes.Review.Queue)]
    [ProducesResponseType<IReadOnlyList<ReviewQueueItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Queue(CancellationToken ct)
        => Ok((await contributions.ListByStatusAsync(ContributionStatus.InReview, ct)).Select(c => c.ToQueueItem()).ToList());

    [HttpPost(ApiRoutes.Review.RequestChanges)]
    [ProducesResponseType<ContributionDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestChanges(Guid id, [FromBody] RequestChangesRequest request, CancellationToken ct)
    {
        var reviewer = await ReviewerNameAsync(ct);
        try
        {
            await service.MarkReadAsync(id, reviewer, ct);
            return Ok((await service.RequestChangesAsync(id, reviewer, request.Note, request.Flags, ct)).ToDetail());
        }
        catch (KeyNotFoundException) { return this.NotFoundProblem($"No contribution {id}."); }
        catch (InvalidContributionTransitionException ex) { return this.ConflictProblem(ex.Message); }
    }

    [HttpPost(ApiRoutes.Review.Publish)]
    [ProducesResponseType<PublishResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var reviewer = await ReviewerNameAsync(ct);
        try
        {
            await service.MarkReadAsync(id, reviewer, ct);
            var c = await service.PublishAsync(id, reviewer, ct);
            return Ok(new PublishResultDto(c.Id, c.PublishedDishId!));
        }
        catch (KeyNotFoundException) { return this.NotFoundProblem($"No contribution {id}."); }
        catch (InvalidContributionTransitionException ex) { return this.ConflictProblem(ex.Message); }
    }

    private async Task<string> ReviewerNameAsync(CancellationToken ct)
    {
        var p = await profiles.GetOrCreateProfileAsync(me.UserId!, ct);
        return p.DisplayName.Length > 0 ? p.DisplayName : DefaultReviewer;
    }
}
```

`Controllers/AdminController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Data.Seed;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
public sealed class AdminController(UserManager<ArchiveUser> users) : ControllerBase
{
    /// <summary>Replaces a user's role set. Unknown role names are rejected.</summary>
    [HttpPut(ApiRoutes.Admin.UserRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetRoles(string userName, [FromBody] string[] roles)
    {
        var unknown = roles.Except(RoleSeeder.Roles).ToList();
        if (unknown.Count > 0)
            return Problem(title: "Unknown role", detail: string.Join(", ", unknown), statusCode: StatusCodes.Status400BadRequest);

        var user = await users.FindByNameAsync(userName);
        if (user is null) return this.NotFoundProblem($"No user {userName}.");

        var current = await users.GetRolesAsync(user);
        await users.RemoveFromRolesAsync(user, current.Except(roles));
        await users.AddToRolesAsync(user, roles.Except(current));
        return NoContent();
    }
}
```

`Data/Seed/ReviewerSeeder.cs`:

```csharp
using Microsoft.AspNetCore.Identity;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Seed;

/// <summary>
/// Development only: one reviewer+admin device account from configuration, so the whole
/// review loop can be driven from Scalar on a laptop. No section, no account.
/// </summary>
public static class ReviewerSeeder
{
    public static async Task SeedAsync(IConfiguration config, UserManager<ArchiveUser> users, TasteZambiaDbContext db)
    {
        var section = config.GetSection("Reviewer");
        var id = section["DeviceId"]; var secret = section["DeviceSecret"]; var name = section["DisplayName"];
        if (id is null || secret is null) return;

        var user = await users.FindByNameAsync(id);
        if (user is null)
        {
            user = new ArchiveUser { UserName = id };
            var created = await users.CreateAsync(user, secret);
            if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        foreach (var role in new[] { "reviewer", "admin" })
            if (!await users.IsInRoleAsync(user, role)) await users.AddToRoleAsync(user, role);

        if (name is not null && await db.UserProfiles.FindAsync(user.Id) is null)
        {
            db.UserProfiles.Add(new UserProfile { UserId = user.Id, DisplayName = name, Location = "Kasama, Northern", UpdatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
    }
}
```

`Program.cs` Development block, after `RoleSeeder`: `await ReviewerSeeder.SeedAsync(app.Configuration, scope.ServiceProvider.GetRequiredService<UserManager<ArchiveUser>>(), db);`

`appsettings.Development.json`:

```json
"Reviewer": {
  "DeviceId": "device-dev-reviewer-000000",
  "DeviceSecret": "dev-reviewer-secret-not-for-production-0000",
  "DisplayName": "Namakau Sitali"
}
```

- [ ] **Step 4: Run the whole API suite; verify from Scalar**

Run: `dotnet test TasteZambia.API.Tests` → PASS.

Then with the API running: `POST /auth/device` with the dev reviewer's id/secret in Scalar, authorise with the token, `GET /review/queue` → `[]` (or the phone's submissions once Task 5 lands).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(api): reviewer queue, request-changes, publish; admin roles; dev reviewer

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 5: Mobile — local drafts and real submission

**Files:**
- Modify: `TasteZambia.Core/Models/ContributionDraft.cs`, `TasteZambia.Core/Services/ContributionService.cs`, `TasteZambia.Core/ViewModels/ShareViewModel.cs`, `TasteZambia.Core/Data/Http/HttpProfileRepository.cs`, `TasteZambia.Mobile/MauiProgram.cs`
- Create: `TasteZambia.Core/Services/DraftStore.cs`
- Test: `TasteZambia.Core.Tests/Services/DraftStoreTests.cs`; update `ShareLifecycleTests.cs`, `TestServices.cs`, and any test constructing `ContributionService`

**Interfaces:**
- Produces:
  - `LocalDraft { Guid Id; DateTimeOffset EditedAt; ContributionDraft Draft; }`
  - `DraftStore(ILocalStore local, TimeProvider clock)`: `IReadOnlyList<LocalDraft> All` (newest edit first), `LocalDraft Create(ContributionDraft)`, `void Save(LocalDraft)` (bumps `EditedAt`), `void Remove(Guid)`, `event EventHandler? Changed`
  - `RecipeDraft.From(LocalDraft, DateTimeOffset now)` → the design's card: `PercentComplete` from 9 checks (`LocalName`, `EnglishDescription`, `Province`, `MealType`, `Ingredients.Count > 0`, `Steps.Count >= 2`, `Origin`, `CulturalSignificance`, `TraditionalMethod`), `Missing` = `"Needs {first missing label}"` or `"Ready to submit"`, `When` = `"Edited {relative}"`, `TintHex` by completeness (≥ 80 → `#2F6A4D`, ≥ 40 → `#C07F1E`, else `#A3452A`)
  - `IContributionService` (Core) becomes:
    ```csharp
    ContributionDraft StartShareDraft();           // unchanged: the design's pre-filled walkthrough
    ContributionDraft StartFamilyDraft();          // unchanged
    IReadOnlyList<ReviewStage> ReviewPipeline { get; }   // unchanged, seeded copy
    IReadOnlyList<RecipeDraft> Drafts { get; }     // now from DraftStore
    LocalDraft SaveDraft(ContributionDraft draft, Guid? id = null);
    Task<Contribution> SubmitAsync(ContributionDraft draft, CancellationToken ct = default);   // POST, then removes the local draft
    Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default);
    Task<ContributionDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default);
    Task<ContributionDetailDto> ResubmitAsync(Guid id, IReadOnlyList<FlagAnswerDto> answers, CancellationToken ct = default);
    Task<ContributionDetailDto> WithdrawAsync(Guid id, CancellationToken ct = default);
    IReadOnlyList<ReviewStep> TimelineFor(ContributionDetailDto detail);    // pure mapping, tested
    IReadOnlyList<FlaggedField> FlagsFor(ContributionDetailDto detail);
    ```
    `Timeline`/`FlaggedFields` properties are **removed** (they were seeded).
  - `Contribution` (Core model) gains `Guid Id` and `string? PublishedDishId`: `record Contribution(Guid Id, string Name, ContributionStatus Status, string Meta, string? PublishedDishId = null)`. Seeded `SeedData.Contributions` get `Guid.Empty` ids.
  - `HttpProfileRepository.GetContributionsAsync` → `GET /me/contributions` mapped to `Contribution` with `Meta` = `"{Province} Province · {status phrase} {date}"` (`submitted 2 Sep 2026` / `changes requested 3 Sep 2026` / `published 12 Sep 2026` / `withdrawn 5 Sep 2026`).
  - `ShareViewModel.Next()` calls `contributions.SaveDraft(Draft, _draftId)` on every step change and `SubmitAsync` at step 4. `ShareViewModel` accepts an optional `DraftId` route parameter to continue a draft; `DraftsViewModel.Continue` navigates with it.

- [ ] **Step 1: Write the failing tests**

`DraftStoreTests.cs`:

```csharp
using Microsoft.Extensions.Time.Testing;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public class DraftStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    private static (DraftStore store, FakeTimeProvider clock) Sut(ILocalStore? local = null)
    {
        var clock = new FakeTimeProvider(Now);
        return (new DraftStore(local ?? new InMemoryLocalStore(), clock), clock);
    }

    [Fact]
    public void Create_ThenAll_IsNewestEditFirstAndSurvivesAReload()
    {
        var local = new InMemoryLocalStore();
        var (store, clock) = Sut(local);
        var first = store.Create(new ContributionDraft { LocalName = "Munkoyo" });
        clock.Advance(TimeSpan.FromHours(1));
        var second = store.Create(new ContributionDraft { LocalName = "Ubwali bwa Tute" });

        var (reloaded, _) = Sut(local);
        Assert.Equal([second.Id, first.Id], reloaded.All.Select(d => d.Id));
        Assert.Equal("Munkoyo", reloaded.All[1].Draft.LocalName);
    }

    [Fact]
    public void Save_BumpsEditedAtAndRaisesChanged()
    {
        var (store, clock) = Sut();
        var d = store.Create(new ContributionDraft { LocalName = "Munkoyo" });
        var raised = false;
        store.Changed += (_, _) => raised = true;

        clock.Advance(TimeSpan.FromMinutes(5));
        d.Draft.Province = "Northern";
        store.Save(d);

        Assert.True(raised);
        Assert.Equal(Now.AddMinutes(5), store.All.Single().EditedAt);
        Assert.Equal("Northern", store.All.Single().Draft.Province);
    }

    [Fact]
    public void Remove_DropsIt()
    {
        var (store, _) = Sut();
        var d = store.Create(new ContributionDraft());
        store.Remove(d.Id);
        Assert.Empty(store.All);
    }

    [Fact]
    public void RecipeDraftCard_ReflectsCompleteness()
    {
        var now = Now;
        var empty = new LocalDraft { Id = Guid.NewGuid(), EditedAt = now.AddDays(-21), Draft = new() { LocalName = "Ubwali bwa Tute", Province = "Luapula" } };
        var card = RecipeDraft.From(empty, now);
        Assert.Equal(22, card.PercentComplete);                 // 2 of 9
        Assert.Equal("Needs a short English description", card.Missing);
        Assert.Equal("Edited 3 weeks ago", card.When);
        Assert.Equal("#A3452A", card.TintHex);

        var full = new LocalDraft { Id = Guid.NewGuid(), EditedAt = now.AddHours(-2), Draft = new ContributionService(new DraftStore(new InMemoryLocalStore(), TimeProvider.System), TestServices.NoNetwork()).StartShareDraft() };
        var fullCard = RecipeDraft.From(full, now);
        Assert.Equal(100, fullCard.PercentComplete);
        Assert.Equal("Ready to submit", fullCard.Missing);
        Assert.Equal("Edited 2 hours ago", fullCard.When);
        Assert.Equal("#2F6A4D", fullCard.TintHex);
    }
}
```

Relative-time rules for `When`: `< 1 min` → "just now"; `< 60 min` → "{n} minutes ago" (1 → "1 minute ago"); `< 24 h` → "{n} hours ago"; `< 7 d` → "{n} days ago"; `< 30 d` → "{n} weeks ago"; else "{n} months ago". Missing-field labels in check order: `"a local name"`, `"a short English description"`, `"a province"`, `"a meal type"`, `"at least one ingredient"`, `"one more cooking step"`, `"where it is cooked"`, `"what it means"`, `"the traditional method"`.

Timeline mapping test — add to `ShareLifecycleTests.cs`:

```csharp
[Fact]
public void TimelineFor_MapsEventsOntoTheFourDesignStages()
{
    var svc = TestServices.Contributions();
    var t = new DateTimeOffset(2026, 9, 2, 9, 0, 0, TimeSpan.Zero);
    var detail = Detail(ContributionStatus.InReview,
    [
        new(ReviewEventKind.Submitted, t, null, null),
        new(ReviewEventKind.Read, t.AddDays(1), "Namakau Sitali", null),
    ]);

    var steps = svc.TimelineFor(detail);

    Assert.Equal(4, steps.Count);
    Assert.Equal(("Submitted", "2 Sep 2026", ReviewState.Done), (steps[0].Label, steps[0].When, steps[0].State));
    Assert.Equal(("Read by the archive team", "3 Sep 2026", ReviewState.Done), (steps[1].Label, steps[1].When, steps[1].State));
    Assert.Equal("Reviewed by Namakau Sitali.", steps[1].Note);
    Assert.Equal(("Checked against regional sources", "In progress", ReviewState.InProgress), (steps[2].Label, steps[2].When, steps[2].State));
    Assert.Equal(("Published and credited", "Pending", ReviewState.Pending), (steps[3].Label, steps[3].When, steps[3].State));
    Assert.False(steps[3].IsNotLast);
}

[Fact]
public void TimelineFor_PublishedContribution_IsAllDone()
{
    var svc = TestServices.Contributions();
    var t = new DateTimeOffset(2026, 9, 2, 9, 0, 0, TimeSpan.Zero);
    var detail = Detail(ContributionStatus.Published,
    [
        new(ReviewEventKind.Submitted, t, null, null),
        new(ReviewEventKind.Read, t.AddDays(1), "Namakau Sitali", null),
        new(ReviewEventKind.Published, t.AddDays(10), "Namakau Sitali", null),
    ]);
    Assert.All(svc.TimelineFor(detail), s => Assert.Equal(ReviewState.Done, s.State));
    Assert.Equal("12 Sep 2026", svc.TimelineFor(detail)[3].When);
}

private static ContributionDetailDto Detail(ContributionStatus status, IReadOnlyList<ReviewEventDto> events, IReadOnlyList<FlaggedFieldDto>? flags = null)
    => new(Guid.NewGuid(), "Chibwabwa na Mbalala", "Pumpkin leaves", "Northern", status, events[0].At, null,
        "Chanda Mwaba", "Kitwe", "", "", true, events, flags ?? []);
```

Stage semantics for `TimelineFor`: stage 1 Done at `Submitted.At`; stage 2 Done at `Read.At` if a Read event exists, else InProgress/"In progress"; stage 3 ("Checked against regional sources") Done when status is `Published` (at `Published.At`), InProgress when a Read exists and status is `InReview`/`ChangesRequested`, else Pending; stage 4 Done at `Published.At` when Published, else Pending/"Pending". For `Withdrawn`, stages after the last Done are Pending with When `"Withdrawn"`. Notes are the seeded strings from `SeedData.SubmissionTimeline` except stage 2, whose note is `$"Reviewed by {actor}, {scope}."` → without a scope on the server, use `$"Reviewed by {actor}."`. Date format `d MMM yyyy`, invariant culture.

`TestServices.Contributions()` → `new ContributionService(new DraftStore(new InMemoryLocalStore(), TimeProvider.System), NoNetwork())`.

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test TasteZambia.Core.Tests --filter "DraftStoreTests|TimelineFor"`
Expected: FAIL (compile).

- [ ] **Step 3: Models and `DraftStore`**

Append to `ContributionDraft.cs`:

```csharp
/// <summary>A draft on this phone. Drafts never leave the device until they are submitted.</summary>
public sealed class LocalDraft
{
    public Guid Id { get; set; }
    public DateTimeOffset EditedAt { get; set; }
    public ContributionDraft Draft { get; set; } = new();
}
```

Replace the `RecipeDraft` record with:

```csharp
public sealed record RecipeDraft(string Name, int PercentComplete, string Missing, string When, string TintHex)
{
    public Guid Id { get; init; }
    public string PercentLabel => $"{PercentComplete}%";
    public double Fraction => PercentComplete / 100.0;

    private static readonly (Func<ContributionDraft, bool> Filled, string Label)[] Checks =
    [
        (d => d.LocalName.Length > 0, "a local name"),
        (d => d.EnglishDescription.Length > 0, "a short English description"),
        (d => d.Province.Length > 0, "a province"),
        (d => d.MealType.Length > 0, "a meal type"),
        (d => d.Ingredients.Count > 0, "at least one ingredient"),
        (d => d.Steps.Count >= 2, "one more cooking step"),
        (d => d.Origin.Length > 0, "where it is cooked"),
        (d => d.CulturalSignificance.Length > 0, "what it means"),
        (d => d.TraditionalMethod.Length > 0, "the traditional method"),
    ];

    public static RecipeDraft From(LocalDraft local, DateTimeOffset now)
    {
        var d = local.Draft;
        var filled = Checks.Count(c => c.Filled(d));
        var percent = (int)Math.Round(filled * 100.0 / Checks.Length);
        var missing = Checks.FirstOrDefault(c => !c.Filled(d)).Label;
        return new RecipeDraft(
            d.LocalName.Length > 0 ? d.LocalName : "Untitled recipe",
            percent,
            missing is null ? "Ready to submit" : $"Needs {missing}",
            $"Edited {Relative(now - local.EditedAt)}",
            percent >= 80 ? "#2F6A4D" : percent >= 40 ? "#C07F1E" : "#A3452A")
        { Id = local.Id };
    }

    public static string Relative(TimeSpan ago)
    {
        if (ago < TimeSpan.FromMinutes(1)) return "just now";
        if (ago < TimeSpan.FromHours(1)) return Plural((int)ago.TotalMinutes, "minute");
        if (ago < TimeSpan.FromDays(1)) return Plural((int)ago.TotalHours, "hour");
        if (ago < TimeSpan.FromDays(7)) return Plural((int)ago.TotalDays, "day");
        if (ago < TimeSpan.FromDays(30)) return Plural((int)(ago.TotalDays / 7), "week");
        return Plural((int)(ago.TotalDays / 30), "month");
    }

    private static string Plural(int n, string unit) => $"{n} {unit}{(n == 1 ? "" : "s")} ago";
}
```

`Services/DraftStore.cs`:

```csharp
using TasteZambia.Core.Models;

namespace TasteZambia.Core.Services;

/// <summary>The phone's drafts. JSON in the local store; nothing here talks to the network.</summary>
public sealed class DraftStore
{
    private const string Key = "drafts";
    private readonly ILocalStore _local;
    private readonly TimeProvider _clock;
    private readonly List<LocalDraft> _drafts;

    public event EventHandler? Changed;

    public DraftStore(ILocalStore local, TimeProvider clock)
    {
        _local = local;
        _clock = clock;
        _drafts = local.Get<List<LocalDraft>>(Key) ?? [];
    }

    public IReadOnlyList<LocalDraft> All => _drafts.OrderByDescending(d => d.EditedAt).ToList();

    public LocalDraft Create(ContributionDraft draft)
    {
        var local = new LocalDraft { Id = Guid.NewGuid(), EditedAt = _clock.GetUtcNow(), Draft = draft };
        _drafts.Add(local);
        Persist();
        return local;
    }

    public void Save(LocalDraft draft)
    {
        var index = _drafts.FindIndex(d => d.Id == draft.Id);
        draft.EditedAt = _clock.GetUtcNow();
        if (index < 0) _drafts.Add(draft); else _drafts[index] = draft;
        Persist();
    }

    public void Remove(Guid id)
    {
        if (_drafts.RemoveAll(d => d.Id == id) > 0) Persist();
    }

    private void Persist()
    {
        _local.Set(Key, _drafts);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
```

`ContributionDraft` has `List<RecipeIngredient>` where `RecipeIngredient` is a record with `required init` members and a computed `IsLinked` — `System.Text.Json` round-trips it (`IsLinked` has no setter and is skipped). Confirm with `DraftStoreTests`.

- [ ] **Step 4: Rewrite the Core `ContributionService`**

Keep `StartShareDraft()`/`StartFamilyDraft()`/`ReviewPipeline` byte-for-byte. Replace the rest:

```csharp
public sealed class ContributionService(DraftStore drafts, HttpClient api) : IContributionService
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public IReadOnlyList<ReviewStage> ReviewPipeline => SeedData.ReviewPipeline;

    public IReadOnlyList<RecipeDraft> Drafts
        => drafts.All.Select(d => RecipeDraft.From(d, DateTimeOffset.UtcNow)).ToList();

    public LocalDraft SaveDraft(ContributionDraft draft, Guid? id = null)
    {
        var existing = id is { } known ? drafts.All.FirstOrDefault(d => d.Id == known) : null;
        if (existing is null) return drafts.Create(draft);
        existing.Draft = draft;
        drafts.Save(existing);
        return existing;
    }

    public async Task<Contribution> SubmitAsync(ContributionDraft draft, CancellationToken ct = default)
    {
        var request = new SubmitContributionRequest(
            draft.LocalName, draft.EnglishDescription, draft.Province, draft.MealType, draft.Language,
            draft.Ingredients.Select(i => new ContributionIngredientDto(i.IngredientKey, i.DisplayName, i.DisplaySubtitle, i.Quantity)).ToList(),
            draft.Steps, draft.Origin, draft.CulturalSignificance, draft.TraditionalMethod,
            draft.TaughtBy, draft.TaughtByOrigin, draft.CreditTeacher);

        var response = await api.PostAsJsonAsync(ApiRoutes.Me.Contributions, request, ct);
        response.EnsureSuccessStatusCode();
        var detail = (await response.Content.ReadFromJsonAsync<ContributionDetailDto>(ct))!;

        foreach (var d in drafts.All.Where(d => d.Draft == draft || d.Draft.LocalName == draft.LocalName).ToList())
            drafts.Remove(d.Id);

        return ToModel(detail.Id, detail.LocalName, detail.Province, detail.Status, detail.SubmittedAt, detail.Events, detail.PublishedDishId);
    }

    public async Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default)
    {
        var list = await api.GetFromJsonAsync<List<ContributionSummaryDto>>(ApiRoutes.Me.Contributions, ct) ?? [];
        return list.Select(s => ToModel(s.Id, s.LocalName, s.Province, s.Status, s.SubmittedAt, [], s.PublishedDishId, s.UpdatedAt)).ToList();
    }

    public async Task<ContributionDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var response = await api.GetAsync(ApiRoutes.Me.ContributionById.Replace("{id}", id.ToString()), ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContributionDetailDto>(ct);
    }

    public async Task<ContributionDetailDto> ResubmitAsync(Guid id, IReadOnlyList<FlagAnswerDto> answers, CancellationToken ct = default)
    {
        var response = await api.PostAsJsonAsync(ApiRoutes.Me.Resubmit.Replace("{id}", id.ToString()), new ResubmitRequest(answers), ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContributionDetailDto>(ct))!;
    }

    public async Task<ContributionDetailDto> WithdrawAsync(Guid id, CancellationToken ct = default)
    {
        var response = await api.PostAsync(ApiRoutes.Me.Withdraw.Replace("{id}", id.ToString()), null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContributionDetailDto>(ct))!;
    }

    public IReadOnlyList<ReviewStep> TimelineFor(ContributionDetailDto d)
    {
        var submitted = d.Events.FirstOrDefault(e => e.Kind == ReviewEventKind.Submitted);
        var read = d.Events.FirstOrDefault(e => e.Kind == ReviewEventKind.Read);
        var published = d.Events.FirstOrDefault(e => e.Kind == ReviewEventKind.Published);
        var withdrawn = d.Status == ContributionStatus.Withdrawn;
        var seeded = SeedData.SubmissionTimeline;

        ReviewStep Stage(int i, ReviewEventDto? at, bool inProgress, string? note = null)
        {
            var state = at is not null ? ReviewState.Done : inProgress ? ReviewState.InProgress : ReviewState.Pending;
            var when = at is not null ? Date(at.At) : withdrawn ? "Withdrawn" : inProgress ? "In progress" : "Pending";
            return new ReviewStep(seeded[i].Label, when, note ?? seeded[i].Note, state, i < 3);
        }

        return
        [
            Stage(0, submitted, false),
            Stage(1, read, !withdrawn && read is null, read is null ? null : $"Reviewed by {read.Actor}."),
            Stage(2, published, !withdrawn && read is not null && published is null),
            Stage(3, published, false),
        ];
    }

    public IReadOnlyList<FlaggedField> FlagsFor(ContributionDetailDto d)
        => d.Flags.Where(f => f.Answer is null).Select(f => new FlaggedField(f.Field, f.Question, f.CurrentValue) { Id = f.Id }).ToList();

    private static Contribution ToModel(Guid id, string name, string province, ContributionStatus status, DateTimeOffset submitted,
        IReadOnlyList<ReviewEventDto> events, string? dishId, DateTimeOffset? updated = null)
    {
        var (phrase, at) = status switch
        {
            ContributionStatus.Published => ("published", updated ?? events.LastOrDefault(e => e.Kind == ReviewEventKind.Published)?.At ?? submitted),
            ContributionStatus.ChangesRequested => ("changes requested", updated ?? submitted),
            ContributionStatus.Withdrawn => ("withdrawn", updated ?? submitted),
            _ => ("submitted", submitted),
        };
        return new Contribution(id, name, status, $"{province} Province · {phrase} {Date(at)}", dishId);
    }

    private static string Date(DateTimeOffset at) => at.ToString("d MMM yyyy", Invariant);
}
```

`FlaggedField` (Core model) gains `public int Id { get; init; }` and a settable `Answer` for the Changes screen: `public sealed record FlaggedField(string Field, string Question, string CurrentValue) { public int Id { get; init; } public string Answer { get; set; } = ""; }`.

`Contribution` model: `public sealed record Contribution(Guid Id, string Name, ContributionStatus Status, string Meta, string? PublishedDishId = null);` — update `SeedData.Contributions` to pass `Guid.Empty`.

`HttpProfileRepository.GetContributionsAsync` — inject `IContributionService contributions` and return `contributions.GetContributionsAsync(ct)`.

- [ ] **Step 5: Wire the wizard and registrations**

`ShareViewModel`: add `[ObservableProperty] private Guid? _draftId;` (route parameter `draftId`); in `InitializeAsync`, if `DraftId` is set load that draft's `Draft` from `contributions` (expose `LocalDraft? FindDraft(Guid)` on the service — add it to the interface), else `StartShareDraft()`. In `Next()`: `DraftId = contributions.SaveDraft(Draft, DraftId).Id;` before `Step++`; at step 4 wrap `SubmitAsync` in try/catch — on `HttpRequestException` set `SubmittedMeta = "Could not reach the archive. Your draft is saved; try again when you are online."` and leave `IsSubmitted` false. `DraftsViewModel.Continue(RecipeDraft)` → `Navigation.GoToAsync("share", new() { ["draftId"] = draft.Id })` (check `INavigationService.GoToAsync`'s parameter shape in `INavigationService.cs` and match it).

`MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<DraftStore>(sp => new DraftStore(sp.GetRequiredService<ILocalStore>(), TimeProvider.System));
builder.Services.AddSingleton<IContributionService>(sp => new ContributionService(
    sp.GetRequiredService<DraftStore>(),
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("me")));
```

`HttpProfileRepository` now also takes `IContributionService` — typed-client DI resolves it.

- [ ] **Step 6: Run the Core tests, fix the fallout, build Android**

Run: `dotnet test TasteZambia.Core.Tests` — expect compile errors in tests that used `new ContributionService()`, `contributions.Timeline`, `contributions.FlaggedFields`; replace with `TestServices.Contributions()` and the new mapping methods. Then `dotnet build TasteZambia.Mobile -f net10.0-android --no-incremental` — no warnings.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat(mobile): local drafts, real submission, contributions from the account

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 6: Mobile — the lifecycle screens on a real contribution

**Files:**
- Modify: `TasteZambia.Core/ViewModels/ShareLifecycleViewModels.cs`, `TasteZambia.Core/ViewModels/ProfileViewModel.cs`, `TasteZambia.Core/Data/Http/DtoMappings.cs` (+ `Dish.Provenance` if the model wants it — optional), `TasteZambia.Mobile/Views/Share/ShareChangesView.xaml` (answer `Editor` per flag), `TasteZambia.Mobile/Views/MainShellPage.xaml.cs` (routes take `id`)
- Test: `TasteZambia.Core.Tests/ViewModels/ShareLifecycleTests.cs`

**Interfaces:**
- `ShareReviewViewModel`: route param `id` (Guid). `InitializeAsync` → `GetDetailAsync` → `DishName`, `SubmittedMeta` (`"{Province} Province · submitted {d MMM yyyy}"`), `ReviewerName` (actor of the `Read` event, else `"The archive team"`), `ReviewerScope` (`"{Province} Province records"`), `Timeline` from `TimelineFor`. `Withdraw` → `WithdrawAsync` then `GoBackAsync`. `CanWithdraw` = status ∈ {InReview, ChangesRequested}. When status is `ChangesRequested`, `HasChangesRequested = true` and a `SeeQuestions` command navigates to `shareChanges` with the same `id`.
- `ShareChangesViewModel`: route param `id`. `Flagged` from `FlagsFor` (each with a bindable `Answer`), `ReviewerName`/`ReviewerNote` from the last `ChangesRequested` event. `Resubmit` → `ResubmitAsync(id, answers)`; disabled (`CanResubmit`) until every answer is non-empty; on success navigates to `shareReview` for the same id.
- `SharePublishedViewModel`: route param `id`. `DishName`, `Subtitle` (`EnglishDescription`), `Credit` = `"Recorded by {ContributorName}, {ContributorLocation}. As taught by {TaughtBy} of {TaughtByOrigin}. Verified against provincial records, {MMMM yyyy}."` (omit the taught-by sentence when `TaughtBy` is empty or `CreditTeacher` is false; omit the location when empty). `OpenedCount`/`SavedCount`/`CookedCount` stay the seeded `318/64/11` — there is no analytics source yet; leave a `// Stage 5+: real counts` comment.
- `ProfileViewModel`: tapping a contribution row navigates by status: `Published` → `sharePublished`, `ChangesRequested` → `shareChanges`, otherwise `shareReview`, always with `id`.
- Route params: `MainShellPage.Navigate(route, params)` already carries a dictionary; the three lifecycle views read `id` the same way `RecipeView` reads its dish id (copy that pattern exactly).

- [ ] **Step 1: Write the failing tests**

Replace the seeded-data assertions in `ShareLifecycleTests.cs` with tests over a `FakeContributionService : IContributionService` that returns scripted `ContributionDetailDto`s (put it in `TasteZambia.Core.Tests/Fakes/FakeContributionService.cs`; it delegates `TimelineFor`/`FlagsFor` to a real `ContributionService` so the mapping stays under test once):

```csharp
[Fact]
public async Task Review_LoadsTheContributionByIdAndBuildsTheTimeline()
{
    var fake = new FakeContributionService();
    var id = fake.Add(Detail(ContributionStatus.InReview, [Submitted(), Read("Namakau Sitali")]));
    var vm = new ShareReviewViewModel(fake, new Nav()) { Id = id };

    await vm.InitializeAsync();

    Assert.Equal("Chibwabwa na Mbalala", vm.DishName);
    Assert.Equal("Northern Province · submitted 2 Sep 2026", vm.SubmittedMeta);
    Assert.Equal("Namakau Sitali", vm.ReviewerName);
    Assert.Equal("Northern Province records", vm.ReviewerScope);
    Assert.Equal(4, vm.Timeline.Count);
    Assert.True(vm.CanWithdraw);
    Assert.False(vm.HasChangesRequested);
}

[Fact]
public async Task Review_Withdraw_CallsTheServiceAndGoesBack()
{
    var fake = new FakeContributionService(); var nav = new Nav();
    var id = fake.Add(Detail(ContributionStatus.InReview, [Submitted()]));
    var vm = new ShareReviewViewModel(fake, nav) { Id = id };
    await vm.InitializeAsync();

    await vm.WithdrawCommand.ExecuteAsync(null);

    Assert.Equal(id, fake.Withdrawn.Single());
    Assert.True(nav.WentBack);
}

[Fact]
public async Task Changes_ResubmitIsDisabledUntilEveryQuestionIsAnswered()
{
    var fake = new FakeContributionService();
    var id = fake.Add(Detail(ContributionStatus.ChangesRequested, [Submitted(), Read("Namakau Sitali"), ChangesRequested("Namakau Sitali", "Two things.")],
        [new(1, "Local name", "Mungwi or Kasama?", "Chibwabwa na Mbalala", null), new(2, "Cooking step 2", "How long?", "Pound…", null)]));
    var vm = new ShareChangesViewModel(fake, new Nav()) { Id = id };
    await vm.InitializeAsync();

    Assert.Equal("Namakau Sitali", vm.ReviewerName);
    Assert.Equal("Two things.", vm.ReviewerNote);
    Assert.Equal(2, vm.Flagged.Count);
    Assert.False(vm.ResubmitCommand.CanExecute(null));

    vm.Flagged[0].Answer = "Mungwi specifically.";
    vm.Flagged[1].Answer = "About ten minutes by hand.";
    vm.AnswersChanged();
    Assert.True(vm.ResubmitCommand.CanExecute(null));

    await vm.ResubmitCommand.ExecuteAsync(null);
    Assert.Equal([1, 2], fake.Resubmitted[id].Select(a => a.FlagId));
}

[Fact]
public async Task Published_BuildsTheCreditLine()
{
    var fake = new FakeContributionService();
    var id = fake.Add(Detail(ContributionStatus.Published, [Submitted(), Read("Namakau Sitali"), Published("Namakau Sitali")])
        with { ContributorName = "Chanda Mwaba", ContributorLocation = "Kitwe", TaughtBy = "Banakulu Mwaba", TaughtByOrigin = "Mungwi, Northern Province" });
    var vm = new SharePublishedViewModel(fake, new Nav()) { Id = id };
    await vm.InitializeAsync();

    Assert.Equal("Recorded by Chanda Mwaba, Kitwe. As taught by Banakulu Mwaba of Mungwi, Northern Province. Verified against provincial records, September 2026.", vm.Credit);
}
```

Helpers `Submitted()`, `Read(actor)`, `ChangesRequested(actor, note)`, `Published(actor)` build `ReviewEventDto`s on 2, 3, 4 and 12 Sep 2026 respectively. Because `FlaggedField.Answer` is a mutable property on a record the VM exposes an explicit `AnswersChanged()` that re-evaluates `CanResubmit` (the view calls it from the editor's `TextChanged`).

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test TasteZambia.Core.Tests --filter ShareLifecycleTests`
Expected: FAIL.

- [ ] **Step 3: Rewrite the three ViewModels**

```csharp
public sealed partial class ShareReviewViewModel(IContributionService contributions, INavigationService navigation) : BaseViewModel(navigation)
{
    public ObservableCollection<ReviewStep> Timeline { get; } = [];

    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _dishName = "";
    [ObservableProperty] private string _submittedMeta = "";
    [ObservableProperty] private string _reviewerName = "The archive team";
    [ObservableProperty] private string _reviewerScope = "";
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(WithdrawCommand))] private bool _canWithdraw;
    [ObservableProperty] private bool _hasChangesRequested;

    public override async Task InitializeAsync()
    {
        var d = await contributions.GetDetailAsync(Id);
        if (d is null) return;

        DishName = d.LocalName;
        SubmittedMeta = $"{d.Province} Province · submitted {d.SubmittedAt.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";
        ReviewerName = d.Events.FirstOrDefault(e => e.Kind == ReviewEventKind.Read)?.Actor ?? "The archive team";
        ReviewerScope = $"{d.Province} Province records";
        CanWithdraw = d.Status is ContributionStatus.InReview or ContributionStatus.ChangesRequested;
        HasChangesRequested = d.Status == ContributionStatus.ChangesRequested;

        Timeline.Clear();
        foreach (var s in contributions.TimelineFor(d)) Timeline.Add(s);
    }

    [RelayCommand(CanExecute = nameof(CanWithdraw))]
    private async Task Withdraw()
    {
        await contributions.WithdrawAsync(Id);
        await Navigation.GoBackAsync();
    }

    [RelayCommand] private Task SeeQuestions() => Navigation.GoToAsync("shareChanges", new() { ["id"] = Id });
    [RelayCommand] private Task Back() => Navigation.GoBackAsync();
}
```

`ShareChangesViewModel` and `SharePublishedViewModel` follow the interface block above; `ShareChangesViewModel.Resubmit` builds `[.. Flagged.Select(f => new FlagAnswerDto(f.Id, f.Answer))]`, awaits `ResubmitAsync`, then `Navigation.GoToAsync("shareReview", new() { ["id"] = Id })`. `CanResubmit => Flagged.Count > 0 && Flagged.All(f => f.Answer.Trim().Length > 0)`; `AnswersChanged()` → `ResubmitCommand.NotifyCanExecuteChanged()`.

`SharePublishedViewModel.Credit`:

```csharp
var taught = d.CreditTeacher && d.TaughtBy.Length > 0
    ? $" As taught by {d.TaughtBy}{(d.TaughtByOrigin.Length > 0 ? $" of {d.TaughtByOrigin}" : "")}."
    : "";
var location = d.ContributorLocation.Length > 0 ? $", {d.ContributorLocation}" : "";
var verified = (d.Events.LastOrDefault(e => e.Kind == ReviewEventKind.Published)?.At ?? d.SubmittedAt).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
Credit = $"Recorded by {d.ContributorName}{location}.{taught} Verified against provincial records, {verified}.";
```

Check the exact `GoToAsync` parameter type in `INavigationService` (Stage 1 mobile plan used `IDictionary<string, object>`) and match it.

- [ ] **Step 4: Views and routes**

- `ShareChangesView.xaml`: under each flagged field, an `Editor` bound to `Answer` with `TextChanged` → code-behind calls `vm.AnswersChanged()`. Style it with the existing input style from the Share wizard (`Styles.xaml` — find the `Entry`/`Editor` style the wizard uses and reuse it; no new colours).
- `ShareReviewView.xaml`: the existing "Withdraw" button binds `WithdrawCommand`; add a "See the questions" `CircleButton`/pill (existing styles) visible when `HasChangesRequested`. Keep layout as designed.
- `MainShellPage.xaml.cs` route table: `shareReview`, `shareChanges`, `sharePublished` views read `id` from the params the way `RecipeView` reads `id` and set it on their VM before `InitializeAsync`.
- `ProfileView`: contribution rows get a tap → `ProfileViewModel.OpenContributionCommand(row)` which navigates per status as in the interface block.
- After a successful submit in `ShareView`, the "View status" affordance (if the design has one on the submitted state; check `ShareView.xaml`) navigates to `shareReview` with the new id — `ShareViewModel` keeps `SubmittedId`.

- [ ] **Step 5: Run everything, build, and walk it on the phone**

Run: `dotnet test TasteZambia.Core.Tests && dotnet test TasteZambia.API.Tests` → PASS. `dotnet build TasteZambia.Mobile -f net10.0-android --no-incremental` → no warnings.

On the phone with the API running:
1. Share → walk the four steps → Submit. `psql`: `select "LocalName","Status" from contributions;` shows `Chibwabwa na Mbalala | 1`.
2. Profile → the contribution row shows "In review" and opens the Review screen with a real timeline (Submitted Done, the rest pending/in progress).
3. In Scalar as the dev reviewer: `POST /review/{id}/request-changes` with one flag. Phone → Profile → the row now reads "Changes requested"; open → questions screen; answer; Resubmit → back to the timeline with "Resubmitted".
4. Scalar: `POST /review/{id}/publish`. Phone → Explore: **Chibwabwa na Mbalala is in the archive**, opens as a recipe with the contributor credited. Profile row → "Published" → the published screen with the credit line.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(mobile): review, changes and published screens on real contributions

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage.** Stage 3 in the backend plan: the six tables → five (no `ReviewAssignment`, decision 3; no `ContributionPhoto`, decision 4) plus `Dish.Provenance` (decision 1). State machine in a service both controllers obey (Task 2, exhaustively tested including illegal transitions and double-publish). Endpoints: `/me/drafts` deliberately not built (decision 2) — drafts are local; `POST /me/contributions` + `submit` are one call because a draft never exists on the server; `GET .../timeline` is folded into the detail DTO (`Events`); `/review/queue`, `request-changes`, `publish` as specified; `withdraw` and `resubmit` added because the design's Review and Changes screens need them. Mobile Tasks 15 and 20 (Share wizard, lifecycle screens) are unblocked and rewired in Tasks 5–6.

**Placeholder scan.** One deliberate deferral: `SharePublishedViewModel` keeps the seeded `318/64/11` stats with a comment — there is no analytics source and inventing one is out of scope. Everything else has code.

**Type consistency.** `SubmitContributionRequest` has the same 13 positional parameters in the contract (Task 2), the API tests (Task 3), and the Core mapping (Task 5). `FlagAnswerDto(int FlagId, string Answer)` is used by the API service, the API controller and the Core VM. `ReviewEventKind` values are the same enum on both sides via Shared. `ContributionDetailDto`'s 14 parameters match between `ContributionMappings.ToDetail` and the Core tests' `Detail(...)` helper. `SignedInClientAsync` returns `(HttpClient, string)` from Task 4 onward — Task 3's tests are written against that shape too (deconstruct and discard the name).

**Three risks for the executor.**
1. **Roles in the JWT.** `[Authorize(Roles = "reviewer")]` reads role claims from the token; Stage 2's `TokenService` issues none. Task 3 changes `IssueAsync` to take the role list — miss this and every reviewer test is 403.
2. **`DishDto` gains a parameter.** `SerializationTests` constructs it positionally; add the `Provenance` argument there, and check `DtoMappings.ToDish` on the mobile side still compiles (it should — it reads by name).
3. **`Recipe.Steps` numbering.** `DishRepository.GetRecipeAsync` orders steps by `Number`; `BuildDish` sets `Number = SortOrder + 1`. If a published recipe shows its steps out of order, this is where to look.
