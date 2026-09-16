using Microsoft.Net.Http.Headers;

namespace TasteZambia.API.Common.Http;

public static class ETagResults
{
    /// <summary>
    /// Returns 304 when the client already holds this version, otherwise 200 with the ETag.
    /// Every collection read goes through here so conditional GETs work uniformly - and
    /// it is the hook Stage 5's delta sync builds on.
    /// </summary>
    public static IResult OkWithETag<T>(HttpContext http, string etag, T payload)
    {
        var incoming = http.Request.Headers.IfNoneMatch.ToString();

        if (!string.IsNullOrEmpty(incoming) && incoming == etag)
            return Results.StatusCode(StatusCodes.Status304NotModified);

        http.Response.Headers[HeaderNames.ETag] = etag;
        http.Response.Headers[HeaderNames.CacheControl] = "private, max-age=0, must-revalidate";
        return Results.Ok(payload);
    }

    public static IResult NotFound(string title, string detail)
        => Results.Problem(title: title, detail: detail, statusCode: StatusCodes.Status404NotFound);
}
