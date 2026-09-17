using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Common;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

[ApiController]
public sealed class CategoriesController(
    ICategoryRepository categories,
    IArchiveVersionService versions) : ControllerBase
{
    [HttpGet(ApiRoutes.Categories.Collection)]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var etag = await versions.GetETagAsync("categories", ct);
        var payload = (await categories.GetAllAsync(ct)).Select(c => c.ToDto()).ToList();
        return this.OkWithETag(etag, payload);
    }
}
