using System.Net.Http.Json;
using TasteZambia.Core.Models;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Contracts.Me;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Data.Http;

/// <summary>
/// The profile comes from the account; the counts come from the device's own copy of
/// its personal data, so they are right even with no signal. Every number here is real:
/// nothing on the Profile screen is a placeholder.
/// </summary>
public sealed class HttpProfileRepository(HttpClient me, PersonalStore store, IContributionService contributions) : IProfileRepository
{
    /// <summary>Shown until the reader adds a name of their own.</summary>
    public const string DefaultName = "Taste Zambia reader";

    public async Task<UserProfile> GetAsync(CancellationToken ct = default)
    {
        var p = await me.GetFromJsonAsync<ProfileDto>(ApiRoutes.Me.Profile, ct) ?? new ProfileDto("", "", "", null);
        var published = await PublishedCountAsync(ct);

        return new UserProfile
        {
            Name = p.DisplayName.Length > 0 ? p.DisplayName : DefaultName,
            Location = p.Location,
            Languages = p.Languages,
            AvatarAsset = p.AvatarAsset ?? "",
            FavouriteCount = store.SavedDishIds.Count,
            CookedCount = store.CookedDishIds.Count,
            ContributedCount = published,
            PreservedCount = 0,   // Stage 4 gives the family archive a real source.
        };
    }

    public async Task UpdateAsync(string name, string location, string languages, CancellationToken ct = default)
    {
        var response = await me.PutAsJsonAsync(ApiRoutes.Me.Profile,
            new UpdateProfileRequest(name.Trim(), location.Trim(), languages.Trim()), ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<RecipeCollection>> GetCollectionsAsync(CancellationToken ct = default)
    {
        var saved = store.SavedDishIds.Count;
        var cooked = store.CookedDishIds.Count;

        return
        [
            new("My Favourite Zambian Foods", Count(saved, "recipe"), "#A3452A"),
            // Nothing writes a wishlist yet; the row stays so the shelf reads as designed.
            new("Recipes I Want to Try", "Nothing yet", "#C07F1E"),
            new("Recipes I've Cooked", Count(cooked, "recipe"), "#2F6A4D"),
            new("My Family Recipes", "Nothing preserved yet", "#17402F"),
        ];
    }

    public Task<IReadOnlyList<Contribution>> GetContributionsAsync(CancellationToken ct = default)
        => contributions.GetContributionsAsync(ct);

    private async Task<int> PublishedCountAsync(CancellationToken ct)
    {
        try
        {
            return (await contributions.GetContributionsAsync(ct)).Count(c => c.Status == ContributionStatus.Published);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Offline: the rest of the profile still renders from the local store.
            return 0;
        }
    }

    private static string Count(int n, string noun)
        => n == 0 ? "Nothing yet" : n == 1 ? $"1 {noun}" : $"{n} {noun}s";
}
