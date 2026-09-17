using System.Net;
using System.Net.Http.Headers;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Data.Http;

/// <summary>
/// Puts the bearer token on every request. On a 401, refreshes once and retries once;
/// a second 401 is returned as-is. Never loops.
/// </summary>
public sealed class AuthenticatedHandler(IAuthSession session) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await session.EnsureSignedInAsync(ct);
        Attach(request);

        var response = await base.SendAsync(request, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        if (!await session.RefreshAsync(ct))
            return response;

        response.Dispose();
        var retry = await CloneAsync(request, ct);
        Attach(retry);
        return await base.SendAsync(retry, ct);
    }

    private void Attach(HttpRequestMessage request)
    {
        if (session.AccessToken is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // A request can only be sent once, so the retry is a copy.
    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage original, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (var h in original.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        if (original.Content is not null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync(ct);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var h in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        return clone;
    }
}
