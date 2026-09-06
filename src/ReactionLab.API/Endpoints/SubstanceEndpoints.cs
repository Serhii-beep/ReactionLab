using ReactionLab.API.Http;
using ReactionLab.Application.Common.Pagination;
using ReactionLab.Application.Features.Substances.Contracts;
using ReactionLab.Application.Features.Substances.GetSubstanceById;
using ReactionLab.Application.Features.Substances.ListSubstances;

namespace ReactionLab.API.Endpoints;

internal static class SubstanceEndpoints
{
    public static RouteGroupBuilder MapSubstanceEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/substances")
            .WithTags("Substances")
            .WithLocaleHeaders();

        group.MapGet("/", async (
            string? q,
            string? element,
            [AsParameters] CursorRequest page,
            HttpContext httpContext,
            ListSubstancesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListSubstancesQuery(q, element, page, httpContext.ResolveLocale());
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListSubstances")
        .WithSummary("Browse the substances catalog, search it with q, or narrow it to one element.")
        .Produces<CursorPagedResult<SubstanceSummaryResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            GetSubstanceByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetSubstanceByIdQuery(id, httpContext.ResolveLocale());
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("GetSubstanceById")
        .WithSummary("One substance with its 3D structure.")
        .Produces<SubstanceResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }
}
