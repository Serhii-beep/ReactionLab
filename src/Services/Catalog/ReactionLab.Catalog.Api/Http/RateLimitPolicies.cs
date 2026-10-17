using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ReactionLab.Catalog.Api.Http;

internal static class RateLimitPolicies
{
    public const string Matching = "matching";

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddConcurrencyLimiter(Matching, limiter =>
        {
            limiter.PermitLimit = 10;
            limiter.QueueLimit = 20;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
    }
}
