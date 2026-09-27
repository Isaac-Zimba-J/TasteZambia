using TasteZambia.Core.Services;
using TasteZambia.Core.ViewModels;
using TasteZambia.Core.Data;

namespace TasteZambia.Core.Tests;

/// <summary>Real services over throwaway local state and a client that can never reach a server.</summary>
public static class TestServices
{
    public static HttpClient NoNetwork() => new() { BaseAddress = new Uri("http://localhost:1"), Timeout = TimeSpan.FromMilliseconds(200) };

    public static OnboardingService Onboarding() => new(new InMemoryLocalStore(), NoNetwork());

    public static ContributionService Contributions() => new(new DraftStore(new InMemoryLocalStore(), TimeProvider.System), NoNetwork());

    public static PersonalStore Personal() => new(new InMemoryLocalStore(), TimeProvider.System);
    public static FavouritesService Favourites() => new(Personal());
    public static CookingProgressService Progress() => new(Personal());

    /// <summary>Search across the seeded archive, as the app wires it.</summary>
    public static ArchiveSearchService Search(IDishRepository? dishes = null)
        => new(dishes ?? new InMemoryDishRepository(), new InMemoryIngredientRepository(),
               new InMemoryArticleRepository(), new InMemoryRegionRepository());

    /// <summary>
    /// A Home ViewModel over the seeded repositories. Every test that builds one goes through
    /// here, so adding a dependency is one edit rather than one per test.
    /// </summary>
    public static HomeViewModel Home(INavigationService navigation, IDishRepository? dishes = null, IProfileRepository? profiles = null)
    {
        var repository = dishes ?? new InMemoryDishRepository();
        return new HomeViewModel(
            new CatalogService(repository),
            new InMemoryCategoryRepository(),
            Favourites(),
            new PreferenceService(),
            profiles ?? new InMemoryProfileRepository(),
            Search(repository),
            navigation);
    }
}
