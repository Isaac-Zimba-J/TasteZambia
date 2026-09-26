using System.Net;
using System.Net.Http.Json;
using TasteZambia.Shared.Contracts.Family;
using TasteZambia.Shared.Routes;

namespace TasteZambia.Core.Data.Http;

/// <summary>
/// The raw wire calls for <c>ApiRoutes.Family.*</c> - DTOs in, DTOs out, nothing mapped to a
/// Core model. <see cref="Services.FamilyArchiveService"/> is the layer that knows what a
/// <see cref="Models.PreservedRecipe"/> is; this one only knows the API.
/// </summary>
public sealed class HttpFamilyRepository(HttpClient http)
{
    public async Task<List<FamilyRecipeSummaryDto>> GetShelfAsync(CancellationToken ct)
        => await http.GetFromJsonAsync<List<FamilyRecipeSummaryDto>>(ApiRoutes.Family.Collection, ct) ?? [];

    public Task<FamilyRecipeDto?> GetAsync(Guid id, CancellationToken ct)
        => Http.GetOrNullAsync<FamilyRecipeDto>(http, Http.Path(ApiRoutes.Family.ById, "id", id.ToString()), ct);

    public async Task<FamilyRecipeDto> CreateAsync(CreateFamilyRecipeRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(ApiRoutes.Family.Collection, request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FamilyRecipeDto>(ct))!;
    }

    /// <summary>Null on a 404 - not found, or a write from someone other than the owner.</summary>
    public async Task<InviteDto?> InviteAsync(Guid id, AddMemberRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(Http.Path(ApiRoutes.Family.Members, "id", id.ToString()), request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InviteDto>(ct);
    }

    /// <summary>Null when the code is unknown (404) or already spent (409) - both read the same to the reader.</summary>
    public async Task<FamilyRecipeDto?> AcceptAsync(AcceptInviteRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(ApiRoutes.Family.Accept, request, ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FamilyRecipeDto>(ct);
    }

    public async Task RemoveMemberAsync(Guid id, Guid memberId, CancellationToken ct)
    {
        var response = await http.DeleteAsync(Http.Path(Http.Path(ApiRoutes.Family.MemberById, "id", id.ToString()), "memberId", memberId.ToString()), ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task AddNoteAsync(Guid id, AddNoteRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(Http.Path(ApiRoutes.Family.Notes, "id", id.ToString()), request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetPrivacyAsync(Guid id, SetPrivacyRequest request, CancellationToken ct)
    {
        var response = await http.PutAsJsonAsync(Http.Path(ApiRoutes.Family.Privacy, "id", id.ToString()), request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task AttachMediaAsync(Guid id, Guid mediaId, CancellationToken ct)
    {
        var path = Http.Path(Http.Path(ApiRoutes.Family.Media, "id", id.ToString()), "mediaId", mediaId.ToString());
        var response = await http.PostAsync(path, null, ct);
        response.EnsureSuccessStatusCode();
    }
}
