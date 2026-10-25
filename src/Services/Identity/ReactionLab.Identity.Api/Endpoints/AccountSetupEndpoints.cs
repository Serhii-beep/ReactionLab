using ReactionLab.Identity.Api.Features.AccountSetups.CompleteAccountSetup;
using ReactionLab.Identity.Api.Features.AccountSetups.Contracts;
using ReactionLab.Identity.Api.Features.AccountSetups.LookUpAccountSetup;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class AccountSetupEndpoints
{
    public static RouteGroupBuilder MapAccountSetupEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/account")
            .WithTags("Account setup")
            .WithNoStore();

        group.MapPost("/setup-lookups", async (
            AccountSetupLookupRequest request,
            LookUpAccountSetupHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new LookUpAccountSetupQuery(request.Token);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("LookUpAccountSetup")
        .WithSummary("Read whose account a setup link sets up, and where it signs in.")
        .Produces<AccountSetupResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/setups", async (
            CompleteAccountSetupRequest request,
            CompleteAccountSetupHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CompleteAccountSetupCommand(request.Token, request.NewPassword);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("CompleteAccountSetup")
        .WithSummary("Set the account's password through its setup link, which then stops working.")
        .RequireRateLimiting(RateLimitPolicies.AccountSetup)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return api;
    }
}
