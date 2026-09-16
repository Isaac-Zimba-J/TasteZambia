using TasteZambia.API.Common.Endpoints;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Features.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Culture;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Features.Culture;

public sealed class GetArticles : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Culture.Articles, Handle)
           .WithName("GetArticles").WithTags("Culture")
           .Produces<IReadOnlyList<ArticleDto>>();

    private static async Task<IResult> Handle(HttpContext http, IArticleRepository articles,
                                              IArchiveVersionService versions, CancellationToken ct)
    {
        var etag = await versions.GetETagAsync("articles", ct);
        // Metadata only by design - the repository's list call omits Body.
        var payload = (await articles.GetAllAsync(ct)).Select(a => a.ToDto()).ToList();
        return ETagResults.OkWithETag(http, etag, payload);
    }
}

public sealed class GetArticleById : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(ApiRoutes.Culture.ArticleById, Handle)
           .WithName("GetArticleById").WithTags("Culture")
           .Produces<ArticleDto>().ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(string id, IArticleRepository articles, CancellationToken ct)
    {
        var article = await articles.GetByIdAsync(id, ct);
        return article is null
            ? ETagResults.NotFound("Article not found", $"No article with id '{id}' is in the archive.")
            : Results.Ok(article.ToDto());
    }
}
