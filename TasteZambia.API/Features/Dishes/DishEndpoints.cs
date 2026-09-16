using TasteZambia.API.Common.Endpoints;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Features.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Dishes;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Features.Dishes;

public sealed class GetDishes : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Dishes.Collection, Handle)
           .WithName("GetDishes").WithTags("Dishes")
           .Produces<IReadOnlyList<DishDto>>();

    private static async Task<IResult> Handle(HttpContext http, IDishRepository dishes,
                                              IArchiveVersionService versions, CancellationToken ct)
    {
        var etag = await versions.GetETagAsync("dishes", ct);
        var payload = (await dishes.GetAllAsync(ct)).Select(d => d.ToDto()).ToList();
        return ETagResults.OkWithETag(http, etag, payload);
    }
}

public sealed class GetDishById : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Dishes.ById, Handle)
           .WithName("GetDishById").WithTags("Dishes")
           .Produces<DishDto>().ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(string id, IDishRepository dishes, CancellationToken ct)
    {
        var dish = await dishes.GetByIdAsync(id, ct);
        return dish is null
            ? ETagResults.NotFound("Dish not found", $"No dish with id '{id}' is in the archive.")
            : Results.Ok(dish.ToDto());
    }
}

public sealed class GetRecipe : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Dishes.Recipe, Handle)
           .WithName("GetRecipe").WithTags("Dishes")
           .Produces<RecipeDto>().ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(string id, IDishRepository dishes, CancellationToken ct)
    {
        var recipe = await dishes.GetRecipeAsync(id, ct);
        return recipe is null
            ? ETagResults.NotFound("Recipe not found", $"No recipe has been recorded for '{id}' yet.")
            : Results.Ok(recipe.ToDto());
    }
}

public sealed class SearchDishes : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Dishes.Search, Handle)
           .WithName("SearchDishes").WithTags("Dishes")
           .Produces<IReadOnlyList<DishDto>>();

    private static async Task<IResult> Handle(string? q, string? filter, ICatalogService catalog, CancellationToken ct)
    {
        var results = await catalog.SearchAsync(q ?? "", filter ?? "All", ct);
        return Results.Ok(results.Select(d => d.ToDto()).ToList());
    }
}
