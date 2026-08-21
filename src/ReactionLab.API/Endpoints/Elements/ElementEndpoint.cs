using ReactionLab.API.Http;
using ReactionLab.Application.Features.Elements.Contracts;
using ReactionLab.Application.Features.Elements.GetElementById;

namespace ReactionLab.API.Endpoints.Elements;

internal static class ElementEndpoint
{
    public static RouteGroupBuilder MapElementEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/elements").WithTags("Elements");

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

        return api;
    }
}
