using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Mapping;
using TasteZambia.API.Repositories;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Culture;
using TasteZambia.Shared.Routes;

namespace TasteZambia.API.Controllers;

[ApiController]
public sealed class ArticlesController(
    IArticleRepository articles,
    IArchiveVersionService versions) : ControllerBase
{
    /// <summary>Metadata only - the list call omits Body by design.</summary>
    [HttpGet(ApiRoutes.Culture.Articles)]
    [ProducesResponseType<IReadOnlyList<ArticleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var etag = await versions.GetETagAsync("articles", ct);
        var payload = (await articles.GetAllAsync(ct)).Select(a => a.ToDto()).ToList();
        return this.OkWithETag(etag, payload);
    }

    [HttpGet(ApiRoutes.Culture.ArticleById)]
    [ProducesResponseType<ArticleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var article = await articles.GetByIdAsync(id, ct);
        return article is null
            ? this.NotFoundProblem("Article not found", $"No article with id '{id}' is in the archive.")
            : Ok(article.ToDto());
    }
}
