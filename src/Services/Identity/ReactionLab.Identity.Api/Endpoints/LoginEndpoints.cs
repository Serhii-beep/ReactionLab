using System.Security.Claims;
using ReactionLab.Identity.Api.Features.Logins.ConnectLogin;
using ReactionLab.Identity.Api.Features.Logins.Contracts;
using ReactionLab.Identity.Api.Features.Logins.DisconnectLogin;
using ReactionLab.Identity.Api.Features.Logins.ListLogins;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class LoginEndpoints
{
    public static RouteGroupBuilder MapLoginEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/account/logins")
            .WithTags("Account")
            .RequireAuthorization(AuthorizationPolicies.AccessToken)
            .WithNoStore();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            ListLoginsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListLoginsQuery(user.GetAccountId());
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListLogins")
        .WithSummary("List the account's ways to sign in: whether it has a password, and each provider.")
        .Produces<LoginsResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{provider}", (
            string provider,
            ConnectLoginRequest request,
            ClaimsPrincipal user,
            HttpContext httpContext,
            ConnectLoginHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ConnectLoginCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                provider,
                request.ReturnUrl);

            return handler.HandleAsync(httpContext, command, cancellationToken);
        })
        .WithName("ConnectLogin")
        .WithSummary("Start connecting a provider. Answers the provider's address, where the browser goes next.")
        .Produces<ConnectLoginResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapDelete("/{provider}", async (
            string provider,
            ClaimsPrincipal user,
            DisconnectLoginHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DisconnectLoginCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                user.GetSessionSignIn(),
                provider);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("DisconnectLogin")
        .WithSummary("Disconnect a provider, keeping a way to sign in, and sign every other session out.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }
}
