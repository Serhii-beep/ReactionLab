using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ReactionLab.Catalog.Api.Http;

internal static class RateLimitPolicies
{
    public const string Matching = "matching";

    private const int RetryAfterSeconds = 1;

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status503ServiceUnavailable;
        options.OnRejected = (context, _) =>
        {
            context.HttpContext.Response.Headers.RetryAfter = RetryAfterSeconds.ToString(CultureInfo.InvariantCulture);
            context.HttpContext.Response.Headers.CacheControl = "no-store";

            return ValueTask.CompletedTask;
        };

        options.AddConcurrencyLimiter(Matching, limiter =>
        {
            limiter.PermitLimit = 10;
            limiter.QueueLimit = 20;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
    }
}
