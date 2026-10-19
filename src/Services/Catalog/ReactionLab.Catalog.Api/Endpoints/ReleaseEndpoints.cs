using ReactionLab.Catalog.Api.Http;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Features.Reactions.Contracts;
using ReactionLab.Catalog.Application.Features.Releases;
using ReactionLab.Catalog.Application.Features.Releases.Reads;
using ReactionLab.Catalog.Application.Features.Substances.Contracts;
using ReactionLab.SharedKernel;
using ReactionLab.SharedKernel.Pagination;
using ReactionLab.Web;

namespace ReactionLab.Catalog.Api.Endpoints;

internal static class ReleaseEndpoints
{
    public static IEndpointRouteBuilder MapReleaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/catalog/v1/releases/{release:guid}/{locale}")
            .WithTags("Catalog releases")
            .ResolvesCatalogRelease();

        group.MapGet("/substances", async (
            string? q,
            string? element,
            string? cursor,
            HttpContext httpContext,
            SearchReleaseSubstancesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new SearchReleaseSubstancesQuery(httpContext.CatalogRelease(), q, element, cursor);

            return (await handler.HandleAsync(query, cancellationToken)).ToHttpResult();
        })
        .TakesQuery("q", "element", "cursor")
        .WithName("SearchReleaseSubstances")
        .WithSummary("Search one release's substances, or list them by prominence, optionally for one element.")
        .Produces<CursorPagedResult<SubstanceSummaryResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/substances/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            GetReleaseSubstanceHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetReleaseSubstanceQuery(httpContext.CatalogRelease(), id);

            return (await handler.HandleAsync(query, cancellationToken)).ToHttpResult();
        })
        .TakesQuery()
        .WithName("GetReleaseSubstance")
        .WithSummary("One substance of a release, with its 3D structure.")
        .Produces<SubstanceResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/substances/by-structure/{key}", async (
            string key,
            HttpContext httpContext,
            FindReleaseSubstancesByStructureHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new FindReleaseSubstancesByStructureQuery(httpContext.CatalogRelease(), key);

            return (await handler.HandleAsync(query, cancellationToken)).ToHttpResult();
        })
        .TakesQuery()
        .WithName("FindReleaseSubstancesByStructure")
        .WithSummary("The substances whose constitution has this structure key.")
        .Produces<IReadOnlyList<SubstanceResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/reactions", async (
            string available,
            string match,
            HttpContext httpContext,
            MatchReleaseReactionsHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!TryReadBench(available, out var bench))
            {
                return Result.Failure(ReleaseErrors.BenchNotCanonical).ToHttpResult();
            }

            if (!Enum.TryParse<ReactantMatch>(match, ignoreCase: false, out var mode)
                || !string.Equals(mode.ToString(), match, StringComparison.Ordinal))
            {
                return Result.Failure(Error.Validation(
                    "Release.MatchUnknown", "Match must be Partial or Complete.", field: "match")).ToHttpResult();
            }

            var query = new MatchReleaseReactionsQuery(httpContext.CatalogRelease(), bench, mode);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.IsFailure && result.Error == ReleaseErrors.NotHeld
                ? CatalogReleaseRequests.Unavailable(httpContext.Response)
                : result.ToHttpResult();
        })
        .TakesQuery("available", "match")
        .RequireRateLimiting(RateLimitPolicies.Matching)
        .WithName("MatchReleaseReactions")
        .WithSummary("The reactions possible with the available substances, nearest first.")
        .Produces<CursorPagedResult<ReactionSummaryResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/reactions/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            GetReleaseReactionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetReleaseReactionQuery(httpContext.CatalogRelease(), id);

            return (await handler.HandleAsync(query, cancellationToken)).ToHttpResult();
        })
        .TakesQuery()
        .WithName("GetReleaseReaction")
        .WithSummary("One reaction of a release, with its participants, energetics and conditions.")
        .Produces<ReactionResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static bool TryReadBench(string available, out IReadOnlyList<Guid> bench)
    {
        var ids = new List<Guid>();

        foreach (var part in available.Split(','))
        {
            if (!Guid.TryParseExact(part, "D", out var id) || !string.Equals(id.ToString("D"), part, StringComparison.Ordinal))
            {
                bench = [];

                return false;
            }

            ids.Add(id);
        }

        bench = ids;

        return true;
    }
}
