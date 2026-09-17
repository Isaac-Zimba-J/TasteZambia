using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Regions;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

[ApiController]
public sealed class RegionsController(
    IRegionRepository regions,
    IArchiveVersionService versions) : ControllerBase
{
    [HttpGet(ApiRoutes.Regions.Collection)]
    [ProducesResponseType<IReadOnlyList<ProvinceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var etag = await versions.GetETagAsync("regions", ct);
        var payload = (await regions.GetAllAsync(ct)).Select(p => p.ToDto()).ToList();
        return this.OkWithETag(etag, payload);
    }
}
