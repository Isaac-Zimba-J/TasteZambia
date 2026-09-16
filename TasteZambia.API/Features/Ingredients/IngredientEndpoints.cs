using TasteZambia.API.Common.Endpoints;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Features.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Ingredients;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Features.Ingredients;

public sealed class GetIngredients : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Ingredients.Collection, Handle)
           .WithName("GetIngredients").WithTags("Ingredients")
           .Produces<IReadOnlyList<IngredientDto>>();

    private static async Task<IResult> Handle(HttpContext http, IIngredientRepository ingredients,
                                              IArchiveVersionService versions, CancellationToken ct)
    {
        var etag = await versions.GetETagAsync("ingredients", ct);
        var payload = (await ingredients.GetAllAsync(ct)).Select(i => i.ToDto()).ToList();
        return ETagResults.OkWithETag(http, etag, payload);
    }
}

public sealed class GetIngredientByKey : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Ingredients.ByKey, Handle)
           .WithName("GetIngredientByKey").WithTags("Ingredients")
           .Produces<IngredientDto>().ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(string key, IIngredientRepository ingredients, CancellationToken ct)
    {
        var ingredient = await ingredients.GetByKeyAsync(key, ct);
        return ingredient is null
            ? ETagResults.NotFound("Ingredient not found", $"No ingredient with key '{key}' is in the archive.")
            : Results.Ok(ingredient.ToDto());
    }
}
