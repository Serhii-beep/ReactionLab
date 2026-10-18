using System.Globalization;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Features.Releases;
using ReactionLab.SharedKernel;
using ReactionLab.Web;

namespace ReactionLab.Catalog.Api.Http;

internal static class CatalogReleaseRequests
{
    public const string ReleaseHeader = "Catalog-Release";

    private const string ItemKey = "catalog.release";

    private const string Immutable = "public, max-age=31536000, immutable";

    private const string ShortLived = "public, max-age=60";

    private const string NoStore = "no-store";

    private const int RetryAfterSeconds = 5;

    public static RouteGroupBuilder ResolvesCatalogRelease(this RouteGroupBuilder group) =>
        group.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            var response = httpContext.Response;
            var cacheControl = NoStore;

            response.OnStarting(() =>
            {
                response.Headers.CacheControl = response.StatusCode == StatusCodes.Status200OK ? cacheControl : NoStore;

                return Task.CompletedTask;
            });

            if (UnexpectedParameter(httpContext) is { } parameter)
            {
                return Result.Failure(Error.Validation(
                    "Release.UnknownParameter",
                    "The request carries a query parameter this route does not take, or takes it twice.",
                    field: parameter)).ToHttpResult();
            }

            var code = httpContext.Request.RouteValues["locale"] as string;

            if (SupportedLocale.Create(code) is not { IsSuccess: true } locale
                || !string.Equals(locale.Value.Code, code, StringComparison.Ordinal))
            {
                return Results.StatusCode(StatusCodes.Status404NotFound);
            }

            var releases = httpContext.RequestServices.GetRequiredService<ICatalogReleases>();

            if (!releases.IsReady)
            {
                return Unavailable(response);
            }

            var spelled = (string)httpContext.Request.RouteValues["release"]!;
            var requested = Guid.Parse(spelled);

            if (!string.Equals(requested.ToString("D"), spelled, StringComparison.Ordinal))
            {
                return Result.Failure(Error.Validation(
                    "Release.NotCanonical",
                    "The release id must be written in lowercase with hyphens.",
                    field: "release")).ToHttpResult();
            }

            Guid effective;

            switch (releases.Classify(requested))
            {
                case ReleaseAvailability.Readable:
                    effective = requested;
                    cacheControl = releases.HasCurrentAnswers(requested) ? Immutable : ShortLived;
                    break;

                case ReleaseAvailability.Superseded when releases.ActiveRelease is { } active:
                    effective = active;
                    cacheControl = ShortLived;
                    response.Headers[ReleaseHeader] = active.ToString("D");
                    break;

                case ReleaseAvailability.Unknown:
                    return Results.StatusCode(StatusCodes.Status404NotFound);

                default:
                    return Unavailable(response);
            }

            httpContext.Items[ItemKey] = new ReleaseRequest(effective, locale.Value);

            return await next(context);
        });

    public static RouteHandlerBuilder TakesQuery(this RouteHandlerBuilder builder, params string[] names) =>
        builder.WithMetadata(new AllowedQuery(names.ToHashSet(StringComparer.Ordinal)));

    public static ReleaseRequest CatalogRelease(this HttpContext httpContext) =>
        httpContext.Items[ItemKey] as ReleaseRequest
            ?? throw new InvalidOperationException("The catalog release filter did not run for this endpoint.");

    public static IResult Unavailable(HttpResponse response)
    {
        response.Headers.RetryAfter = RetryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    private static string? UnexpectedParameter(HttpContext httpContext)
    {
        var allowed = httpContext.GetEndpoint()?.Metadata.GetMetadata<AllowedQuery>()?.Names;

        foreach (var (name, values) in httpContext.Request.Query)
        {
            if (allowed is null || !allowed.Contains(name) || values.Count != 1)
            {
                return name;
            }
        }

        return null;
    }

    private sealed record AllowedQuery(IReadOnlySet<string> Names);
}
