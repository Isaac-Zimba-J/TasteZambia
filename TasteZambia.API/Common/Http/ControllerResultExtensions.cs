using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace TasteZambia.API.Common.Http;

public static class ControllerResultExtensions
{
    /// <summary>
    /// Returns 304 when the client already holds this version, otherwise 200 with the ETag.
    /// Every collection read goes through here so conditional GETs work uniformly - and
    /// it is the hook Stage 5's delta sync builds on.
    /// </summary>
    public static IActionResult OkWithETag<T>(this ControllerBase controller, string etag, T payload)
    {
        var incoming = controller.Request.Headers.IfNoneMatch.ToString();

        if (!string.IsNullOrEmpty(incoming) && incoming == etag)
            return controller.StatusCode(StatusCodes.Status304NotModified);

        controller.Response.Headers[HeaderNames.ETag] = etag;
        controller.Response.Headers[HeaderNames.CacheControl] = "private, max-age=0, must-revalidate";
        return controller.Ok(payload);
    }

    /// <summary>RFC 9457 ProblemDetails for a missing row.</summary>
    public static IActionResult NotFoundProblem(this ControllerBase controller, string title, string detail)
        => controller.Problem(title: title, detail: detail, statusCode: StatusCodes.Status404NotFound);

    /// <summary>RFC 9457 ProblemDetails for an action the current state does not allow.</summary>
    public static IActionResult ConflictProblem(this ControllerBase controller, string detail)
        => controller.Problem(title: "Not allowed in the current state", detail: detail, statusCode: StatusCodes.Status409Conflict);
}
