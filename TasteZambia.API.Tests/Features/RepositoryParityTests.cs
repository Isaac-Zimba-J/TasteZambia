using System.Net.Http.Headers;
using System.Net.Http.Json;
using TasteZambia.Core.Data;
using TasteZambia.Core.Data.Http;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Contracts.Auth;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Contracts.Media;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Tests.Features;

/// <summary>
/// The whole point of Stage 1: the mobile app swaps InMemory* repositories for Http*
/// ones and nothing above the repository layer changes. These tests drive both
/// implementations of each interface - one over seeded data, one over the live API -
/// and assert they return the same models. If they match, the app cannot tell the
/// difference, and no ViewModel test needs to run against HTTP.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class RepositoryParityTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(fixture.ConnectionString);
        await _factory.SeedAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() { _client.Dispose(); _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Dishes_MatchTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryDishRepository().GetAllAsync();
        var http = await new HttpDishRepository(_client).GetAllAsync();

        Assert.Equal(seeded, http);   // Dish is a record: value equality on every field
    }

    [Fact]
    public async Task Recipe_MatchesTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryDishRepository().GetRecipeAsync("ifisashi");
        var http = await new HttpDishRepository(_client).GetRecipeAsync("ifisashi");

        Assert.NotNull(http);
        Assert.Equal(seeded!.Subtitle, http!.Subtitle);
        Assert.Equal(seeded.Dish, http.Dish);
        Assert.Equal(seeded.CulturalContext, http.CulturalContext);
        Assert.Equal(seeded.Ingredients, http.Ingredients);
        Assert.Equal(seeded.Steps, http.Steps);
        Assert.Equal(seeded.TraditionalMethod.Heading, http.TraditionalMethod.Heading);
        Assert.Equal(seeded.TraditionalMethod.Paragraphs, http.TraditionalMethod.Paragraphs);
        Assert.Equal(seeded.ModernMethod.Paragraphs, http.ModernMethod.Paragraphs);
        Assert.Equal(seeded.Variations, http.Variations);
        Assert.Equal(seeded.Contributor, http.Contributor);
    }

    [Fact]
    public async Task MissingRecipe_IsNullOnBothSides()
    {
        Assert.Null(await new InMemoryDishRepository().GetRecipeAsync("kapenta"));
        Assert.Null(await new HttpDishRepository(_client).GetRecipeAsync("kapenta"));
    }

    [Fact]
    public async Task Ingredients_MatchTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryIngredientRepository().GetAllAsync();
        var http = await new HttpIngredientRepository(_client).GetAllAsync();

        Assert.Equal(seeded.Count, http.Count);
        foreach (var (s, h) in seeded.Zip(http))
        {
            Assert.Equal(s.Key, h.Key);
            Assert.Equal(s.LocalNames, h.LocalNames);
            Assert.Equal(s.UsedInDishIds, h.UsedInDishIds);
            Assert.Equal(s.PendingLanguages, h.PendingLanguages);
            Assert.Equal(s.Description, h.Description);
        }
    }

    [Fact]
    public async Task Provinces_MatchTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryRegionRepository().GetAllAsync();
        var http = await new HttpRegionRepository(_client).GetAllAsync();

        Assert.Equal(seeded.Count, http.Count);
        foreach (var (s, h) in seeded.Zip(http))
        {
            Assert.Equal(s.Name, h.Name);
            Assert.Equal(s.SignatureFoods, h.SignatureFoods);
            Assert.Equal(s.CommonIngredients, h.CommonIngredients);
            Assert.Equal(s.CookingTradition, h.CookingTradition);
        }
    }

    [Fact]
    public async Task Articles_MatchTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryArticleRepository().GetByIdAsync("nshima");
        var http = await new HttpArticleRepository(_client).GetByIdAsync("nshima");

        Assert.NotNull(http);
        Assert.Equal(seeded!.Body, http!.Body);
        Assert.Equal(seeded.Audio, http.Audio);
        Assert.Equal(seeded.RelatedDishIds, http.RelatedDishIds);
    }

    [Fact]
    public async Task Categories_MatchTheSeededRepositoryExactly()
    {
        var seeded = await new InMemoryCategoryRepository().GetAllAsync();
        var http = await new HttpCategoryRepository(_client).GetAllAsync();

        Assert.Equal(seeded, http);
    }

    [Fact]
    public async Task Profile_ReadsTheAccountAndCountsFromTheLocalStore()
    {
        var store = new PersonalStore(new InMemoryLocalStore(), TimeProvider.System);
        store.SetSaved("chikanda", true);
        store.SetSaved("delele", true);
        store.SetDone("nshima", 1, true);

        var tokens = await (await _client.PostAsJsonAsync(ApiRoutes.Auth.Device,
            new DeviceAuthRequest($"device-{Guid.NewGuid():N}", new string('s', 40)))).Content.ReadFromJsonAsync<AuthTokensDto>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        await _client.PutAsJsonAsync(ApiRoutes.Me.Profile, new UpdateProfileRequest("Chanda Mwaba", "Kitwe, Copperbelt", "Bemba, English"));

        var repo = new HttpProfileRepository(_client, store, new ContributionService(new DraftStore(new InMemoryLocalStore(), TimeProvider.System), _client),
            new FamilyArchiveService(new HttpFamilyRepository(_client)));
        var profile = await repo.GetAsync();

        Assert.Equal("Chanda Mwaba", profile.Name);
        Assert.Equal(2, profile.FavouriteCount);
        Assert.Equal(1, profile.CookedCount);
        Assert.Equal(0, profile.ContributedCount);   // nothing published on this account yet

        var collections = await repo.GetCollectionsAsync();
        Assert.Equal("2 recipes", collections[0].CountLabel);
        Assert.Equal("1 recipe", collections[2].CountLabel);
    }

    [Fact]
    public async Task Profile_ContributionsComeFromTheAccount()
    {
        var (client, _) = await _factory.SignedInClientAsync();
        var service = new ContributionService(new DraftStore(new InMemoryLocalStore(), TimeProvider.System), client);
        var submitted = await service.SubmitAsync(TasteZambia.Core.Data.SeedData.WalkthroughShareDraft());

        var repo = new HttpProfileRepository(client, new PersonalStore(new InMemoryLocalStore(), TimeProvider.System), service,
            new FamilyArchiveService(new HttpFamilyRepository(client)));
        var contributions = await repo.GetContributionsAsync();

        var row = Assert.Single(contributions);
        Assert.Equal(submitted.Id, row.Id);
        Assert.Equal("Chibwabwa na Mbalala", row.Name);
        Assert.Equal(TasteZambia.Shared.Enums.ContributionStatus.InReview, row.Status);
        Assert.StartsWith("Northern Province · submitted ", row.Meta);
        client.Dispose();
    }

    [Fact]
    public async Task Profile_OfANewReader_ReadsAsEmptyRatherThanInvented()
    {
        var (client, _) = await _factory.SignedInClientAsync();
        using (client)
        {
            var store = new PersonalStore(new InMemoryLocalStore(), TimeProvider.System);
            var repo = new HttpProfileRepository(client, store, new ContributionService(new DraftStore(new InMemoryLocalStore(), TimeProvider.System), client),
                new FamilyArchiveService(new HttpFamilyRepository(client)));

            var profile = await repo.GetAsync();

            Assert.Equal(HttpProfileRepository.DefaultName, profile.Name);
            Assert.Equal("", profile.Location);
            Assert.Equal(0, profile.FavouriteCount);
            Assert.Equal(0, profile.CookedCount);
            Assert.Equal(0, profile.ContributedCount);
            Assert.Equal(0, profile.PreservedCount);

            var collections = await repo.GetCollectionsAsync();
            Assert.Equal("Nothing yet", collections[0].CountLabel);
            Assert.Equal("Nothing preserved yet", collections[3].CountLabel);

            Assert.Empty(await repo.GetContributionsAsync());
        }
    }

    [Fact]
    public async Task Profile_UpdateAsync_SavesTheNameToTheAccount()
    {
        var (client, _) = await _factory.SignedInClientAsync();
        using (client)
        {
            var store = new PersonalStore(new InMemoryLocalStore(), TimeProvider.System);
            var repo = new HttpProfileRepository(client, store, new ContributionService(new DraftStore(new InMemoryLocalStore(), TimeProvider.System), client),
                new FamilyArchiveService(new HttpFamilyRepository(client)));

            await repo.UpdateAsync("Chanda Mwaba", "Kitwe, Copperbelt", "Bemba, English");

            var profile = await repo.GetAsync();
            Assert.Equal("Chanda Mwaba", profile.Name);
            Assert.Equal("Kitwe, Copperbelt", profile.Location);
            Assert.Equal("Bemba, English", profile.Languages);
        }
    }

    [Fact]
    public async Task Profile_PreservedCount_IsWhatTheFamilyShelfHolds()
    {
        var (client, _) = await _factory.SignedInClientAsync();
        using (client)
        {
            var family = new FamilyArchiveService(new HttpFamilyRepository(client));
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

    /// <summary>
    /// The regression for the family archive's own verb mismatch: <c>HttpFamilyRepository</c>
    /// used to call the media endpoint with POST while the controller declares it PUT, so every
    /// attach 404'd. A fake service never noticed, because it does not speak HTTP at all - this
    /// drives the real repository against the real API, the same as the other repositories above.
    /// </summary>
    [Fact]
    public async Task Family_AttachMediaAsync_ReallyAttachesThroughTheLiveMediaRoute()
    {
        var (client, _) = await _factory.SignedInClientAsync();
        using (client)
        {
            var repo = new HttpFamilyRepository(client);
            var recipe = await repo.CreateAsync(new CreateFamilyRecipeRequest(
                "Ifisashi ya Banakulu", "Pumpkin leaves in groundnuts", "Northern", "Bemba",
                "Banakulu Mwaba", "Mungwi", "She cooked this every time.", "Clay pot on the mbaula.",
                PrivacyLevel.SharedWithFamily), CancellationToken.None);

            var file = new ByteArrayContent([1, 2, 3]);
            file.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            var upload = new MultipartFormDataContent
            {
                { file, "file", "photo.png" },
                { new StringContent(MediaKind.Photo.ToString()), "kind" },
            };
            var uploaded = await (await client.PostAsync(ApiRoutes.Media.Collection, upload))
                .Content.ReadFromJsonAsync<UploadResultDto>();

            var attached = await repo.AttachMediaAsync(recipe.Id, uploaded!.Id, CancellationToken.None);
            Assert.True(attached);   // would be false (a 404 swallowed as "not found") under the wrong verb

            var seen = await repo.GetAsync(recipe.Id, CancellationToken.None);
            Assert.Contains(seen!.Media, m => m.Id == uploaded.Id);
        }
    }
}
