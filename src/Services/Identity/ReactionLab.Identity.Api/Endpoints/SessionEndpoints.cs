using System.Security.Claims;
using ReactionLab.Identity.Api.Features.Sessions.Contracts;
using ReactionLab.Identity.Api.Features.Sessions.ListSessions;
using ReactionLab.Identity.Api.Features.Sessions.RevokeOtherSessions;
using ReactionLab.Identity.Api.Features.Sessions.RevokeSession;
using ReactionLab.Identity.Api.Http;
using ReactionLab.SharedKernel.Pagination;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class SessionEndpoints
{
    public static RouteGroupBuilder MapSessionEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/account/sessions")
            .WithTags("Account")
            .RequireAuthorization(AuthorizationPolicies.AccessToken)
            .WithNoStore();

        group.MapGet("/", async (
            [AsParameters] CursorRequest page,
            ClaimsPrincipal user,
            ListSessionsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListSessionsQuery(user.GetAccountId(), user.GetSessionId(), page);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListSessions")
        .WithSummary("List the account's signed-in sessions, newest first.")
        .Produces<CursorPagedResult<SessionResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{sessionId:guid}", async (
            Guid sessionId,
            ClaimsPrincipal user,
            RevokeSessionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RevokeSessionCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                user.GetSignedInAt(),
                sessionId);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("RevokeSession")
        .WithSummary("Sign one session out. Another session than the caller's needs a recent sign-in.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/", async (
            ClaimsPrincipal user,
            RevokeOtherSessionsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RevokeOtherSessionsCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                user.GetSignedInAt());

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("RevokeOtherSessions")
        .WithSummary("Sign every other session out. Needs a recent sign-in.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return api;
    }
}
