using TasteZambia.API.Common.Endpoints;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Features.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Common;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Features.Categories;

public sealed class GetCategories : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Categories.Collection, Handle)
           .WithName("GetCategories").WithTags("Categories")
           .Produces<IReadOnlyList<CategoryDto>>();

    private static async Task<IResult> Handle(HttpContext http, ICategoryRepository categories,
                                              IArchiveVersionService versions, CancellationToken ct)
    {
        var etag = await versions.GetETagAsync("categories", ct);
        var payload = (await categories.GetAllAsync(ct)).Select(c => c.ToDto()).ToList();
        return ETagResults.OkWithETag(http, etag, payload);
    }
}
