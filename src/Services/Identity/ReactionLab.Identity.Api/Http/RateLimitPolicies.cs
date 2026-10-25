using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Api.Http;

internal static class RateLimitPolicies
{
    public const string SignIn = "sign-in";
    public const string Registration = "registration";
    public const string PasswordChange = "password-change";
    public const string Reauthentication = "reauthentication";
    public const string ExternalRegistration = "external-registration";
    public const string AccountSetup = "account-setup";

    private const int RetryAfterSeconds = 1;
    private const int SignInsInFlightPerCore = 4;
    private const int QueuedSignInsPerCore = 4;
    private const int ExternalRegistrationsInFlightPerCore = 4;
    private const int QueuedExternalRegistrationsPerCore = 4;
    private const int ProcessorsPerRegistration = 4;
    private const int QueuedRegistrationsPerPermit = 4;
    private const int ProcessorsPerAccountSetup = 4;
    private const int QueuedAccountSetupsPerPermit = 4;
    private const int ProcessorsPerPasswordCheck = 4;
    private const int QueuedPasswordChecksPerPermit = 4;

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

        options.AddConcurrencyLimiter(ExternalRegistration, limiter =>
        {
            limiter.PermitLimit = Environment.ProcessorCount * ExternalRegistrationsInFlightPerCore;
            limiter.QueueLimit = Environment.ProcessorCount * QueuedExternalRegistrationsPerCore;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

        options.AddConcurrencyLimiter(AccountSetup, limiter =>
        {
            limiter.PermitLimit = Math.Max(1, Environment.ProcessorCount / ProcessorsPerAccountSetup);
            limiter.QueueLimit = limiter.PermitLimit * QueuedAccountSetupsPerPermit;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

        var passwordChecks = SharedPasswordCheckLimiter();

        options.AddPolicy(PasswordChange, httpContext => RateLimitPartition.Get(
            httpContext.User.GetClaim(Claims.Subject) ?? string.Empty,
            _ => RateLimiter.CreateChained(OnePasswordCheckAtATime(), passwordChecks)));

        options.AddPolicy(Reauthentication, httpContext => RateLimitPartition.Get(
            AccountAndSignInMethod(httpContext),
            _ => RateLimiter.CreateChained(OnePasswordCheckAtATime(), passwordChecks)));
    }

    private static (string AccountId, bool WithPassword) AccountAndSignInMethod(HttpContext httpContext) =>
        (httpContext.User.GetClaim(Claims.Subject) ?? string.Empty, httpContext.User.GetSessionSignIn().WithPassword);

    private static ConcurrencyLimiter SharedPasswordCheckLimiter()
    {
        var permits = Math.Max(1, Environment.ProcessorCount / ProcessorsPerPasswordCheck);

        return new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = permits,
            QueueLimit = permits * QueuedPasswordChecksPerPermit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    }

    private static ConcurrencyLimiter OnePasswordCheckAtATime() =>
        new(new ConcurrencyLimiterOptions { PermitLimit = 1, QueueLimit = 0 });
}
