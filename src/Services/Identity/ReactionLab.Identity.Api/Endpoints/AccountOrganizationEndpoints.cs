using System.Security.Claims;
using ReactionLab.Identity.Api.Features.Accounts.Contracts;
using ReactionLab.Identity.Api.Features.Accounts.ListAccountOrganizations;
using ReactionLab.Identity.Api.Features.Accounts.SwitchActiveOrganization;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class AccountOrganizationEndpoints
{
    public static RouteGroupBuilder MapAccountOrganizationEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/account")
            .WithTags("Account")
            .RequireAuthorization(AuthorizationPolicies.AccessToken)
            .WithNoStore();

        group.MapGet("/organizations", async (
            ClaimsPrincipal user,
            ListAccountOrganizationsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListAccountOrganizationsQuery(user.GetAccountId(), user.GetSessionId());
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListAccountOrganizations")
        .WithSummary("List the organizations where the account holds roles, with the session's active one.")
        .Produces<IReadOnlyList<AccountOrganizationResponse>>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/active-organization", async (
            SwitchActiveOrganizationRequest request,
            ClaimsPrincipal user,
            SwitchActiveOrganizationHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SwitchActiveOrganizationCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                request.OrganizationId);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("SwitchActiveOrganization")
        .WithSummary("Make another of the account's organizations the session's; the next refresh carries it.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }
}
