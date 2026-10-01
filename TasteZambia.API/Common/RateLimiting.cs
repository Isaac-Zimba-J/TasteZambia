using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace TasteZambia.API.Common;

public static class RateLimiting
{
    /// <summary>
    /// One partition per caller. A signed-in account is identified by its own id, so a
    /// contributor is never limited by whoever else shares their carrier's address; anonymous
    /// callers fall back to the address itself.
    ///
    /// UseForwardedHeaders runs first, so this is the phone's address and not the proxy's.
    /// A request with no address at all - in-process tests, a unix socket - partitions
    /// together under one key rather than escaping the limit entirely.
    /// </summary>
    private static string CallerKey(HttpContext http)
        => http.User.Identity?.IsAuthenticated == true
            ? $"user:{http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? http.User.Identity.Name}"
            : $"ip:{http.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

    private static string AddressKey(HttpContext http)
        => http.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public static IServiceCollection AddArchiveRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.Section));

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // The default rejection is an empty 429. The app shows the reader what the archive
            // said, so it has to say something - and Retry-After is what turns "no" into
            // "not yet", which is the difference between a bug and a wait.
            o.OnRejected = async (context, _) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var after)
                    ? after
                    : context.HttpContext.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value.Window;

                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);

                // Through the app's own problem-details service rather than WriteAsJsonAsync,
                // which would send plain application/json. Every other failure the API reports
                // is problem+json, and the app should not need a special case for this one.
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.RequestServices
                    .GetRequiredService<IProblemDetailsService>()
                    .WriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = context.HttpContext,
                        ProblemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Too many requests",
                            Detail = $"Too many requests from here. Try again in about {Describe(retryAfter)}.",
                        },
                    });
            };

            o.AddPolicy(RateLimitPolicies.DeviceAuth, http => Fixed(http, AddressKey(http), l => l.DeviceAuthPerWindow));
            o.AddPolicy(RateLimitPolicies.Refresh, http => Fixed(http, AddressKey(http), l => l.RefreshPerWindow));
            o.AddPolicy(RateLimitPolicies.Writes, http => Fixed(http, CallerKey(http), l => l.WritesPerWindow));

            // A backstop under the named policies, so an endpoint nobody remembered to
            // decorate is still not unlimited. Health is exempt: the proxy checks it every
            // 30 seconds from one address, and a limiter must never be why a container is
            // reported unhealthy.
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                http.Request.Path.StartsWithSegments("/health")
                    ? RateLimitPartition.GetNoLimiter("health")
                    : Fixed(http, $"global:{AddressKey(http)}", l => l.GlobalPerWindow));
        });

        return services;
    }

    private static RateLimitPartition<string> Fixed(HttpContext http, string key, Func<RateLimitOptions, int> permits)
    {
        var limits = http.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, permits(limits)),
            Window = limits.Window,
            // No queue: a caller over the limit is told so now. Holding their request open
            // would spend the server's memory on the very thing being limited.
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
    }

    private static string Describe(TimeSpan span) => span.TotalMinutes switch
    {
        < 1.5 => "a minute",
        < 60 => $"{(int)Math.Ceiling(span.TotalMinutes)} minutes",
        _ => "an hour",
    };
}
