using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ReactionLab.Identity.Api.Http;

internal static class RateLimitPolicies
{
    public const string SignIn = "sign-in";
    public const string Registration = "registration";

    private const int RetryAfterSeconds = 1;
    private const int SignInsInFlightPerCore = 4;
    private const int QueuedSignInsPerCore = 4;
    private const int ProcessorsPerRegistration = 4;
    private const int QueuedRegistrationsPerPermit = 4;

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status503ServiceUnavailable;
        options.OnRejected = (context, _) =>
        {
            context.HttpContext.Response.Headers.RetryAfter = RetryAfterSeconds.ToString(CultureInfo.InvariantCulture);
            context.HttpContext.Response.Headers.CacheControl = "no-store";

            return ValueTask.CompletedTask;
        };

        options.AddConcurrencyLimiter(SignIn, limiter =>
        {
            limiter.PermitLimit = Environment.ProcessorCount * SignInsInFlightPerCore;
            limiter.QueueLimit = Environment.ProcessorCount * QueuedSignInsPerCore;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

        options.AddConcurrencyLimiter(Registration, limiter =>
        {
            limiter.PermitLimit = Math.Max(1, Environment.ProcessorCount / ProcessorsPerRegistration);
            limiter.QueueLimit = limiter.PermitLimit * QueuedRegistrationsPerPermit;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
    }
}
