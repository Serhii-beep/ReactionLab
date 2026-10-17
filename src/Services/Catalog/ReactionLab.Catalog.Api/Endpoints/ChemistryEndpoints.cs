using ReactionLab.Catalog.Application.Features.Chemistry.GetChemistryReference;
using ReactionLab.Web;

namespace ReactionLab.Catalog.Api.Endpoints;

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
