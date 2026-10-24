using System.Security.Claims;
using ReactionLab.Identity.Api.Features.PlatformRoles.Contracts;
using ReactionLab.Identity.Api.Features.PlatformRoles.GrantPlatformRole;
using ReactionLab.Identity.Api.Features.PlatformRoles.ListPlatformRoleAccounts;
using ReactionLab.Identity.Api.Features.PlatformRoles.ListPlatformRoleChanges;
using ReactionLab.Identity.Api.Features.PlatformRoles.LookUpPlatformRoleAccount;
using ReactionLab.Identity.Api.Features.PlatformRoles.RevokePlatformRole;
using ReactionLab.Identity.Api.Http;
using ReactionLab.SharedKernel.Pagination;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class PlatformRoleEndpoints
{
    public static RouteGroupBuilder MapPlatformRoleEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/platform-roles")
            .WithTags("Platform roles")
            .RequireAuthorization(AuthorizationPolicies.PlatformAdmin)
            .WithNoStore();

        group.MapGet("/accounts", async (
            [AsParameters] CursorRequest page,
            ListPlatformRoleAccountsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListPlatformRoleAccountsQuery(page);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListPlatformRoleAccounts")
        .WithSummary("List the accounts that hold a platform role, with their roles. For platform admins.")
        .Produces<CursorPagedResult<PlatformRoleAccountResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/account-lookups", async (
            PlatformRoleAccountLookupRequest request,
            ClaimsPrincipal user,
            LookUpPlatformRoleAccountHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new LookUpPlatformRoleAccountQuery(user.GetAccountId(), user.GetSessionId(), request.Email);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("LookUpPlatformRoleAccount")
        .WithSummary("Find an account by email. For platform admins who confirmed their password in this session.")
        .Produces<PlatformRoleAccountResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{role}/accounts/{accountId:guid}", async (
            string role,
            Guid accountId,
            ClaimsPrincipal user,
            GrantPlatformRoleHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new GrantPlatformRoleCommand(user.GetAccountId(), user.GetSessionId(), accountId, role);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("GrantPlatformRole")
        .WithSummary("Grant a platform role. For platform admins who confirmed their password in this session.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{role}/accounts/{accountId:guid}", async (
            string role,
            Guid accountId,
            ClaimsPrincipal user,
            RevokePlatformRoleHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RevokePlatformRoleCommand(user.GetAccountId(), user.GetSessionId(), accountId, role);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("RevokePlatformRole")
        .WithSummary("Revoke a platform role. For platform admins who confirmed their password in this session.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/changes", async (
            Guid? accountId,
            [AsParameters] CursorRequest page,
            ListPlatformRoleChangesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListPlatformRoleChangesQuery(accountId, page);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListPlatformRoleChanges")
        .WithSummary("List every grant and revocation of a platform role, newest first. For platform admins.")
        .Produces<CursorPagedResult<PlatformRoleChangeResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return api;
    }
}
