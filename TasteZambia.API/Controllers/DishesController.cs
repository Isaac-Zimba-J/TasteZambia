using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Dishes;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

[ApiController]
public sealed class DishesController(
    IDishRepository dishes,
    ICatalogService catalog,
    IArchiveVersionService versions) : ControllerBase
{
    [HttpGet(ApiRoutes.Dishes.Collection)]
    [ProducesResponseType<IReadOnlyList<DishDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var etag = await versions.GetETagAsync("dishes", ct);
        var payload = (await dishes.GetAllAsync(ct)).Select(d => d.ToDto()).ToList();
        return this.OkWithETag(etag, payload);
    }

    [HttpGet(ApiRoutes.Dishes.ById)]
    [ProducesResponseType<DishDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var dish = await dishes.GetByIdAsync(id, ct);
        return dish is null
            ? this.NotFoundProblem("Dish not found", $"No dish with id '{id}' is in the archive.")
            : Ok(dish.ToDto());
    }

    [HttpGet(ApiRoutes.Dishes.Recipe)]
    [ProducesResponseType<RecipeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecipe(string id, CancellationToken ct)
    {
        var recipe = await dishes.GetRecipeAsync(id, ct);
        return recipe is null
            ? this.NotFoundProblem("Recipe not found", $"No recipe has been recorded for '{id}' yet.")
            : Ok(recipe.ToDto());
    }

    /// <summary>Searches local name, English name, region and description - the same haystack the app searches.</summary>
    [HttpGet(ApiRoutes.Dishes.Search)]
    [ProducesResponseType<IReadOnlyList<DishDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] string? filter, CancellationToken ct)
    {
        var results = await catalog.SearchAsync(q ?? "", filter ?? "All", ct);
        return Ok(results.Select(d => d.ToDto()).ToList());
    }
}
