using TasteZambia.API.Common.Endpoints;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Features.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Regions;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Features.Regions;

public sealed class GetRegions : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Regions.Collection, Handle)
           .WithName("GetRegions").WithTags("Regions")
           .Produces<IReadOnlyList<ProvinceDto>>();

    private static async Task<IResult> Handle(HttpContext http, IRegionRepository regions,
                                              IArchiveVersionService versions, CancellationToken ct)
    {
        var etag = await versions.GetETagAsync("regions", ct);
        var payload = (await regions.GetAllAsync(ct)).Select(p => p.ToDto()).ToList();
        return ETagResults.OkWithETag(http, etag, payload);
    }
}
