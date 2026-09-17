using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Ingredients;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

[ApiController]
public sealed class IngredientsController(
    IIngredientRepository ingredients,
    IArchiveVersionService versions) : ControllerBase
{
    [HttpGet(ApiRoutes.Ingredients.Collection)]
    [ProducesResponseType<IReadOnlyList<IngredientDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var etag = await versions.GetETagAsync("ingredients", ct);
        var payload = (await ingredients.GetAllAsync(ct)).Select(i => i.ToDto()).ToList();
        return this.OkWithETag(etag, payload);
    }

    [HttpGet(ApiRoutes.Ingredients.ByKey)]
    [ProducesResponseType<IngredientDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct)
    {
        var ingredient = await ingredients.GetByKeyAsync(key, ct);
        return ingredient is null
            ? this.NotFoundProblem("Ingredient not found", $"No ingredient with key '{key}' is in the archive.")
            : Ok(ingredient.ToDto());
    }
}
