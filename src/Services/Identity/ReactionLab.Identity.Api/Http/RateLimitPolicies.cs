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
    public const string EmailConfirmationRequest = "email-confirmation-request";
    public const string PasswordChange = "password-change";
    public const string Reauthentication = "reauthentication";
    public const string ExternalRegistration = "external-registration";
    public const string AccountSetup = "account-setup";
    public const string MemberCredentials = "member-credentials";
    public const string MemberImports = "member-imports";

    private const int RetryAfterSeconds = 1;
    private const int SignInsInFlightPerCore = 4;
    private const int QueuedSignInsPerCore = 4;
    private const int ExternalRegistrationsInFlightPerCore = 4;
    private const int QueuedExternalRegistrationsPerCore = 4;
    private const int ProcessorsPerRegistration = 4;
    private const int EmailConfirmationRequestsInFlightPerCore = 16;
    private const int QueuedEmailConfirmationRequestsPerCore = 16;
    private const int QueuedRegistrationsPerPermit = 4;
    private const int ProcessorsPerAccountSetup = 4;
    private const int QueuedAccountSetupsPerPermit = 4;
    private const int ProcessorsPerMemberCredential = 4;
    private const int QueuedMemberCredentialsPerPermit = 4;
    private const int ProcessorsPerMemberImport = 4;
    private const int QueuedMemberImportsPerPermit = 1;
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

        options.AddConcurrencyLimiter(EmailConfirmationRequest, limiter =>
        {
            limiter.PermitLimit = Environment.ProcessorCount * EmailConfirmationRequestsInFlightPerCore;
            limiter.QueueLimit = Environment.ProcessorCount * QueuedEmailConfirmationRequestsPerCore;
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

        var passwordChecks = SharedLimiter(ProcessorsPerPasswordCheck, QueuedPasswordChecksPerPermit);
        var memberCredentials = SharedLimiter(ProcessorsPerMemberCredential, QueuedMemberCredentialsPerPermit);
        var memberImports = SharedLimiter(ProcessorsPerMemberImport, QueuedMemberImportsPerPermit);

        options.AddPolicy(PasswordChange, httpContext => RateLimitPartition.Get(
            httpContext.User.GetClaim(Claims.Subject) ?? string.Empty,
            _ => RateLimiter.CreateChained(OneRequestAtATime(), passwordChecks)));

        options.AddPolicy(Reauthentication, httpContext => RateLimitPartition.Get(
            AccountAndSignInMethod(httpContext),
            _ => RateLimiter.CreateChained(OneRequestAtATime(), passwordChecks)));

        options.AddPolicy(MemberCredentials, httpContext => RateLimitPartition.Get(
            httpContext.User.GetClaim(Claims.Subject) ?? string.Empty,
            _ => RateLimiter.CreateChained(OneRequestAtATime(), memberCredentials)));

        options.AddPolicy(MemberImports, httpContext => RateLimitPartition.Get(
            httpContext.User.GetClaim(Claims.Subject) ?? string.Empty,
            _ => RateLimiter.CreateChained(OneRequestAtATime(), memberImports)));
    }

    private static (string AccountId, bool WithPassword) AccountAndSignInMethod(HttpContext httpContext) =>
        (httpContext.User.GetClaim(Claims.Subject) ?? string.Empty, httpContext.User.GetSessionSignIn().WithPassword);

    private static ConcurrencyLimiter SharedLimiter(int processorsPerPermit, int queuedPerPermit)
    {
        var permits = Math.Max(1, Environment.ProcessorCount / processorsPerPermit);

        return new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = permits,
            QueueLimit = permits * queuedPerPermit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    }

    private static ConcurrencyLimiter OneRequestAtATime() =>
        new(new ConcurrencyLimiterOptions { PermitLimit = 1, QueueLimit = 0 });
}
