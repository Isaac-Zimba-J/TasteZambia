using System.Net.Http.Json;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Data.Http;

/// <summary>
/// The profile comes from the account; the counts come from the device's own copy of
/// its personal data, so they are right even with no signal. Contribution and family
/// numbers stay seeded until Stages 3 and 4 give them a source.
/// </summary>
public sealed class HttpProfileRepository(HttpClient me, PersonalStore store) : IProfileRepository
{
    private const string DefaultName = "Taste Zambia reader";

    public async Task<UserProfile> GetAsync(CancellationToken ct = default)
    {
        var p = await me.GetFromJsonAsync<ProfileDto>(ApiRoutes.Me.Profile, ct) ?? new ProfileDto("", "", "", null);

        return new UserProfile
        {
            Name = p.DisplayName.Length > 0 ? p.DisplayName : DefaultName,
            Location = p.Location,
            Languages = p.Languages,
            AvatarAsset = p.AvatarAsset ?? SeedData.Profile.AvatarAsset,
            FavouriteCount = store.SavedDishIds.Count,
            CookedCount = store.CookedDishIds.Count,
            ContributedCount = SeedData.Profile.ContributedCount,   // Stage 3
            PreservedCount = SeedData.Profile.PreservedCount,       // Stage 4
        };
    }

    public Task<IReadOnlyList<RecipeCollection>> GetCollectionsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<RecipeCollection>>(
        [
            new("My Favourite Zambian Foods", Recipes(store.SavedDishIds.Count),  "#A3452A"),
            new("Recipes I Want to Try",      "9 recipes",                         "#C07F1E"),
            new("Recipes I've Cooked",        Recipes(store.CookedDishIds.Count), "#2F6A4D"),
            new("My Family Recipes",          "4 preserved",                       "#17402F"),
        ]);

    public Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default)
        => Task.FromResult(SeedData.Contributions);

    private static string Recipes(int n) => n == 1 ? "1 recipe" : $"{n} recipes";
}
