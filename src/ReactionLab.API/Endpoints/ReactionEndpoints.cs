using ReactionLab.API.Http;
using ReactionLab.Application.Common.Pagination;
using ReactionLab.Application.Features.Reactions.Contracts;
using ReactionLab.Application.Features.Reactions.GetReactionById;
using ReactionLab.Application.Features.Reactions.ListReactions;

namespace ReactionLab.API.Endpoints;

internal static class ReactionEndpoints
{
    public static RouteGroupBuilder MapReactionEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/reactions").WithTags("Reactions");

        group.MapGet("/", async (
            string? q,
            Guid[] available,
            [AsParameters] CursorRequest page,
            HttpContext httpContext,
            ListReactionsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListReactionsQuery(q, available, page, httpContext.ResolveLocale());
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListReactions")
        .WithSummary("Browse or search reactions.")
        .Produces<CursorPagedResult<ReactionSummaryResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            GetReactionByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetReactionByIdQuery(id, httpContext.ResolveLocale());
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("GetReactionById")
        .WithSummary("One reaction with its participants, energetics, and conditions.")
        .Produces<ReactionResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }
}
