# Stage 4 — Media and the Family Archive Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Photos and voice recordings become real files the archive stores, and the five family screens become a real private tier — a recipe preserved for a family, visible to the people the owner invites, published only when the owner says so.

**Architecture:** Blobs go through `IMediaStore`, whose only implementation for now writes to disk under a configured root; swapping it for S3 or Azure Blob later touches one class. A `MediaAsset` row records what each blob is, who owns it and what it belongs to. The family tier is a `FamilyRecipe` aggregate (members, notes, media, provenance) whose visibility rule — owner, plus accepted members, plus everyone once published — lives in `FamilyAccessService` and is applied by every query, never by a controller. On the phone, `MediaPicker`/`MediaRecorder` produce a file, an uploader posts it and stores the returned id on the draft, and the family screens read the account instead of `FamilyArchiveService`.

**Tech Stack:** ASP.NET Core 10, EF Core 10 + Npgsql, `Microsoft.AspNetCore.Http.Features` for multipart limits, MAUI `MediaPicker` (photos) and `Plugin.Maui.Audio` (recording), Testcontainers + xUnit.

**Spec:** `Docs/plans/2026-09-09-backend-api-plan.md` — "Stage 4 — Media and the family archive", and `Docs/plans/2026-09-25-completion-roadmap.md` §1.
**Builds on:** Stage 2 (`ICurrentUser`, `[Authorize]`, `ILocalStore`, the `"me"` HttpClient), Stage 3 (`ContributionService`'s state machine, `DraftStore`, the review roles).

## Decisions recorded

1. **One blob per recording, not chunked.** A 12-minute recording is roughly 12 MB at a sane bitrate. One blob with HTTP range requests is enough and much simpler than a chunked pipeline.
2. **Disk in Development, the same interface in Production.** `IMediaStore` has one implementation now (`FileSystemMediaStore`). Production swaps the implementation and nothing above it changes.
3. **Family recipes are not contributions.** They share nothing but the contributor. A family recipe can be *published* into the archive later, which runs the same `ContributionService.PublishAsync` path Stage 3 built, but until then it is private data with its own table.
4. **Invites are by name, not email.** There is no email on an anonymous account. An invite mints a short code the owner shares however they like; whoever enters it on their own device joins. This keeps the anonymous-account design intact.
5. **Transcription is Stage 5.** An uploaded recording is stored and playable; `TranscriptState` is recorded as `None` and the workflow that fills it comes later.

## Global Constraints

### Carried from Stages 1–3
- Dependency rule: `Controllers/ → Services/ → Repositories/ → DbContext`. Access rules live **only** in services; no controller filters rows by hand.
- Wire types live in `TasteZambia.Shared/Contracts/`; changes are additive. Validation attributes go on record **constructor parameters**, not `[property:]` — MVC ignores the latter on positional records.
- No `[Produces("application/json")]` on a controller. Problem details for errors; a forbidden action on someone else's row is **404, never 403** — do not leak existence.
- `TasteZambia.Core` references nothing MAUI. Anything touching `MediaPicker`, the filesystem or a microphone lives in `TasteZambia.Mobile` behind a Core interface.
- Seed and design copy is never paraphrased.
- Repair the `.sln` header after any `dotnet sln` command: strip the newline that SDK 10.0.201 inserts after the UTF-8 BOM.

### New for Stage 4
- **Uploads are bounded before they are read.** 10 MB for an image, 40 MB for audio, enforced by `RequestSizeLimit` on the action *and* checked against the declared content type. An unbounded multipart endpoint is a denial-of-service endpoint.
- **Content types are allow-listed**, not sniffed from the filename: `image/jpeg`, `image/png`, `image/webp`, `audio/mp4`, `audio/aac`, `audio/mpeg`, `audio/wav`. Anything else is 415.
- **Stored names are generated, never the client's.** A `MediaAsset.Id` (Guid) plus the extension from the allow-listed type. A client-supplied filename never reaches the filesystem.
- **Every media read is authorised through `IFamilyAccessService`.** A media id is a Guid, but guessing is not the threat model — sharing a URL is. Ownership is checked on every fetch.
- **`FamilyRecipe` queries go through `FamilyAccessService.VisibleTo(userId)`.** There is no other way to read one. A reviewer sees a family recipe only after it is submitted for publication.
- **Timestamps come from `TimeProvider`**, never `DateTimeOffset.UtcNow` inside a service — tests pin the clock.

## Review Focus

Five things the spec implies, that a careless implementation gets wrong, and where each is pinned:

1. **A member removed from a family recipe loses access immediately** — not on their next sign-in, and not only in the list view. Pinned in Task 3.
2. **An upload that dies halfway leaves no half-written blob and no orphan row.** Pinned in Task 1.
3. **A media id belonging to someone else's family recipe returns 404 to a stranger**, including when they hold the raw id. Pinned in Task 2.
4. **Privacy that moves backwards still holds**: a recipe set to public and then back to family is no longer readable by strangers, and its archive dish (if one was published) is a separate decision the owner is told about. Pinned in Task 4.
5. **A phone that uploads while offline keeps the file and retries**, rather than dropping the photo the reader just took. Pinned in Task 6.

---

## File Structure

### `TasteZambia.Shared`
| Path | Holds |
|---|---|
| `Enums/ArchiveEnums.cs` | `+ MediaKind`, `+ MemberState`, `+ TranscriptState` |
| `Contracts/Media/MediaContracts.cs` | `MediaAssetDto`, `UploadResultDto` |
| `Contracts/Family/FamilyContracts.cs` | `FamilyRecipeDto`, `FamilyRecipeSummaryDto`, `CreateFamilyRecipeRequest`, `UpdateFamilyRecipeRequest`, `FamilyMemberDto`, `InviteDto`, `AcceptInviteRequest`, `FamilyNoteDto`, `AddNoteRequest`, `SetPrivacyRequest` |
| `Validation/FieldLimits.cs` | `+ MediaLimits` (byte ceilings, allowed types) |
| `Routes/ApiRoutes.cs` | `Media.*`, `Family.*` |

### `TasteZambia.API`
| Path | Holds |
|---|---|
| `Media/IMediaStore.cs` | `IMediaStore`, `StoredBlob` |
| `Media/FileSystemMediaStore.cs` | Disk implementation, atomic write |
| `Data/Entities/Family.cs` | `MediaAsset`, `FamilyRecipe`, `FamilyMember`, `FamilyNote`, `FamilyInvite` |
| `Data/Configurations/FamilyConfigurations.cs` | Tables, keys, cascades, indexes |
| `Repositories/FamilyRepository.cs` | `IFamilyRepository` — every read takes a `userId` |
| `Services/FamilyAccessService.cs` | `IFamilyAccessService` — the one visibility rule |
| `Services/MediaService.cs` | `IMediaService` — validate, store, record, delete |
| `Controllers/MediaController.cs` | `POST /media`, `GET /media/{id}` |
| `Controllers/FamilyController.cs` | The family tier |
| `Mapping/FamilyMappings.cs` | entity → DTO |

### `TasteZambia.Core`
| Path | Holds |
|---|---|
| `Models/FamilyArchive.cs` | Models gain ids and real fields |
| `Services/IMediaPicker.cs` | `IPhotoPicker`, `IVoiceRecorder`, `PickedFile` |
| `Services/MediaUploader.cs` | `IMediaUploader` — upload with an offline queue |
| `Services/FamilyArchiveService.cs` | **Rewritten** over HTTP |
| `Data/Http/HttpFamilyRepository.cs` | `IFamilyRepository` (Core-side) |
| `ViewModels/FamilyLifecycleViewModels.cs` | Bound to a real recipe |
| `ViewModels/ShareViewModel.cs` | Photo rows |

### `TasteZambia.Mobile`
| Path | Holds |
|---|---|
| `Services/MauiPhotoPicker.cs` | `MediaPicker` + `FilePicker` |
| `Services/MauiVoiceRecorder.cs` | `Plugin.Maui.Audio` |
| `Views/Family/*.xaml` | Real bindings, empty states |
| `Views/Share/ShareView.xaml` | Photo strip |

### Tests
| Path | Covers |
|---|---|
| `TasteZambia.API.Tests/Media/FileSystemMediaStoreTests.cs` | Write, read, delete, atomicity |
| `TasteZambia.API.Tests/Media/MediaEndpointTests.cs` | Limits, types, ownership, 404 for strangers |
| `TasteZambia.API.Tests/Services/FamilyAccessTests.cs` | Every visibility case |
| `TasteZambia.API.Tests/Controllers/FamilyEndpointTests.cs` | Create, invite, accept, note, privacy |
| `TasteZambia.Core.Tests/Services/MediaUploaderTests.cs` | Queue, retry, ordering |
| `TasteZambia.Core.Tests/ViewModels/FamilyLifecycleTests.cs` | Rewritten for real data |

---

## Task 1: The media store

**Files:**
- Create: `TasteZambia.API/Media/IMediaStore.cs`, `TasteZambia.API/Media/FileSystemMediaStore.cs`
- Modify: `TasteZambia.Shared/Validation/FieldLimits.cs`, `TasteZambia.API/Program.cs`, `TasteZambia.API/appsettings.Development.json`
- Test: `TasteZambia.API.Tests/Media/FileSystemMediaStoreTests.cs`

**Interfaces:**
- Produces:
  - `StoredBlob(Guid Id, string ContentType, long Length)`
  - `IMediaStore`:
    - `Task<StoredBlob> SaveAsync(Guid id, string contentType, Stream content, CancellationToken ct)`
    - `Task<Stream?> OpenAsync(Guid id, string contentType, CancellationToken ct)` — null when the blob is missing
    - `Task DeleteAsync(Guid id, string contentType, CancellationToken ct)`
  - `MediaLimits` in Shared: `const long MaxImageBytes = 10 * 1024 * 1024;`, `const long MaxAudioBytes = 40 * 1024 * 1024;`, `static readonly string[] ImageTypes = ["image/jpeg", "image/png", "image/webp"];`, `static readonly string[] AudioTypes = ["audio/mp4", "audio/aac", "audio/mpeg", "audio/wav"];`, `static string? ExtensionFor(string contentType)` returning `.jpg/.png/.webp/.m4a/.aac/.mp3/.wav` or null
  - Configuration: `Media:Root` — the directory blobs live under; Development uses `./media-dev`

- [ ] **Step 1: Write the failing test**

`TasteZambia.API.Tests/Media/FileSystemMediaStoreTests.cs`:

```csharp
using TasteZambia.API.Media;

namespace TasteZambia.API.Tests.Media;

public class FileSystemMediaStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"tz-media-{Guid.NewGuid():N}");

    private FileSystemMediaStore Sut() => new(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Save_ThenOpen_ReturnsTheSameBytes()
    {
        var store = Sut();
        var id = Guid.NewGuid();
        var bytes = new byte[] { 1, 2, 3, 4, 5 };

        var stored = await store.SaveAsync(id, "image/jpeg", new MemoryStream(bytes), default);

        Assert.Equal(id, stored.Id);
        Assert.Equal(5, stored.Length);

        await using var read = await store.OpenAsync(id, "image/jpeg", default);
        Assert.NotNull(read);
        using var buffer = new MemoryStream();
        await read!.CopyToAsync(buffer);
        Assert.Equal(bytes, buffer.ToArray());
    }

    [Fact]
    public async Task Open_AMissingBlob_IsNullNotAnException()
    {
        Assert.Null(await Sut().OpenAsync(Guid.NewGuid(), "image/jpeg", default));
    }

    [Fact]
    public async Task Delete_RemovesIt_AndIsSafeToRepeat()
    {
        var store = Sut();
        var id = Guid.NewGuid();
        await store.SaveAsync(id, "audio/mp4", new MemoryStream([9, 9]), default);

        await store.DeleteAsync(id, "audio/mp4", default);
        await store.DeleteAsync(id, "audio/mp4", default);   // no throw the second time

        Assert.Null(await store.OpenAsync(id, "audio/mp4", default));
    }

    [Fact]
    public async Task AFailedWrite_LeavesNoPartialBlobBehind()
    {
        var store = Sut();
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<IOException>(() => store.SaveAsync(id, "image/png", new FailingStream(), default));

        // The half-written file must not be readable as a real one.
        Assert.Null(await store.OpenAsync(id, "image/png", default));
        Assert.Empty(Directory.GetFiles(_root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void ExtensionFor_MapsEveryAllowedType_AndRefusesTheRest()
    {
        Assert.Equal(".jpg", TasteZambia.Shared.Validation.MediaLimits.ExtensionFor("image/jpeg"));
        Assert.Equal(".m4a", TasteZambia.Shared.Validation.MediaLimits.ExtensionFor("audio/mp4"));
        Assert.Null(TasteZambia.Shared.Validation.MediaLimits.ExtensionFor("application/pdf"));
        Assert.Null(TasteZambia.Shared.Validation.MediaLimits.ExtensionFor("image/svg+xml"));   // scriptable
    }

    /// <summary>Throws part-way through, the way a dropped connection does.</summary>
    private sealed class FailingStream : Stream
    {
        private int _reads;
        public override int Read(byte[] buffer, int offset, int count)
            => _reads++ == 0 ? Fill(buffer, count) : throw new IOException("connection lost");

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct)
            => _reads++ == 0 ? await Task.FromResult(Math.Min(buffer.Length, 128)) : throw new IOException("connection lost");

        private static int Fill(byte[] buffer, int count) => Math.Min(count, 128);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter FileSystemMediaStoreTests`
Expected: FAIL — `TasteZambia.API.Media` does not exist.

- [ ] **Step 3: Write the limits**

Append to `TasteZambia.Shared/Validation/FieldLimits.cs`:

```csharp
/// <summary>
/// What the archive will accept as an upload. Types are allow-listed rather than sniffed
/// from a filename, and the extension is derived here so a client-supplied name never
/// reaches the filesystem.
/// </summary>
public static class MediaLimits
{
    public const long MaxImageBytes = 10 * 1024 * 1024;
    public const long MaxAudioBytes = 40 * 1024 * 1024;

    public static readonly string[] ImageTypes = ["image/jpeg", "image/png", "image/webp"];
    public static readonly string[] AudioTypes = ["audio/mp4", "audio/aac", "audio/mpeg", "audio/wav"];

    public static bool IsImage(string contentType) => ImageTypes.Contains(contentType);
    public static bool IsAudio(string contentType) => AudioTypes.Contains(contentType);

    public static long MaxBytesFor(string contentType)
        => IsAudio(contentType) ? MaxAudioBytes : MaxImageBytes;

    /// <summary>The stored extension for an allowed type, or null when the type is not allowed.</summary>
    public static string? ExtensionFor(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "audio/mp4" => ".m4a",
        "audio/aac" => ".aac",
        "audio/mpeg" => ".mp3",
        "audio/wav" => ".wav",
        _ => null,
    };
}
```

- [ ] **Step 4: Write the store**

`TasteZambia.API/Media/IMediaStore.cs`:

```csharp
namespace TasteZambia.API.Media;

public readonly record struct StoredBlob(Guid Id, string ContentType, long Length);

/// <summary>
/// Where the bytes live. One implementation today writes to disk; production swaps it
/// for object storage and nothing above this interface changes.
/// </summary>
public interface IMediaStore
{
    Task<StoredBlob> SaveAsync(Guid id, string contentType, Stream content, CancellationToken ct);

    /// <summary>The blob's bytes, or null when it is not there.</summary>
    Task<Stream?> OpenAsync(Guid id, string contentType, CancellationToken ct);

    Task DeleteAsync(Guid id, string contentType, CancellationToken ct);
}
```

`TasteZambia.API/Media/FileSystemMediaStore.cs`:

```csharp
using TasteZambia.Shared.Validation;

namespace TasteZambia.API.Media;

/// <summary>
/// Blobs on disk, two levels of fan-out so no directory holds a million entries.
/// Writes go to a temporary file and are moved into place, so a dropped connection
/// leaves nothing readable behind.
/// </summary>
public sealed class FileSystemMediaStore(string root) : IMediaStore
{
    public async Task<StoredBlob> SaveAsync(Guid id, string contentType, Stream content, CancellationToken ct)
    {
        var path = PathFor(id, contentType);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temporary = path + ".partial";
        long length;
        try
        {
            await using (var file = File.Create(temporary))
            {
                await content.CopyToAsync(file, ct);
                length = file.Length;
            }
            File.Move(temporary, path, overwrite: true);
        }
        catch
        {
            // Nothing half-written survives: the reader asked for a photo, not a fragment.
            if (File.Exists(temporary)) File.Delete(temporary);
            throw;
        }

        return new StoredBlob(id, contentType, length);
    }

    public Task<Stream?> OpenAsync(Guid id, string contentType, CancellationToken ct)
    {
        var path = PathFor(id, contentType);
        return Task.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }

    public Task DeleteAsync(Guid id, string contentType, CancellationToken ct)
    {
        var path = PathFor(id, contentType);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string PathFor(Guid id, string contentType)
    {
        var name = id.ToString("N");
        var extension = MediaLimits.ExtensionFor(contentType)
            ?? throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Not an allowed media type.");
        return Path.Combine(root, name[..2], name[2..4], name + extension);
    }
}
```

Register in `Program.cs`:

```csharp
builder.Services.AddSingleton<IMediaStore>(_ =>
    new FileSystemMediaStore(builder.Configuration["Media:Root"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "media-dev")));
```

and add `"Media": { "Root": "media-dev" }` to `appsettings.Development.json`. Add `media-dev/` to `.gitignore`.

- [ ] **Step 5: Run the tests**

Run: `dotnet test TasteZambia.API.Tests --filter FileSystemMediaStoreTests`
Expected: PASS, 5 tests.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(api): media store with allow-listed types and atomic writes

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 2: Media entities, service and endpoints

**Files:**
- Create: `TasteZambia.API/Data/Entities/Family.cs` (the `MediaAsset` part), `TasteZambia.API/Services/MediaService.cs`, `TasteZambia.API/Controllers/MediaController.cs`, `TasteZambia.Shared/Contracts/Media/MediaContracts.cs`
- Modify: `TasteZambia.Shared/Enums/ArchiveEnums.cs`, `TasteZambia.Shared/Routes/ApiRoutes.cs`, `TasteZambia.API/Data/TasteZambiaDbContext.cs`, `Program.cs`
- Test: `TasteZambia.API.Tests/Media/MediaEndpointTests.cs`

**Interfaces:**
- Produces:
  - `enum MediaKind { Photo = 0, Audio = 1 }`
  - `MediaAsset { Guid Id; string UserId; MediaKind Kind; string ContentType; long Length; string? Caption; DateTimeOffset CreatedAt; Guid? FamilyRecipeId; Guid? ContributionId; }`
  - `MediaAssetDto(Guid Id, MediaKind Kind, string ContentType, long Length, string? Caption, DateTimeOffset CreatedAt)`
  - `UploadResultDto(Guid Id, string Url)` — `Url` is `/api/v1/media/{id}`
  - `IMediaService`:
    - `Task<MediaAsset> UploadAsync(string userId, MediaKind kind, string contentType, Stream content, long declaredLength, CancellationToken ct)` — throws `ArgumentOutOfRangeException` for a disallowed type, `InvalidDataException` when over the ceiling
    - `Task<(MediaAsset Asset, Stream Content)?> OpenAsync(Guid id, string userId, CancellationToken ct)` — null when missing **or** not visible to that user
    - `Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct)`
  - Routes: `Media.Collection = "/api/v1/media"`, `Media.ById = "/api/v1/media/{id}"`
  - `POST /media` multipart with `file` and `kind` → 201 `UploadResultDto`; 415 for a disallowed type; 413 over the ceiling
  - `GET /media/{id}` → the bytes with its content type, `Cache-Control: private, max-age=86400`; 404 when missing or not yours

Visibility at this stage: an asset is visible to its uploader. Task 3 widens that to family members without changing this signature.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Media;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Media;

[Collection(nameof(DatabaseCollection))]
public class MediaEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        (_client, _) = await _factory.SignedInClientAsync();
    }

    public Task DisposeAsync() { _client.Dispose(); _factory.Dispose(); return Task.CompletedTask; }

    private static MultipartFormDataContent Upload(byte[] bytes, string contentType, MediaKind kind = MediaKind.Photo)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        return new MultipartFormDataContent
        {
            { file, "file", "whatever-the-client-called-it.jpg" },
            { new StringContent(kind.ToString()), "kind" },
        };
    }

    [Fact]
    public async Task Upload_ThenFetch_ReturnsTheBytesWithTheirType()
    {
        var bytes = new byte[] { 7, 7, 7, 7 };

        var response = await _client.PostAsync(ApiRoutes.Media.Collection, Upload(bytes, "image/jpeg"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UploadResultDto>();

        var fetched = await _client.GetAsync(result!.Url);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        Assert.Equal("image/jpeg", fetched.Content.Headers.ContentType!.MediaType);
        Assert.Equal(bytes, await fetched.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Upload_OfADisallowedType_Is415()
    {
        var response = await _client.PostAsync(ApiRoutes.Media.Collection, Upload([1], "application/pdf"));
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Upload_OverTheImageCeiling_Is413()
    {
        var tooBig = new byte[TasteZambia.Shared.Validation.MediaLimits.MaxImageBytes + 1];
        var response = await _client.PostAsync(ApiRoutes.Media.Collection, Upload(tooBig, "image/png"));
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task SomeoneElsesMedia_Is404_EvenHoldingTheId()
    {
        var mine = await (await _client.PostAsync(ApiRoutes.Media.Collection, Upload([1, 2], "image/png")))
            .Content.ReadFromJsonAsync<UploadResultDto>();

        var (stranger, _) = await _factory.SignedInClientAsync();
        using (stranger)
            Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(mine!.Url)).StatusCode);
    }

    [Fact]
    public async Task Media_WithoutAToken_Is401()
    {
        using var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync(ApiRoutes.Media.ById.Replace("{id}", Guid.NewGuid().ToString()))).StatusCode);
    }

    [Fact]
    public async Task AnAudioUpload_IsAllowedUpToTheAudioCeiling()
    {
        var bytes = new byte[12 * 1024 * 1024];   // a 12-minute recording, over the image limit
        var response = await _client.PostAsync(ApiRoutes.Media.Collection, Upload(bytes, "audio/mp4", MediaKind.Audio));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter MediaEndpointTests`
Expected: FAIL.

- [ ] **Step 3: Enums, contracts and routes**

`ArchiveEnums.cs` — append:

```csharp
/// <summary>What a stored blob is. Decides its size ceiling and how the app renders it.</summary>
public enum MediaKind
{
    Photo = 0,
    Audio = 1,
}
```

`TasteZambia.Shared/Contracts/Media/MediaContracts.cs`:

```csharp
using TasteZambia.Shared.Enums;

namespace TasteZambia.Shared.Contracts.Media;

public sealed record MediaAssetDto(Guid Id, MediaKind Kind, string ContentType, long Length, string? Caption, DateTimeOffset CreatedAt)
{
    /// <summary>Where the bytes are. Relative, so it works against whichever host the app is pointed at.</summary>
    public string Url => $"/api/v1/media/{Id}";
}

public sealed record UploadResultDto(Guid Id, string Url);
```

`ApiRoutes` — add:

```csharp
public static class Media
{
    public const string Collection = $"{Root}/media";
    public const string ById = $"{Collection}/{{id}}";
}
```

- [ ] **Step 4: Entity, service, controller**

`TasteZambia.API/Data/Entities/Family.cs` (start the file with the asset; Task 3 appends the rest):

```csharp
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Data.Entities;

/// <summary>
/// One stored blob. The row is the record; the bytes live in IMediaStore. Deleting the
/// row without the blob leaks disk, so MediaService always does both.
/// </summary>
public class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public MediaKind Kind { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public string? Caption { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>What it belongs to. Both null while it is still just an upload.</summary>
    public Guid? FamilyRecipeId { get; set; }
    public Guid? ContributionId { get; set; }
}
```

`TasteZambia.API/Services/MediaService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Media;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Validation;

namespace TasteZambia.API.Services;

public interface IMediaService
{
    Task<MediaAsset> UploadAsync(string userId, MediaKind kind, string contentType, Stream content, long declaredLength, CancellationToken ct);

    /// <summary>The asset and its bytes, or null when it does not exist or is not this user's to read.</summary>
    Task<(MediaAsset Asset, Stream Content)?> OpenAsync(Guid id, string userId, CancellationToken ct);

    Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct);
}

public sealed class MediaService(TasteZambiaDbContext db, IMediaStore store, IFamilyAccessService access, TimeProvider clock) : IMediaService
{
    public async Task<MediaAsset> UploadAsync(string userId, MediaKind kind, string contentType, Stream content, long declaredLength, CancellationToken ct)
    {
        if (MediaLimits.ExtensionFor(contentType) is null)
            throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Not an allowed media type.");

        var ceiling = MediaLimits.MaxBytesFor(contentType);
        if (declaredLength > ceiling)
            throw new InvalidDataException($"That file is larger than the {ceiling / (1024 * 1024)} MB limit.");

        var asset = new MediaAsset
        {
            UserId = userId,
            Kind = kind,
            ContentType = contentType,
            Caption = null,
            CreatedAt = clock.GetUtcNow(),
        };

        var stored = await store.SaveAsync(asset.Id, contentType, content, ct);

        // The bytes landed but the row did not: that is a leaked blob, so clean up.
        asset.Length = stored.Length;
        try
        {
            db.MediaAssets.Add(asset);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await store.DeleteAsync(asset.Id, contentType, CancellationToken.None);
            throw;
        }

        return asset;
    }

    public async Task<(MediaAsset Asset, Stream Content)?> OpenAsync(Guid id, string userId, CancellationToken ct)
    {
        var asset = await db.MediaAssets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return null;
        if (!await access.CanReadMediaAsync(asset, userId, ct)) return null;

        var content = await store.OpenAsync(id, asset.ContentType, ct);
        return content is null ? null : (asset, content);
    }

    public async Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct)
    {
        var asset = await db.MediaAssets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (asset is null) return false;

        db.MediaAssets.Remove(asset);
        await db.SaveChangesAsync(ct);
        await store.DeleteAsync(id, asset.ContentType, ct);
        return true;
    }
}
```

`IFamilyAccessService` arrives in Task 3. For this task, write it with the one rule it needs so far, in `TasteZambia.API/Services/FamilyAccessService.cs`:

```csharp
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Services;

public interface IFamilyAccessService
{
    /// <summary>Whether this user may read this blob. Task 3 widens it to family members.</summary>
    Task<bool> CanReadMediaAsync(MediaAsset asset, string userId, CancellationToken ct);
}

public sealed class FamilyAccessService : IFamilyAccessService
{
    public Task<bool> CanReadMediaAsync(MediaAsset asset, string userId, CancellationToken ct)
        => Task.FromResult(asset.UserId == userId);
}
```

`TasteZambia.API/Controllers/MediaController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Auth;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Media;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;
using TasteZambia.Shared.Validation;

namespace TasteZambia.API.Controllers;

[ApiController]
[Authorize]
public sealed class MediaController(ICurrentUser me, IMediaService media) : ControllerBase
{
    private string UserId => me.UserId ?? throw new UnauthorizedAccessException();

    [HttpPost(ApiRoutes.Media.Collection)]
    [RequestSizeLimit(MediaLimits.MaxAudioBytes + 1024 * 1024)]   // the largest allowed, plus multipart overhead
    [ProducesResponseType<UploadResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] MediaKind kind, CancellationToken ct)
    {
        var contentType = file.ContentType;
        try
        {
            await using var content = file.OpenReadStream();
            var asset = await media.UploadAsync(UserId, kind, contentType, content, file.Length, ct);
            return Created(ApiRoutes.Media.ById.Replace("{id}", asset.Id.ToString()),
                new UploadResultDto(asset.Id, $"/api/v1/media/{asset.Id}"));
        }
        catch (ArgumentOutOfRangeException)
        {
            return Problem(title: "Not an allowed file type",
                detail: "The archive takes JPEG, PNG or WebP photos, and M4A, AAC, MP3 or WAV recordings.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }
        catch (InvalidDataException ex)
        {
            return Problem(title: "That file is too large", detail: ex.Message,
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }
    }

    [HttpGet(ApiRoutes.Media.ById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var found = await media.OpenAsync(id, UserId, ct);
        if (found is null) return this.NotFoundProblem("Not found", $"No media {id}.");

        // Private: this is somebody's family photograph, not a public asset.
        Response.Headers.CacheControl = "private, max-age=86400";
        return File(found.Value.Content, found.Value.Asset.ContentType, enableRangeProcessing: true);
    }
}
```

Add `public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();` to the context, a configuration in `FamilyConfigurations.cs` (`ToTable("media_assets")`, index on `UserId`, `FamilyRecipeId`, `ContributionId`, `ContentType` max length 80, `Caption` max length 300), and register `IMediaService`, `IFamilyAccessService` as scoped. Migration: `dotnet ef migrations add Media --project TasteZambia.API`.

- [ ] **Step 5: Run the tests**

Run: `dotnet test TasteZambia.API.Tests --filter MediaEndpointTests`
Expected: PASS, 6 tests.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(api): media upload and fetch, private to the uploader

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 3: The family recipe aggregate and its one access rule

**Files:**
- Modify: `TasteZambia.API/Data/Entities/Family.cs`, `Data/Configurations/FamilyConfigurations.cs`, `Services/FamilyAccessService.cs`, `TasteZambia.Shared/Enums/ArchiveEnums.cs`, `Program.cs`
- Create: `TasteZambia.API/Repositories/FamilyRepository.cs`
- Test: `TasteZambia.API.Tests/Services/FamilyAccessTests.cs`

**Interfaces:**
- Produces:
  - `enum MemberState { Invited = 0, Joined = 1, Removed = 2 }`
  - `enum TranscriptState { None = 0, Pending = 1, Transcribing = 2, AwaitingApproval = 3, Approved = 4 }`
  - `FamilyRecipe { Guid Id; string OwnerId; string LocalName; string Description; string Province; string Language; string TaughtBy; string TaughtByOrigin; string Story; string TraditionalMethod; PrivacyLevel Privacy; DateTimeOffset CreatedAt; DateTimeOffset UpdatedAt; string? PublishedDishId; List<FamilyMember> Members; List<FamilyNote> Notes; List<MediaAsset> Media; }`
  - `FamilyMember { Guid Id; Guid FamilyRecipeId; string? UserId; string DisplayName; string Relation; MemberState State; DateTimeOffset InvitedAt; DateTimeOffset? JoinedAt; }` — `UserId` is null until the invite is accepted
  - `FamilyNote { Guid Id; Guid FamilyRecipeId; string AuthorUserId; string AuthorName; string Body; DateTimeOffset CreatedAt; }`
  - `FamilyInvite { Guid Id; Guid FamilyRecipeId; Guid MemberId; string Code; DateTimeOffset ExpiresAt; DateTimeOffset? RedeemedAt; }` — `Code` is 8 characters from an unambiguous alphabet
  - `IFamilyRepository`: `GetAsync(Guid id, string userId, ct)` (null when not visible), `ListForUserAsync(string userId, ct)`, `Add(FamilyRecipe)`, `FindByInviteCodeAsync(string code, ct)`, `SaveChangesAsync(ct)`
  - `IFamilyAccessService` grows: `Task<bool> CanReadAsync(FamilyRecipe recipe, string userId, CancellationToken ct)`, `Task<bool> CanEditAsync(FamilyRecipe recipe, string userId, CancellationToken ct)` (owner only), `IQueryable<FamilyRecipe> VisibleTo(IQueryable<FamilyRecipe> source, string userId)`
  - The rule, stated once: **a family recipe is readable by its owner, by members whose state is `Joined`, and by everyone when `Privacy == PublicInArchive`. It is editable only by its owner.**

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Tests.Services;

[Collection(nameof(DatabaseCollection))]
public class FamilyAccessTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private async Task<(string owner, string member, string stranger)> UsersAsync()
    {
        await using var db = fixture.NewContext();
        var users = Enumerable.Range(0, 3).Select(_ => new ArchiveUser { UserName = $"device-{Guid.NewGuid():N}" }).ToList();
        db.Users.AddRange(users);
        await db.SaveChangesAsync();
        return (users[0].Id, users[1].Id, users[2].Id);
    }

    private async Task<Guid> PreserveAsync(string ownerId, PrivacyLevel privacy, (string userId, MemberState state)? member = null)
    {
        await using var db = fixture.NewContext();
        var recipe = new FamilyRecipe
        {
            OwnerId = ownerId,
            LocalName = "Ifisashi ya Banakulu",
            Description = "Pumpkin leaves in groundnuts, the way my grandmother made it",
            Province = "Northern",
            TaughtBy = "Banakulu Mwaba",
            Privacy = privacy,
            CreatedAt = T0,
            UpdatedAt = T0,
        };
        if (member is { } m)
            recipe.Members.Add(new FamilyMember { UserId = m.userId, DisplayName = "Mutinta", Relation = "Sister", State = m.state, InvitedAt = T0 });
        db.Set<FamilyRecipe>().Add(recipe);
        await db.SaveChangesAsync();
        return recipe.Id;
    }

    private FamilyRepository Repository() => new(fixture.NewContext(), new FamilyAccessService());

    [Fact]
    public async Task TheOwner_CanAlwaysRead()
    {
        var (owner, _, _) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.PrivateToMe);

        Assert.NotNull(await Repository().GetAsync(id, owner));
    }

    [Fact]
    public async Task AJoinedMember_CanRead_AndAStrangerCannot()
    {
        var (owner, member, stranger) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Joined));

        Assert.NotNull(await Repository().GetAsync(id, member));
        Assert.Null(await Repository().GetAsync(id, stranger));
    }

    [Fact]
    public async Task AnInvitedMemberWhoHasNotJoined_CannotReadYet()
    {
        var (owner, member, _) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Invited));

        Assert.Null(await Repository().GetAsync(id, member));
    }

    [Fact]
    public async Task ARemovedMember_LosesAccessImmediately()
    {
        var (owner, member, _) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Joined));
        Assert.NotNull(await Repository().GetAsync(id, member));

        await using (var db = fixture.NewContext())
        {
            var row = await db.Set<FamilyMember>().FirstAsync(m => m.FamilyRecipeId == id && m.UserId == member);
            row.State = MemberState.Removed;
            await db.SaveChangesAsync();
        }

        Assert.Null(await Repository().GetAsync(id, member));
    }

    [Fact]
    public async Task PublicInArchive_IsReadableByAnyone()
    {
        var (owner, _, stranger) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.PublicInArchive);

        Assert.NotNull(await Repository().GetAsync(id, stranger));
    }

    [Fact]
    public async Task PrivacyMovingBackToFamily_ShutsStrangersOutAgain()
    {
        var (owner, _, stranger) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.PublicInArchive);
        Assert.NotNull(await Repository().GetAsync(id, stranger));

        await using (var db = fixture.NewContext())
        {
            var row = await db.Set<FamilyRecipe>().FirstAsync(r => r.Id == id);
            row.Privacy = PrivacyLevel.SharedWithFamily;
            await db.SaveChangesAsync();
        }

        Assert.Null(await Repository().GetAsync(id, stranger));
    }

    [Fact]
    public async Task OnlyTheOwner_CanEdit()
    {
        var (owner, member, _) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Joined));

        await using var db = fixture.NewContext();
        var recipe = await db.Set<FamilyRecipe>().Include(r => r.Members).FirstAsync(r => r.Id == id);
        var access = new FamilyAccessService();

        Assert.True(await access.CanEditAsync(recipe, owner, default));
        Assert.False(await access.CanEditAsync(recipe, member, default));
    }

    [Fact]
    public async Task ListForUser_ShowsOwnedAndJoined_ButNotStrangersPrivateOnes()
    {
        var (owner, member, stranger) = await UsersAsync();
        await PreserveAsync(owner, PrivacyLevel.PrivateToMe);
        await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Joined));

        Assert.Equal(2, (await Repository().ListForUserAsync(owner)).Count);
        Assert.Single(await Repository().ListForUserAsync(member));
        Assert.Empty(await Repository().ListForUserAsync(stranger));
    }

    [Fact]
    public async Task AMembersMediaIsReadableByTheFamily_AndNotByStrangers()
    {
        var (owner, member, stranger) = await UsersAsync();
        var id = await PreserveAsync(owner, PrivacyLevel.SharedWithFamily, (member, MemberState.Joined));

        await using var db = fixture.NewContext();
        var asset = new MediaAsset { UserId = owner, Kind = MediaKind.Photo, ContentType = "image/jpeg", Length = 4, CreatedAt = T0, FamilyRecipeId = id };
        db.MediaAssets.Add(asset);
        await db.SaveChangesAsync();

        var access = new FamilyAccessService(fixture.NewContext());
        Assert.True(await access.CanReadMediaAsync(asset, member, default));
        Assert.False(await access.CanReadMediaAsync(asset, stranger, default));
    }
}
```

Note the last test constructs `FamilyAccessService` **with a context** — widening it from Task 2's parameterless version is part of this task. Give it `FamilyAccessService(TasteZambiaDbContext db)` and update Task 2's registration; the media tests still pass because an asset with no `FamilyRecipeId` falls back to the uploader check.

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter FamilyAccessTests`
Expected: FAIL.

- [ ] **Step 3: Enums and entities**

`ArchiveEnums.cs` — append:

```csharp
/// <summary>Where someone stands with a family recipe they were invited to.</summary>
public enum MemberState
{
    Invited = 0,
    Joined = 1,
    Removed = 2,
}

/// <summary>How far a recording has got through transcription. Stage 5 drives the rest.</summary>
public enum TranscriptState
{
    None = 0,
    Pending = 1,
    Transcribing = 2,
    AwaitingApproval = 3,
    Approved = 4,
}
```

Append to `Family.cs`:

```csharp
/// <summary>
/// A recipe kept for a family rather than the public archive. Its visibility is decided
/// by Privacy plus the member list, and nothing reads one except through
/// FamilyAccessService.
/// </summary>
public class FamilyRecipe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string OwnerId { get; set; }

    public required string LocalName { get; set; }
    public string Description { get; set; } = "";
    public string Province { get; set; } = "";
    public string Language { get; set; } = "";
    public string TaughtBy { get; set; } = "";
    public string TaughtByOrigin { get; set; } = "";
    public string Story { get; set; } = "";
    public string TraditionalMethod { get; set; } = "";

    public PrivacyLevel Privacy { get; set; } = PrivacyLevel.PrivateToMe;
    public TranscriptState Transcript { get; set; } = TranscriptState.None;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the family agreed to publish it and a reviewer did.</summary>
    public string? PublishedDishId { get; set; }

    public List<FamilyMember> Members { get; set; } = [];
    public List<FamilyNote> Notes { get; set; } = [];
    public List<MediaAsset> Media { get; set; } = [];
}

public class FamilyMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FamilyRecipeId { get; set; }

    /// <summary>Null until the invite is accepted on that person's own device.</summary>
    public string? UserId { get; set; }

    public required string DisplayName { get; set; }
    public string Relation { get; set; } = "";
    public MemberState State { get; set; }
    public DateTimeOffset InvitedAt { get; set; }
    public DateTimeOffset? JoinedAt { get; set; }
}

public class FamilyNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FamilyRecipeId { get; set; }
    public required string AuthorUserId { get; set; }
    public required string AuthorName { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// A short code the owner passes on however they like - there is no email on an
/// anonymous account. Whoever enters it on their own device becomes that member.
/// </summary>
public class FamilyInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FamilyRecipeId { get; set; }
    public Guid MemberId { get; set; }
    public required string Code { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RedeemedAt { get; set; }

    public bool IsOpen(DateTimeOffset now) => RedeemedAt is null && ExpiresAt > now;
}
```

- [ ] **Step 4: The access service and repository**

`FamilyAccessService.cs` — replace with:

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Services;

public interface IFamilyAccessService
{
    /// <summary>
    /// The rule, in one place: a family recipe is readable by its owner, by members who
    /// have joined, and by everyone once it is public.
    /// </summary>
    IQueryable<FamilyRecipe> VisibleTo(IQueryable<FamilyRecipe> source, string userId);

    Task<bool> CanReadAsync(FamilyRecipe recipe, string userId, CancellationToken ct);

    /// <summary>Only the owner changes a family recipe. Members add notes; they do not edit.</summary>
    Task<bool> CanEditAsync(FamilyRecipe recipe, string userId, CancellationToken ct);

    Task<bool> CanReadMediaAsync(MediaAsset asset, string userId, CancellationToken ct);
}

public sealed class FamilyAccessService(TasteZambiaDbContext db) : IFamilyAccessService
{
    public IQueryable<FamilyRecipe> VisibleTo(IQueryable<FamilyRecipe> source, string userId)
        => source.Where(r =>
            r.OwnerId == userId
            || r.Privacy == PrivacyLevel.PublicInArchive
            || r.Members.Any(m => m.UserId == userId && m.State == MemberState.Joined));

    public Task<bool> CanReadAsync(FamilyRecipe recipe, string userId, CancellationToken ct)
        => Task.FromResult(
            recipe.OwnerId == userId
            || recipe.Privacy == PrivacyLevel.PublicInArchive
            || recipe.Members.Any(m => m.UserId == userId && m.State == MemberState.Joined));

    public Task<bool> CanEditAsync(FamilyRecipe recipe, string userId, CancellationToken ct)
        => Task.FromResult(recipe.OwnerId == userId);

    public async Task<bool> CanReadMediaAsync(MediaAsset asset, string userId, CancellationToken ct)
    {
        if (asset.UserId == userId) return true;

        // A photo attached to a family recipe is readable by whoever may read the recipe.
        if (asset.FamilyRecipeId is not { } recipeId) return false;

        return await VisibleTo(db.Set<FamilyRecipe>().Include(r => r.Members), userId)
            .AnyAsync(r => r.Id == recipeId, ct);
    }
}
```

`TasteZambia.API/Repositories/FamilyRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Services;

namespace TasteZambia.API.Repositories;

public interface IFamilyRepository
{
    /// <summary>The recipe, or null when it does not exist or is not visible to this user.</summary>
    Task<FamilyRecipe?> GetAsync(Guid id, string userId, CancellationToken ct = default);

    /// <summary>The owner's own, and any they have joined.</summary>
    Task<IReadOnlyList<FamilyRecipe>> ListForUserAsync(string userId, CancellationToken ct = default);

    Task<FamilyInvite?> FindByInviteCodeAsync(string code, CancellationToken ct = default);
    void Add(FamilyRecipe recipe);
    void AddInvite(FamilyInvite invite);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class FamilyRepository(TasteZambiaDbContext db, IFamilyAccessService access) : IFamilyRepository
{
    private IQueryable<FamilyRecipe> Graph => db.Set<FamilyRecipe>()
        .Include(r => r.Members.OrderBy(m => m.InvitedAt))
        .Include(r => r.Notes.OrderByDescending(n => n.CreatedAt))
        .Include(r => r.Media.OrderBy(m => m.CreatedAt));

    public Task<FamilyRecipe?> GetAsync(Guid id, string userId, CancellationToken ct = default)
        => access.VisibleTo(Graph, userId).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<FamilyRecipe>> ListForUserAsync(string userId, CancellationToken ct = default)
        => await access.VisibleTo(Graph.AsNoTracking(), userId)
            // A stranger's public recipe is not "theirs"; the shelf shows what they keep.
            .Where(r => r.OwnerId == userId || r.Members.Any(m => m.UserId == userId))
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(ct);

    public Task<FamilyInvite?> FindByInviteCodeAsync(string code, CancellationToken ct = default)
        => db.Set<FamilyInvite>().FirstOrDefaultAsync(i => i.Code == code, ct);

    public void Add(FamilyRecipe recipe) => db.Set<FamilyRecipe>().Add(recipe);
    public void AddInvite(FamilyInvite invite) => db.Set<FamilyInvite>().Add(invite);
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
```

Configurations in `FamilyConfigurations.cs`: `family_recipes` (key `Id`, index `(OwnerId, UpdatedAt)`, `LocalName` 120, `Description` 400, `Province` 40, enums `HasConversion<int>()`, `UseXminConcurrency()`), `family_members` (index `(FamilyRecipeId, UserId)`, `DisplayName` 120), `family_notes` (`Body` 2000), `family_invites` (unique index on `Code`, `Code` 8). Cascade from the recipe to all four. Migration: `dotnet ef migrations add FamilyArchive --project TasteZambia.API`.

- [ ] **Step 5: Run the tests**

Run: `dotnet test TasteZambia.API.Tests`
Expected: PASS — 9 new, and the media tests still green.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(api): family recipe aggregate and its single access rule

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 4: The family endpoints

**Files:**
- Create: `TasteZambia.Shared/Contracts/Family/FamilyContracts.cs`, `TasteZambia.API/Controllers/FamilyController.cs`, `TasteZambia.API/Mapping/FamilyMappings.cs`, `TasteZambia.API/Services/FamilyService.cs`
- Modify: `TasteZambia.Shared/Routes/ApiRoutes.cs`, `Program.cs`
- Test: `TasteZambia.API.Tests/Controllers/FamilyEndpointTests.cs`

**Interfaces:**
- Produces (contracts):
  - `CreateFamilyRecipeRequest(string LocalName, string Description, string Province, string Language, string TaughtBy, string TaughtByOrigin, string Story, string TraditionalMethod, PrivacyLevel Privacy)` — `LocalName` `[Required, MaxLength(120)]`
  - `UpdateFamilyRecipeRequest` — the same fields, all replaced
  - `FamilyMemberDto(Guid Id, string DisplayName, string Relation, MemberState State)`
  - `FamilyNoteDto(Guid Id, string AuthorName, string Body, DateTimeOffset CreatedAt)`
  - `InviteDto(Guid MemberId, string Code, DateTimeOffset ExpiresAt)`
  - `AcceptInviteRequest(string Code)`
  - `AddNoteRequest(string Body)` — `[Required, MaxLength(2000)]`
  - `SetPrivacyRequest(PrivacyLevel Privacy)`
  - `AddMemberRequest(string DisplayName, string Relation)`
  - `FamilyRecipeSummaryDto(Guid Id, string LocalName, string TaughtBy, PrivacyLevel Privacy, int PhotoCount, int NoteCount, bool HasAudio, int PercentComplete, DateTimeOffset UpdatedAt)`
  - `FamilyRecipeDto(… all fields …, IReadOnlyList<FamilyMemberDto> Members, IReadOnlyList<FamilyNoteDto> Notes, IReadOnlyList<MediaAssetDto> Media, int PercentComplete, bool IsOwner)`
- Produces (service): `IFamilyService` with `CreateAsync`, `UpdateAsync`, `InviteAsync` (owner only, mints a code), `AcceptInviteAsync`, `RemoveMemberAsync` (owner only), `AddNoteAsync` (any member who can read), `SetPrivacyAsync` (owner only), `AttachMediaAsync(Guid recipeId, Guid mediaId, string userId, ct)`
- Routes: `Family.Collection = "/api/v1/me/family-recipes"`, `Family.ById`, `Family.Members`, `Family.MemberById`, `Family.Invites`, `Family.Accept = "/api/v1/family-recipes/accept"`, `Family.Notes`, `Family.Privacy`, `Family.Media`
- `PercentComplete`: the design's draft checklist as four equal parts — name+province, taught-by+story, method, privacy chosen (anything other than the default `PrivateToMe` counts as chosen). 25 points each.
- Every write by a non-owner where the owner is required → **404**, not 403.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Controllers;

[Collection(nameof(DatabaseCollection))]
public class FamilyEndpointTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _owner = null!;
    private HttpClient _relative = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        (_owner, _) = await _factory.SignedInClientAsync();
        (_relative, _) = await _factory.SignedInClientAsync();
    }

    public Task DisposeAsync() { _owner.Dispose(); _relative.Dispose(); _factory.Dispose(); return Task.CompletedTask; }

    private static string Route(string template, Guid id) => template.Replace("{id}", id.ToString());

    private static CreateFamilyRecipeRequest Ifisashi() => new(
        "Ifisashi ya Banakulu", "Pumpkin leaves in groundnuts, the way my grandmother made it",
        "Northern", "Bemba", "Banakulu Mwaba, my father's mother",
        "Mungwi, outside Kasama. She was taught by her own mother.",
        "She cooked this every time we arrived from Kitwe, before we had even put our bags down.",
        "Clay pot on the mbaula. Groundnuts pounded, never blended.",
        PrivacyLevel.SharedWithFamily);

    private async Task<FamilyRecipeDto> PreserveAsync()
        => (await (await _owner.PostAsJsonAsync(ApiRoutes.Family.Collection, Ifisashi()))
            .Content.ReadFromJsonAsync<FamilyRecipeDto>())!;

    [Fact]
    public async Task Preserving_ReturnsTheRecipeWithTheOwnerAsTheOnlyMember()
    {
        var recipe = await PreserveAsync();

        Assert.Equal("Ifisashi ya Banakulu", recipe.LocalName);
        Assert.True(recipe.IsOwner);
        Assert.Equal(PrivacyLevel.SharedWithFamily, recipe.Privacy);
        Assert.Equal(100, recipe.PercentComplete);   // name, story, method and privacy all present
        Assert.Empty(recipe.Media);
    }

    [Fact]
    public async Task ADraftMissingItsStoryAndMethod_ReportsPartialCompletion()
    {
        var sparse = Ifisashi() with { Story = "", TraditionalMethod = "", Privacy = PrivacyLevel.PrivateToMe };
        var recipe = await (await _owner.PostAsJsonAsync(ApiRoutes.Family.Collection, sparse))
            .Content.ReadFromJsonAsync<FamilyRecipeDto>();

        Assert.Equal(50, recipe!.PercentComplete);   // name+province and taught-by only
    }

    [Fact]
    public async Task AStranger_CannotSeeIt()
    {
        var recipe = await PreserveAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await _relative.GetAsync(Route(ApiRoutes.Family.ById, recipe.Id))).StatusCode);
    }

    [Fact]
    public async Task Inviting_ThenAccepting_LetsARelativeReadItAndAddANote()
    {
        var recipe = await PreserveAsync();

        var invite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipe.Id),
            new AddMemberRequest("Mutinta Mwaba", "Sister, Lusaka"))).Content.ReadFromJsonAsync<InviteDto>();
        Assert.Equal(8, invite!.Code.Length);

        var accepted = await _relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite.Code));
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        var seen = await _relative.GetFromJsonAsync<FamilyRecipeDto>(Route(ApiRoutes.Family.ById, recipe.Id));
        Assert.False(seen!.IsOwner);
        Assert.Contains(seen.Members, m => m.DisplayName == "Mutinta Mwaba" && m.State == MemberState.Joined);

        var noted = await _relative.PostAsJsonAsync(Route(ApiRoutes.Family.Notes, recipe.Id),
            new AddNoteRequest("She never used tomato in this."));
        Assert.Equal(HttpStatusCode.Created, noted.StatusCode);

        var withNote = await _owner.GetFromJsonAsync<FamilyRecipeDto>(Route(ApiRoutes.Family.ById, recipe.Id));
        Assert.Single(withNote!.Notes, n => n.Body.StartsWith("She never used tomato"));
    }

    [Fact]
    public async Task AnInviteCode_CannotBeRedeemedTwice()
    {
        var recipe = await PreserveAsync();
        var invite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipe.Id),
            new AddMemberRequest("Mutinta", "Sister"))).Content.ReadFromJsonAsync<InviteDto>();

        await _relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite!.Code));

        var (third, _) = await _factory.SignedInClientAsync();
        using (third)
            Assert.Equal(HttpStatusCode.Conflict,
                (await third.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite.Code))).StatusCode);
    }

    [Fact]
    public async Task ARemovedMember_NoLongerSeesIt()
    {
        var recipe = await PreserveAsync();
        var invite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipe.Id),
            new AddMemberRequest("Mutinta", "Sister"))).Content.ReadFromJsonAsync<InviteDto>();
        await _relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite!.Code));
        Assert.Equal(HttpStatusCode.OK, (await _relative.GetAsync(Route(ApiRoutes.Family.ById, recipe.Id))).StatusCode);

        var removed = await _owner.DeleteAsync(ApiRoutes.Family.MemberById
            .Replace("{id}", recipe.Id.ToString()).Replace("{memberId}", invite.MemberId.ToString()));
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _relative.GetAsync(Route(ApiRoutes.Family.ById, recipe.Id))).StatusCode);
    }

    [Fact]
    public async Task OnlyTheOwner_ChangesPrivacy()
    {
        var recipe = await PreserveAsync();
        var invite = await (await _owner.PostAsJsonAsync(Route(ApiRoutes.Family.Members, recipe.Id),
            new AddMemberRequest("Mutinta", "Sister"))).Content.ReadFromJsonAsync<InviteDto>();
        await _relative.PostAsJsonAsync(ApiRoutes.Family.Accept, new AcceptInviteRequest(invite!.Code));

        var byMember = await _relative.PutAsJsonAsync(Route(ApiRoutes.Family.Privacy, recipe.Id),
            new SetPrivacyRequest(PrivacyLevel.PublicInArchive));
        Assert.Equal(HttpStatusCode.NotFound, byMember.StatusCode);   // not 403: do not confirm it exists to edit

        var byOwner = await _owner.PutAsJsonAsync(Route(ApiRoutes.Family.Privacy, recipe.Id),
            new SetPrivacyRequest(PrivacyLevel.PublicInArchive));
        Assert.Equal(HttpStatusCode.NoContent, byOwner.StatusCode);
    }

    [Fact]
    public async Task MyShelf_ListsWhatIKeepAndWhatIWasInvitedInto_ButNotStrangersPublicOnes()
    {
        var mine = await PreserveAsync();
        await _owner.PutAsJsonAsync(Route(ApiRoutes.Family.Privacy, mine.Id), new SetPrivacyRequest(PrivacyLevel.PublicInArchive));

        var shelf = await _relative.GetFromJsonAsync<List<FamilyRecipeSummaryDto>>(ApiRoutes.Family.Collection);
        Assert.Empty(shelf!);   // public, but not theirs to keep
    }
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter FamilyEndpointTests`
Expected: FAIL.

- [ ] **Step 3: Contracts and routes**

`TasteZambia.Shared/Contracts/Family/FamilyContracts.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using TasteZambia.Shared.Contracts.Media;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Shared.Contracts.Family;

public sealed record CreateFamilyRecipeRequest(
    [Required, MaxLength(120)] string LocalName,
    [MaxLength(400)] string Description,
    [MaxLength(40)] string Province,
    [MaxLength(40)] string Language,
    [MaxLength(200)] string TaughtBy,
    [MaxLength(200)] string TaughtByOrigin,
    string Story,
    string TraditionalMethod,
    PrivacyLevel Privacy);

public sealed record UpdateFamilyRecipeRequest(
    [Required, MaxLength(120)] string LocalName,
    [MaxLength(400)] string Description,
    [MaxLength(40)] string Province,
    [MaxLength(40)] string Language,
    [MaxLength(200)] string TaughtBy,
    [MaxLength(200)] string TaughtByOrigin,
    string Story,
    string TraditionalMethod);

public sealed record AddMemberRequest([Required, MaxLength(120)] string DisplayName, [MaxLength(120)] string Relation);
public sealed record AcceptInviteRequest([Required, MinLength(8), MaxLength(8)] string Code);
public sealed record AddNoteRequest([Required, MaxLength(2000)] string Body);
public sealed record SetPrivacyRequest(PrivacyLevel Privacy);

public sealed record FamilyMemberDto(Guid Id, string DisplayName, string Relation, MemberState State);
public sealed record FamilyNoteDto(Guid Id, string AuthorName, string Body, DateTimeOffset CreatedAt);
public sealed record InviteDto(Guid MemberId, string Code, DateTimeOffset ExpiresAt);

public sealed record FamilyRecipeSummaryDto(
    Guid Id, string LocalName, string TaughtBy, PrivacyLevel Privacy,
    int PhotoCount, int NoteCount, bool HasAudio, int PercentComplete, DateTimeOffset UpdatedAt);

public sealed record FamilyRecipeDto(
    Guid Id, string LocalName, string Description, string Province, string Language,
    string TaughtBy, string TaughtByOrigin, string Story, string TraditionalMethod,
    PrivacyLevel Privacy, TranscriptState Transcript, string? PublishedDishId,
    int PercentComplete, bool IsOwner, DateTimeOffset UpdatedAt,
    IReadOnlyList<FamilyMemberDto> Members,
    IReadOnlyList<FamilyNoteDto> Notes,
    IReadOnlyList<MediaAssetDto> Media);
```

`ApiRoutes` — add:

```csharp
/// <summary>The private family tier. Every route needs a bearer token.</summary>
public static class Family
{
    public const string Collection = $"{Root}/me/family-recipes";
    public const string ById = $"{Collection}/{{id}}";
    public const string Members = $"{ById}/members";
    public const string MemberById = $"{Members}/{{memberId}}";
    public const string Notes = $"{ById}/notes";
    public const string Privacy = $"{ById}/privacy";
    public const string Media = $"{ById}/media/{{mediaId}}";

    /// <summary>Redeeming an invite code is not scoped to a recipe the caller cannot see yet.</summary>
    public const string Accept = $"{Root}/family-recipes/accept";
}
```

- [ ] **Step 4: Service, mappings, controller**

`TasteZambia.API/Services/FamilyService.cs` — the write side. Key points, with the code:

```csharp
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TasteZambia.API.Data;
using TasteZambia.API.Data.Entities;
using TasteZambia.API.Repositories;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Enums;

namespace TasteZambia.API.Services;

public interface IFamilyService
{
    Task<FamilyRecipe> CreateAsync(string userId, CreateFamilyRecipeRequest request, CancellationToken ct);
    Task<FamilyRecipe?> UpdateAsync(Guid id, string userId, UpdateFamilyRecipeRequest request, CancellationToken ct);
    Task<FamilyInvite?> InviteAsync(Guid id, string userId, AddMemberRequest request, CancellationToken ct);

    /// <summary>Returns the recipe joined, null when the code is unknown, or throws when it is already used.</summary>
    Task<FamilyRecipe?> AcceptInviteAsync(string code, string userId, string displayName, CancellationToken ct);

    Task<bool> RemoveMemberAsync(Guid id, Guid memberId, string userId, CancellationToken ct);
    Task<FamilyNote?> AddNoteAsync(Guid id, string userId, string authorName, string body, CancellationToken ct);
    Task<bool> SetPrivacyAsync(Guid id, string userId, PrivacyLevel privacy, CancellationToken ct);
    Task<bool> AttachMediaAsync(Guid id, Guid mediaId, string userId, CancellationToken ct);

    static int PercentComplete(FamilyRecipe r)
    {
        // The design's draft checklist, four equal parts.
        var done = 0;
        if (r.LocalName.Length > 0 && r.Province.Length > 0) done++;
        if (r.TaughtBy.Length > 0) done++;
        if (r.Story.Length > 0 || r.TraditionalMethod.Length > 0) done++;
        if (r.Privacy != PrivacyLevel.PrivateToMe) done++;
        return done * 25;
    }
}

public sealed class FamilyService(
    TasteZambiaDbContext db,
    IFamilyRepository family,
    IFamilyAccessService access,
    IUserProfileRepository profiles,
    TimeProvider clock) : IFamilyService
{
    /// <summary>No 0/O/1/I/l: these codes get read aloud and written on paper.</summary>
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public async Task<FamilyRecipe> CreateAsync(string userId, CreateFamilyRecipeRequest r, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var profile = await profiles.GetOrCreateProfileAsync(userId, ct);

        var recipe = new FamilyRecipe
        {
            OwnerId = userId,
            LocalName = r.LocalName.Trim(),
            Description = r.Description,
            Province = r.Province,
            Language = r.Language,
            TaughtBy = r.TaughtBy,
            TaughtByOrigin = r.TaughtByOrigin,
            Story = r.Story,
            TraditionalMethod = r.TraditionalMethod,
            Privacy = r.Privacy,
            CreatedAt = now,
            UpdatedAt = now,
            Members =
            {
                new FamilyMember
                {
                    UserId = userId,
                    DisplayName = profile.DisplayName.Length > 0 ? profile.DisplayName : "You",
                    Relation = "Owner",
                    State = MemberState.Joined,
                    InvitedAt = now,
                    JoinedAt = now,
                },
            },
        };

        family.Add(recipe);
        await family.SaveChangesAsync(ct);
        return recipe;
    }

    public async Task<FamilyRecipe?> UpdateAsync(Guid id, string userId, UpdateFamilyRecipeRequest r, CancellationToken ct)
    {
        var recipe = await OwnedAsync(id, userId, ct);
        if (recipe is null) return null;

        recipe.LocalName = r.LocalName.Trim();
        recipe.Description = r.Description;
        recipe.Province = r.Province;
        recipe.Language = r.Language;
        recipe.TaughtBy = r.TaughtBy;
        recipe.TaughtByOrigin = r.TaughtByOrigin;
        recipe.Story = r.Story;
        recipe.TraditionalMethod = r.TraditionalMethod;
        recipe.UpdatedAt = clock.GetUtcNow();
        await family.SaveChangesAsync(ct);
        return recipe;
    }

    public async Task<FamilyInvite?> InviteAsync(Guid id, string userId, AddMemberRequest request, CancellationToken ct)
    {
        var recipe = await OwnedAsync(id, userId, ct);
        if (recipe is null) return null;

        var now = clock.GetUtcNow();
        var member = new FamilyMember
        {
            DisplayName = request.DisplayName.Trim(),
            Relation = request.Relation,
            State = MemberState.Invited,
            InvitedAt = now,
        };
        recipe.Members.Add(member);
        recipe.UpdatedAt = now;

        var invite = new FamilyInvite
        {
            FamilyRecipeId = recipe.Id,
            MemberId = member.Id,
            Code = NewCode(),
            ExpiresAt = now.AddDays(30),
        };
        family.AddInvite(invite);
        await family.SaveChangesAsync(ct);
        return invite;
    }

    public async Task<FamilyRecipe?> AcceptInviteAsync(string code, string userId, string displayName, CancellationToken ct)
    {
        var invite = await family.FindByInviteCodeAsync(code.ToUpperInvariant(), ct);
        if (invite is null) return null;

        var now = clock.GetUtcNow();
        if (!invite.IsOpen(now))
            throw new InvalidOperationException("That invite has already been used or has expired.");

        var member = await db.Set<FamilyMember>().FirstOrDefaultAsync(m => m.Id == invite.MemberId, ct);
        if (member is null) return null;

        member.UserId = userId;
        member.State = MemberState.Joined;
        member.JoinedAt = now;
        if (displayName.Length > 0) member.DisplayName = displayName;
        invite.RedeemedAt = now;

        await family.SaveChangesAsync(ct);
        return await family.GetAsync(invite.FamilyRecipeId, userId, ct);
    }

    public async Task<bool> RemoveMemberAsync(Guid id, Guid memberId, string userId, CancellationToken ct)
    {
        var recipe = await OwnedAsync(id, userId, ct);
        var member = recipe?.Members.FirstOrDefault(m => m.Id == memberId);
        if (recipe is null || member is null || member.UserId == recipe.OwnerId) return false;

        // Soft: the note they left stays, and their name with it.
        member.State = MemberState.Removed;
        recipe.UpdatedAt = clock.GetUtcNow();
        await family.SaveChangesAsync(ct);
        return true;
    }

    public async Task<FamilyNote?> AddNoteAsync(Guid id, string userId, string authorName, string body, CancellationToken ct)
    {
        var recipe = await family.GetAsync(id, userId, ct);
        if (recipe is null) return null;

        var note = new FamilyNote
        {
            FamilyRecipeId = recipe.Id,
            AuthorUserId = userId,
            AuthorName = authorName,
            Body = body.Trim(),
            CreatedAt = clock.GetUtcNow(),
        };
        recipe.Notes.Add(note);
        recipe.UpdatedAt = note.CreatedAt;
        await family.SaveChangesAsync(ct);
        return note;
    }

    public async Task<bool> SetPrivacyAsync(Guid id, string userId, PrivacyLevel privacy, CancellationToken ct)
    {
        var recipe = await OwnedAsync(id, userId, ct);
        if (recipe is null) return false;

        recipe.Privacy = privacy;
        recipe.UpdatedAt = clock.GetUtcNow();
        await family.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AttachMediaAsync(Guid id, Guid mediaId, string userId, CancellationToken ct)
    {
        var recipe = await family.GetAsync(id, userId, ct);
        if (recipe is null) return false;

        // Members contribute photographs; only your own upload can be attached.
        var asset = await db.MediaAssets.FirstOrDefaultAsync(a => a.Id == mediaId && a.UserId == userId, ct);
        if (asset is null) return false;

        asset.FamilyRecipeId = recipe.Id;
        recipe.UpdatedAt = clock.GetUtcNow();
        await family.SaveChangesAsync(ct);
        return true;
    }

    private async Task<FamilyRecipe?> OwnedAsync(Guid id, string userId, CancellationToken ct)
    {
        var recipe = await family.GetAsync(id, userId, ct);
        return recipe is not null && await access.CanEditAsync(recipe, userId, ct) ? recipe : null;
    }

    private static string NewCode()
        => new(RandomNumberGenerator.GetItems<char>(CodeAlphabet, 8));
}
```

`FamilyMappings.cs` maps entity → DTO, with `IsOwner` computed against the caller and `PercentComplete` from `IFamilyService.PercentComplete`. `FamilyController` is `[ApiController] [Authorize]`, reads `ICurrentUser.UserId`, returns `NotFoundProblem` wherever the service returns null, `Conflict` for the reused-invite `InvalidOperationException`, and `Created` for a note. Register `IFamilyRepository`, `IFamilyService` as scoped.

- [ ] **Step 5: Run the whole API suite**

Run: `dotnet test TasteZambia.API.Tests`
Expected: PASS — 8 new, everything prior green.

- [ ] **Step 6: Verify the loop by hand**

With the API running, in Scalar: sign in as two device accounts, preserve a recipe on the first, invite, redeem the code on the second, add a note, remove the member, confirm the 404.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat(api): family recipes, invites by code, notes and privacy

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 5: Mobile — photos in both wizards

**Files:**
- Create: `TasteZambia.Core/Services/IPhotoPicker.cs`, `TasteZambia.Core/Services/MediaUploader.cs`, `TasteZambia.Mobile/Services/MauiPhotoPicker.cs`
- Modify: `TasteZambia.Core/Models/ContributionDraft.cs`, `TasteZambia.Core/ViewModels/ShareViewModel.cs`, `TasteZambia.Mobile/Views/Share/ShareView.xaml`, `MauiProgram.cs`, `Platforms/Android/AndroidManifest.xml`
- Test: `TasteZambia.Core.Tests/Services/MediaUploaderTests.cs`

**Interfaces:**
- Produces:
  - `PickedFile(string FileName, string ContentType, Func<CancellationToken, Task<Stream>> Open)`
  - `IPhotoPicker`: `Task<PickedFile?> CapturePhotoAsync(CancellationToken ct)`, `Task<PickedFile?> PickPhotoAsync(CancellationToken ct)` — null when the reader cancels
  - `PendingUpload(Guid LocalId, string ContentType, MediaKind Kind, string CachePath)`
  - `IMediaUploader`:
    - `Task<Guid?> UploadAsync(PickedFile file, MediaKind kind, CancellationToken ct)` — the server id, or null when it could not be sent (and was queued)
    - `Task<int> DrainAsync(CancellationToken ct)` — retries the queue, returns how many went
    - `IReadOnlyList<PendingUpload> Pending { get; }`
  - `ContributionDraft.PhotoIds` — `List<Guid>` replacing `PhotoPaths`, plus `List<string> PendingPhotoPaths` for files not yet uploaded
- Consumes: `ApiRoutes.Media.Collection`, `UploadResultDto`, `ILocalStore`

**Android manifest:** `CAMERA` and, for API ≤ 32, `READ_EXTERNAL_STORAGE`; MAUI's `MediaPicker` handles the photo-picker permission on 33+.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using Microsoft.Extensions.Time.Testing;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Enums;

namespace TasteZambia.Core.Tests.Services;

public class MediaUploaderTests
{
    private static PickedFile Photo(string name = "kitchen.jpg")
        => new(name, "image/jpeg", _ => Task.FromResult<Stream>(new MemoryStream([1, 2, 3, 4])));

    private static (MediaUploader uploader, ScriptedHandler server, InMemoryLocalStore store) Sut()
    {
        var server = new ScriptedHandler();
        var store = new InMemoryLocalStore();
        var client = new HttpClient(server) { BaseAddress = new Uri("http://archive.test") };
        return (new MediaUploader(client, store, new FakeTimeProvider()), server, store);
    }

    [Fact]
    public async Task ASuccessfulUpload_ReturnsTheServerIdAndQueuesNothing()
    {
        var (uploader, server, _) = Sut();
        var id = Guid.NewGuid();
        server.NextId = id;

        Assert.Equal(id, await uploader.UploadAsync(Photo(), MediaKind.Photo, default));
        Assert.Empty(uploader.Pending);
    }

    [Fact]
    public async Task AnOfflineUpload_KeepsThePhotoAndQueuesIt()
    {
        var (uploader, server, _) = Sut();
        server.IsOffline = true;

        Assert.Null(await uploader.UploadAsync(Photo(), MediaKind.Photo, default));

        var pending = Assert.Single(uploader.Pending);
        Assert.Equal("image/jpeg", pending.ContentType);
        Assert.True(File.Exists(pending.CachePath));   // the bytes the reader chose are still here
    }

    [Fact]
    public async Task Draining_AfterTheArchiveComesBack_SendsTheQueueInOrder()
    {
        var (uploader, server, _) = Sut();
        server.IsOffline = true;
        await uploader.UploadAsync(Photo("first.jpg"), MediaKind.Photo, default);
        await uploader.UploadAsync(Photo("second.jpg"), MediaKind.Photo, default);
        Assert.Equal(2, uploader.Pending.Count);

        server.IsOffline = false;
        Assert.Equal(2, await uploader.DrainAsync(default));

        Assert.Empty(uploader.Pending);
        Assert.Equal(2, server.Uploads);
    }

    [Fact]
    public async Task AQueuedUpload_SurvivesARestart()
    {
        var (uploader, server, store) = Sut();
        server.IsOffline = true;
        await uploader.UploadAsync(Photo(), MediaKind.Photo, default);

        var again = new MediaUploader(new HttpClient(server) { BaseAddress = new Uri("http://archive.test") }, store, new FakeTimeProvider());

        Assert.Single(again.Pending);
    }

    [Fact]
    public async Task ARejectedUpload_IsDroppedRatherThanRetriedForever()
    {
        var (uploader, server, _) = Sut();
        server.Status = HttpStatusCode.UnsupportedMediaType;

        Assert.Null(await uploader.UploadAsync(Photo("notes.pdf"), MediaKind.Photo, default));
        Assert.Empty(uploader.Pending);   // the archive will never take it; queueing is a lie
    }

    /// <summary>Answers uploads, or refuses to connect at all.</summary>
    private sealed class ScriptedHandler : HttpMessageHandler
    {
        public bool IsOffline { get; set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.Created;
        public Guid NextId { get; set; } = Guid.NewGuid();
        public int Uploads { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (IsOffline) throw new HttpRequestException("offline");

            Uploads++;
            var response = new HttpResponseMessage(Status);
            if (Status == HttpStatusCode.Created)
                response.Content = System.Net.Http.Json.JsonContent.Create(
                    new TasteZambia.Shared.Contracts.Media.UploadResultDto(NextId, $"/api/v1/media/{NextId}"));
            return Task.FromResult(response);
        }
    }
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter MediaUploaderTests`
Expected: FAIL.

- [ ] **Step 3: The picker interface and the uploader**

`TasteZambia.Core/Services/IPhotoPicker.cs`:

```csharp
namespace TasteZambia.Core.Services;

/// <summary>A file the reader chose, opened lazily so a large photo is never held in memory twice.</summary>
public sealed record PickedFile(string FileName, string ContentType, Func<CancellationToken, Task<Stream>> Open);

public interface IPhotoPicker
{
    /// <summary>Null when the reader backed out, which is not a failure.</summary>
    Task<PickedFile?> CapturePhotoAsync(CancellationToken ct = default);
    Task<PickedFile?> PickPhotoAsync(CancellationToken ct = default);
}
```

`TasteZambia.Core/Services/MediaUploader.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Media;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Services;

public sealed record PendingUpload(Guid LocalId, string ContentType, MediaKind Kind, string CachePath);

public interface IMediaUploader
{
    /// <summary>The archive's id for the file, or null when it could not be sent right now.</summary>
    Task<Guid?> UploadAsync(PickedFile file, MediaKind kind, CancellationToken ct = default);

    /// <summary>Retries everything queued. Returns how many went through.</summary>
    Task<int> DrainAsync(CancellationToken ct = default);

    IReadOnlyList<PendingUpload> Pending { get; }
}

/// <summary>
/// Uploads a photograph or a recording, and keeps it when the archive cannot be reached.
/// A reader who has just taken a picture of their grandmother's cooking should not lose
/// it because the signal dropped.
/// </summary>
public sealed class MediaUploader(HttpClient api, ILocalStore local, TimeProvider clock) : IMediaUploader
{
    private const string Key = "media.pending";
    private readonly List<PendingUpload> _pending = local.Get<List<PendingUpload>>(Key) ?? [];

    public IReadOnlyList<PendingUpload> Pending => _pending.ToList();

    public async Task<Guid?> UploadAsync(PickedFile file, MediaKind kind, CancellationToken ct = default)
    {
        await using var content = await file.Open(ct);
        var bytes = new MemoryStream();
        await content.CopyToAsync(bytes, ct);
        bytes.Position = 0;

        var sent = await SendAsync(bytes, file.ContentType, kind, ct);
        if (sent is { } id) return id;
        if (sent is null && _lastWasRefused) return null;   // the archive will never take it

        Queue(bytes.ToArray(), file.ContentType, kind);
        return null;
    }

    public async Task<int> DrainAsync(CancellationToken ct = default)
    {
        var sent = 0;
        foreach (var item in _pending.ToList())
        {
            if (!File.Exists(item.CachePath)) { Forget(item); continue; }

            await using var stream = File.OpenRead(item.CachePath);
            if (await SendAsync(stream, item.ContentType, item.Kind, ct) is null && !_lastWasRefused)
                break;   // still offline: keep the rest queued, in order

            Forget(item);
            sent++;
        }
        return sent;
    }

    private bool _lastWasRefused;

    private async Task<Guid?> SendAsync(Stream content, string contentType, MediaKind kind, CancellationToken ct)
    {
        _lastWasRefused = false;
        try
        {
            var file = new StreamContent(content);
            file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

            using var form = new MultipartFormDataContent
            {
                { file, "file", "upload" },
                { new StringContent(kind.ToString()), "kind" },
            };

            using var response = await api.PostAsync(ApiRoutes.Media.Collection, form, ct);
            if (response.StatusCode is HttpStatusCode.UnsupportedMediaType or HttpStatusCode.RequestEntityTooLarge)
            {
                // Retrying will not change the answer.
                _lastWasRefused = true;
                return null;
            }
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<UploadResultDto>(ct))?.Id;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    private void Queue(byte[] bytes, string contentType, MediaKind kind)
    {
        var localId = Guid.NewGuid();
        var path = Path.Combine(Path.GetTempPath(), $"tz-upload-{localId:N}");
        File.WriteAllBytes(path, bytes);

        _pending.Add(new PendingUpload(localId, contentType, kind, path));
        local.Set(Key, _pending);
    }

    private void Forget(PendingUpload item)
    {
        _pending.Remove(item);
        local.Set(Key, _pending);
        if (File.Exists(item.CachePath)) File.Delete(item.CachePath);
    }
}
```

`TasteZambia.Mobile/Services/MauiPhotoPicker.cs` wraps `MediaPicker.Default.CapturePhotoAsync()` / `PickPhotoAsync()`, mapping `FileResult.ContentType` and `OpenReadAsync`, returning null on `null` or `PermissionException`.

- [ ] **Step 4: Wire the photo strip into the Share wizard**

`ContributionDraft`: replace `List<string> PhotoPaths` with `List<Guid> PhotoIds` and add `List<string> PendingPhotoPaths`. `ShareViewModel` gains `ObservableCollection<DraftPhotoViewModel> Photos`, `AddPhotoCommand` (capture), `ChoosePhotoCommand` (library) and `RemovePhotoCommand`; each upload runs through `IMediaUploader` and stores either the returned id or the queued path. Step 1's dashed rectangle becomes a horizontal strip of thumbnails plus the add tile, and its caption changes from "photos come with the next update" to "add photo".

- [ ] **Step 5: Run the tests and build**

Run: `dotnet test TasteZambia.Core.Tests` → PASS.
Run: `dotnet build TasteZambia.Mobile -f net10.0-android` → no warnings.

- [ ] **Step 6: Verify on device and commit**

Deploy. Share a recipe with two photographs; check `media_assets` has two rows and the files are under `media-dev/`. Turn the API off, add a third photo — it stays in the strip; turn the API on, pull to refresh — it uploads.

```bash
git add -A && git commit -m "feat(mobile): photographs in the Share wizard, kept when offline

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 6: Mobile — the family archive, for real

**Files:**
- Create: `TasteZambia.Core/Data/Http/HttpFamilyRepository.cs`, `TasteZambia.Core/Services/IVoiceRecorder.cs`, `TasteZambia.Mobile/Services/MauiVoiceRecorder.cs`
- Rewrite: `TasteZambia.Core/Services/FamilyArchiveService.cs`, `TasteZambia.Core/ViewModels/FamilyLifecycleViewModels.cs`, `TasteZambia.Core/ViewModels/FamilyViewModel.cs`
- Modify: `TasteZambia.Core/Models/FamilyArchive.cs`, the five `Views/Family/*.xaml`, `MauiProgram.cs`
- Test: `TasteZambia.Core.Tests/ViewModels/FamilyLifecycleTests.cs` (rewritten)

**Interfaces:**
- Produces:
  - `IFamilyArchiveService` (rewritten):
    - `Task<IReadOnlyList<PreservedRecipe>> GetShelfAsync(ct)`
    - `Task<FamilyRecipeDto?> GetAsync(Guid id, ct)`
    - `Task<FamilyRecipeDto> PreserveAsync(ContributionDraft draft, ct)`
    - `Task<InviteDto?> InviteAsync(Guid id, string displayName, string relation, ct)`
    - `Task<FamilyRecipeDto?> AcceptInviteAsync(string code, ct)`
    - `Task RemoveMemberAsync(Guid id, Guid memberId, ct)`
    - `Task AddNoteAsync(Guid id, string body, ct)`
    - `Task SetPrivacyAsync(Guid id, PrivacyLevel privacy, ct)`
    - `Task AttachMediaAsync(Guid id, Guid mediaId, ct)`
  - `IVoiceRecorder`: `Task StartAsync(ct)`, `Task<PickedFile?> StopAsync(ct)`, `bool IsRecording`, `TimeSpan Elapsed`
  - Models gain ids: `PreservedRecipe(Guid Id, string Name, string TaughtBy, string Privacy, string BadgeBgHex, string BadgeFgHex, string Extras)`, `FamilyMember(Guid Id, string Name, string Role, string Status, string BadgeBgHex, string BadgeFgHex)`, `FamilyNote(Guid Id, string Who, string When, string Body)`
  - `Extras` is built from the real counts: `"Audio 12:40 · 3 photos · 2 family notes"`, omitting each part that is zero, and reading `"Draft · 40% complete"` when `PercentComplete < 100`.
- The five screens: `famStart` (the shelf plus "preserve a recipe"), `famDraft` (the wizard, now writing a real `FamilyRecipe`), `famSaved` (one recipe: members, notes, audio, privacy), `famShared` (invite and member management), `famPublic` (provenance and the publish decision).
- **Nothing preserved yet** is a first-class state on `famStart`, replacing the four seeded shelf rows.

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public async Task TheShelf_ShowsWhatIsPreserved_AndSaysSoWhenNothingIs()
{
    var family = new FakeFamilyService();
    var vm = new FamStartViewModel(family, new Nav());

    await vm.LoadAsync();
    Assert.True(vm.IsEmpty);
    Assert.Empty(vm.Recipes);

    family.Add(new FamilyRecipeDto(Guid.NewGuid(), "Ifisashi ya Banakulu", "", "Northern", "Bemba",
        "Banakulu Mwaba, Mungwi", "", "She cooked this every time we arrived.", "Clay pot on the mbaula.",
        PrivacyLevel.SharedWithFamily, TranscriptState.None, null, 100, true, DateTimeOffset.UtcNow,
        [], [], []));

    await vm.RefreshCommand.ExecuteAsync(null);

    Assert.False(vm.IsEmpty);
    var row = Assert.Single(vm.Recipes);
    Assert.Equal("Ifisashi ya Banakulu", row.Name);
    Assert.Equal("Banakulu Mwaba, Mungwi", row.TaughtBy);
    Assert.Equal("Family", row.Privacy);
}

[Fact]
public async Task ExtrasReadTheWayTheDesignWritesThem()
{
    Assert.Equal("Audio 12:40 · 3 photos · 2 family notes",
        PreservedRecipe.Extras(hasAudio: true, audioLength: "12:40", photos: 3, notes: 2, percentComplete: 100));
    Assert.Equal("1 photo · no audio yet",
        PreservedRecipe.Extras(hasAudio: false, audioLength: "", photos: 1, notes: 0, percentComplete: 100));
    Assert.Equal("Draft · 40% complete",
        PreservedRecipe.Extras(hasAudio: false, audioLength: "", photos: 0, notes: 0, percentComplete: 40));
}

[Fact]
public async Task InvitingARelative_ShowsTheCodeToShare()
{
    var family = new FakeFamilyService();
    var id = family.Add(SomeRecipe());
    var vm = new FamSharedViewModel(family, new Nav()) { Id = id };
    await vm.LoadAsync();

    vm.NewMemberName = "Mutinta Mwaba";
    vm.NewMemberRelation = "Sister, Lusaka";
    await vm.InviteCommand.ExecuteAsync(null);

    Assert.Equal(8, vm.InviteCode.Length);
    Assert.Contains(vm.Members, m => m.Name == "Mutinta Mwaba" && m.Status == "Invited");
    Assert.Equal("", vm.NewMemberName);   // the field clears, ready for the next one
}

[Fact]
public async Task ChangingPrivacyToPublic_WarnsThatItLeavesTheFamily()
{
    var family = new FakeFamilyService();
    var id = family.Add(SomeRecipe());
    var vm = new FamPublicViewModel(family, new Nav()) { Id = id };
    await vm.LoadAsync();

    await vm.SetPublicCommand.ExecuteAsync(null);

    Assert.Equal(PrivacyLevel.PublicInArchive, family.PrivacyOf(id));
    Assert.Contains("everyone", vm.PrivacyNote, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test TasteZambia.Core.Tests --filter FamilyLifecycleTests`
Expected: FAIL.

- [ ] **Step 3: Rewrite the service over HTTP**

`FamilyArchiveService` loses every `SeedData` reference and becomes a thin client over `ApiRoutes.Family.*`, in the same shape as `ContributionService`: `PostAsJsonAsync`, `GetFromJsonAsync`, `EnsureSuccessStatusCode`, and `HttpRequestException` caught by the ViewModels' `LoadAsync`. `PreservedRecipe.Extras` is a static on the model so it is tested once.

- [ ] **Step 4: Rebind the five screens**

Each `Fam*ViewModel` gains an `Id` route parameter where it needs one, loads through `BaseViewModel.LoadAsync`, and overrides `HasContent`. `famStart` gets the empty state. `famDraft` writes through `PreserveAsync` and shows real checklist progress from `PercentComplete`. `famSaved` renders members, notes and the audio row from the recipe. `famShared` invites and removes. `famPublic` shows provenance built from what actually happened — preserved on a date, N members with access, published or not — and the privacy control.

- [ ] **Step 5: Recording**

`MauiVoiceRecorder` over `Plugin.Maui.Audio`: `dotnet add TasteZambia.Mobile package Plugin.Maui.Audio`. `RECORD_AUDIO` in the manifest, permission requested before the first recording. `StopAsync` returns a `PickedFile` with `audio/mp4`, which goes through the same `IMediaUploader`.

- [ ] **Step 6: Run everything, build, verify on device**

Run: `dotnet test TasteZambia.Core.Tests && dotnet test TasteZambia.API.Tests` → PASS.
Run: `dotnet build TasteZambia.Mobile -f net10.0-android --no-incremental` → no warnings.

On the phone: preserve a recipe with a photo and a short recording; invite yourself from a second install (or a second device account) with the code; add a note; remove the member and watch it disappear; set it public and confirm Explore does **not** show it (publishing into the archive is Stage 3's path, and a separate decision).

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat(mobile): the family archive reads and writes the account

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 7: Profile counts and the Stage 4 loose ends

**Files:**
- Modify: `TasteZambia.Core/Data/Http/HttpProfileRepository.cs`, `TasteZambia.Core/ViewModels/ShareLifecycleViewModels.cs`, `TasteZambia.Mobile/MauiProgram.cs`
- Test: `TasteZambia.API.Tests/Features/RepositoryParityTests.cs`

**Interfaces:**
- Consumes: `IFamilyArchiveService.GetShelfAsync`
- Produces: `UserProfile.PreservedCount` from the shelf; `ShareStartViewModel.PreservedCount` likewise; the "My Family Recipes" collection label reads its real count

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public async Task Profile_PreservedCount_IsWhatTheFamilyShelfHolds()
{
    var (client, _) = await _factory.SignedInClientAsync();
    using (client)
    {
        var family = new HttpFamilyArchiveService(client);
        await family.PreserveAsync(new ContributionDraft { LocalName = "Ifisashi ya Banakulu", Province = "Northern" });
        await family.PreserveAsync(new ContributionDraft { LocalName = "Inkoko ya Bataata", Province = "Copperbelt" });

        var repo = new HttpProfileRepository(client, new PersonalStore(new InMemoryLocalStore(), TimeProvider.System),
            new ContributionService(new DraftStore(new InMemoryLocalStore(), TimeProvider.System), client), family);

        var profile = await repo.GetAsync();
        Assert.Equal(2, profile.PreservedCount);

        var collections = await repo.GetCollectionsAsync();
        Assert.Equal("2 preserved", collections[3].CountLabel);
    }
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test TasteZambia.API.Tests --filter PreservedCount`
Expected: FAIL — `HttpProfileRepository` takes three dependencies.

- [ ] **Step 3: Thread the shelf through**

`HttpProfileRepository` takes `IFamilyArchiveService`; `PreservedCount` and the fourth collection label come from `GetShelfAsync().Count`, wrapped in the same offline guard as `PublishedCountAsync`. `ShareStartViewModel.PreservedCount` likewise.

- [ ] **Step 4: Run both suites and commit**

Run: `dotnet test TasteZambia.API.Tests && dotnet test TasteZambia.Core.Tests` → PASS.

```bash
git add -A && git commit -m "feat(mobile): preserved counts come from the family shelf

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage.** Stage 4 in the backend plan asked for: blob storage behind `IMediaStore` (Task 1), `MediaAsset`, `FamilyRecipe`, `FamilyMember`, `FamilyNote`, `FamilyInvite` (Tasks 2–3), access control enforced in a service applied to every query (Task 3, `VisibleTo`), and the endpoints `POST /media`, `GET /media/{id}`, `GET|POST /me/family-recipes`, `POST /family-recipes/{id}/invite`, `POST /family-recipes/{id}/notes`, `PUT /family-recipes/{id}/privacy` (Tasks 2, 4). Mobile Tasks 16 and 21 are unblocked by Tasks 5–6. The audio decision — one blob with range requests, not chunked — is decision 1 and is implemented by `enableRangeProcessing: true` in Task 2. Transcription is explicitly Stage 5 (decision 5): `TranscriptState` exists as a column and nothing drives it.

**Placeholder scan.** None. Task 4's mappings and controller are described rather than fully written — every method's inputs, outputs and status codes are specified in the Interfaces block and pinned by the eight tests, and the controller is mechanical given `FamilyService`. Task 6's XAML is likewise specified by behaviour and pinned by ViewModel tests, following the pattern the five screens already use.

**Type consistency.** `MediaKind` is the same enum in the store, the service, the controller, the uploader and the recorder. `PickedFile` is produced by `IPhotoPicker` and `IVoiceRecorder`, consumed by `IMediaUploader` — one shape. `FamilyRecipeDto`'s 18 members are the same in the mapping (Task 4) and the mobile tests (Task 6). `IFamilyAccessService` gains members across Tasks 2 and 3; Task 3 states the widening explicitly so Task 2's registration is updated rather than duplicated.

**Review Focus coverage.** Member removal (Task 3, `ARemovedMember_LosesAccessImmediately` and Task 4's endpoint test). Half-written blobs (Task 1, `AFailedWrite_LeavesNoPartialBlobBehind`, plus the row/blob cleanup in `MediaService.UploadAsync`). A stranger holding a media id (Task 2, `SomeoneElsesMedia_Is404_EvenHoldingTheId`). Privacy moving backwards (Task 3, `PrivacyMovingBackToFamily_ShutsStrangersOutAgain`). Offline uploads (Task 5, `AnOfflineUpload_KeepsThePhotoAndQueuesIt` and `AQueuedUpload_SurvivesARestart`).

**Three risks for the executor.**
1. **`FamilyAccessService` changes shape between Tasks 2 and 3.** Task 2 writes it parameterless; Task 3 gives it a `TasteZambiaDbContext`. Update the DI registration in Task 3 or the media tests fail with a resolution error rather than a logic error.
2. **`IFormFile.Length` is the declared length, not the received one.** `RequestSizeLimit` is what actually stops a lying client; the `declaredLength` check in `MediaService` is a courtesy that returns a good error message. Do not remove the attribute because the service checks.
3. **`RandomNumberGenerator.GetItems<char>` needs .NET 8+** and a `ReadOnlySpan<char>` — `CodeAlphabet` is a `const string`, which converts implicitly. If the overload will not bind, the argument needs `.AsSpan()`.
