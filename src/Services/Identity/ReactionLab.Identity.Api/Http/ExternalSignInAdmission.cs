using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.ExternalSignIn;

namespace ReactionLab.Identity.Api.Http;

internal static class ExternalSignInAdmission
{
    private const string PathPrefix = "/api/v1/account/external-sign-ins";
    private const string CallbackSuffix = "/callback";
    private const string ProviderRouteValue = "provider";
    private const string ReturnUrlParameter = "returnUrl";
    private const string FetchDestinationHeader = "Sec-Fetch-Dest";
    private const string DocumentDestination = "document";
    private const int StartsInFlightPerCore = 4;
    private const int QueuedStartsPerCore = 4;
    private const int CallbacksInFlightPerCore = 8;
    private const int QueuedCallbacksPerCore = 8;

    public static IApplicationBuilder UseExternalSignInAdmission(this IApplicationBuilder app) =>
        app.UseWhen(
            httpContext => httpContext.Request.Path.StartsWithSegments(PathPrefix),
            branch => branch.Use(RefuseAllButNavigationsAsync)
                .Use(RedirectUntilDiscoveredAsync)
                .UseRateLimiter(CreateLimiterOptions()));

    private static Task RefuseAllButNavigationsAsync(HttpContext httpContext, RequestDelegate next)
    {
        var destination = httpContext.Request.Headers[FetchDestinationHeader].ToString();

        if (destination.Length > 0 && destination != DocumentDestination)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

            return Task.CompletedTask;
        }

        return next(httpContext);
    }

    private static Task RedirectUntilDiscoveredAsync(HttpContext httpContext, RequestDelegate next)
    {
        var discovery = httpContext.RequestServices.GetRequiredService<ProviderDiscovery>();

        if (httpContext.GetRouteValue(ProviderRouteValue) is string providerName && discovery.IsPending(providerName))
        {
            RedirectToSignInPage(httpContext);

            return Task.CompletedTask;
        }

        return next(httpContext);
    }

    private static RateLimiterOptions CreateLimiterOptions()
    {
        var starts = Concurrency(StartsInFlightPerCore, QueuedStartsPerCore);
        var callbacks = Concurrency(CallbacksInFlightPerCore, QueuedCallbacksPerCore);

        return new RateLimiterOptions
        {
            GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, bool>(httpContext =>
                RateLimitPartition.GetConcurrencyLimiter(
                    IsCallback(httpContext),
                    isCallback => isCallback ? callbacks : starts)),
            OnRejected = RedirectToSignInPageAsync
        };
    }

    private static bool IsCallback(HttpContext httpContext) =>
        httpContext.Request.Path.Value?.EndsWith(CallbackSuffix, StringComparison.OrdinalIgnoreCase) is true;

    private static ConcurrencyLimiterOptions Concurrency(int inFlightPerCore, int queuedPerCore) =>
        new()
        {
            PermitLimit = Environment.ProcessorCount * inFlightPerCore,
            QueueLimit = Environment.ProcessorCount * queuedPerCore,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        };

    private static ValueTask RedirectToSignInPageAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        RedirectToSignInPage(context.HttpContext);

        return ValueTask.CompletedTask;
    }

    private static void RedirectToSignInPage(HttpContext httpContext)
    {
        var returnUrl = IsCallback(httpContext)
            ? LocalReturnUrls.Default
            : LocalReturnUrls.OrDefault(httpContext.Request.Query[ReturnUrlParameter]);

        httpContext.Response.Redirect(SignInPageRedirects.WithError(ExternalSignInErrors.UnavailableCode, returnUrl));
    }
}
