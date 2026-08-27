using ReactionLab.API.Http;
using ReactionLab.Application.Features.Elements.Contracts;
using ReactionLab.Application.Features.Elements.GetElementById;
using ReactionLab.Application.Features.Elements.GetElementBySymbol;
using ReactionLab.Application.Features.Elements.ListElements;
using ReactionLab.Application.Features.Elements.TranslateElement;

namespace ReactionLab.API.Endpoints;

internal static class ElementEndpoints
{
    public static RouteGroupBuilder MapElementEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/elements")
            .WithTags("Elements")
            .WithLocaleHeaders();

        group.MapGet("/", async (
            string? q,
            HttpContext httpContext,
            ListElementsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListElementsQuery(q, httpContext.ResolveLocale());
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListElements")
        .WithSummary("The periodic table, or the elements matching q.")
        .Produces<IReadOnlyList<ElementSummaryResponse>>();

        group.MapGet("/symbol/{symbol}", async (
            string symbol,
            HttpContext httpContext,
            GetElementBySymbolHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetElementBySymbolQuery(symbol, httpContext.ResolveLocale());
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("GetElementBySymbol")
        .WithSummary("One element by its chemical symbol, case-insensitively.")
        .Produces<ElementResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            GetElementByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetElementByIdQuery(id, httpContext.ResolveLocale());
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("GetElementById")
        .WithSummary("Get one element by id")
        .Produces<ElementResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/translations/{locale}", async (
            Guid id,
            string locale,
            TranslateElementRequest request,
            TranslateElementHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new TranslateElementCommand(
                id, locale, request.Name, request.DiscoveryInfo, request.InterestingFacts);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("TranslateElement")
        .WithSummary("Add or replace one locale's text for an element.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);


        return api;
    }
}
