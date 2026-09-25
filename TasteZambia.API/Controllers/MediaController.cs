using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteZambia.API.Auth;
using TasteZambia.API.Common.Http;
using TasteZambia.API.Services;
using TasteZambia.Shared.Contracts.Media;
using TasteZambia.Shared.Enums;
using TasteZambia.Shared.Routes;
using TasteZambia.Shared.Validation;

namespace TasteZambia.API.Controllers;

[ApiController]
[Authorize]
public sealed class MediaController(ICurrentUser me, IMediaService media) : ControllerBase
{
    private string UserId => me.UserId ?? throw new UnauthorizedAccessException();

    [HttpPost(ApiRoutes.Media.Collection)]
    [RequestSizeLimit(MediaLimits.MaxAudioBytes + 1024 * 1024)]   // the largest allowed, plus multipart overhead
    [ProducesResponseType<UploadResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] MediaKind kind, CancellationToken ct)
    {
        var contentType = file.ContentType;
        try
        {
            await using var content = file.OpenReadStream();
            var asset = await media.UploadAsync(UserId, kind, contentType, content, file.Length, ct);
            return Created(ApiRoutes.Media.ById.Replace("{id}", asset.Id.ToString()),
                new UploadResultDto(asset.Id, $"/api/v1/media/{asset.Id}"));
        }
        catch (ArgumentOutOfRangeException)
        {
            return Problem(title: "Not an allowed file type",
                detail: "The archive takes JPEG, PNG or WebP photos, and M4A, AAC, MP3 or WAV recordings.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }
        catch (InvalidDataException ex)
        {
            return Problem(title: "That file is too large", detail: ex.Message,
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }
    }

    [HttpGet(ApiRoutes.Media.ById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var found = await media.OpenAsync(id, UserId, ct);
        if (found is null) return this.NotFoundProblem("Not found", $"No media {id}.");

        // Private: this is somebody's family photograph, not a public asset.
        Response.Headers.CacheControl = "private, max-age=86400";
        return File(found.Value.Content, found.Value.Asset.ContentType, enableRangeProcessing: true);
    }
}
