using ReactionLab.API.Http;
using ReactionLab.Application.Features.Chemistry.GetChemistryReference;

namespace ReactionLab.API.Endpoints;

internal static class ChemistryEndpoints
{
    public static RouteGroupBuilder MapChemistryEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/chemistry").WithTags("Chemistry");

        group.MapGet("/reference", async (
            GetChemistryReferenceHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetChemistryReferenceQuery();
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToRawJsonResult();
        })
        .WithName("GetChemistryReference")
        .WithSummary("Every curated dataset the chemistry engine needs, keyed by dataset name.")
        .Produces(StatusCodes.Status200OK, contentType: "application/json");

        return group;
    }
}
