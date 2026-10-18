using ReactionLab.Identity.Api.Features.Accounts.Contracts;
using ReactionLab.Identity.Api.Features.Accounts.RegisterAccount;
using ReactionLab.Identity.Api.Features.Accounts.SignIn;
using ReactionLab.Identity.Api.Features.Accounts.SignOut;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccountEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/account")
            .WithTags("Account");

        group.MapPost("/registrations", async (
            RegisterAccountRequest request,
            HttpContext httpContext,
            RegisterAccountHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterAccountCommand(
                request.Email,
                request.Password,
                request.DisplayName,
                request.BirthYear,
                request.Locale ?? httpContext.ResolveLocale().Code);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("RegisterAccount")
        .WithSummary("Create an account.")
        .RequireRateLimiting(RateLimitPolicies.Registration)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/sign-in", async (
            SignInRequest request,
            SignInHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SignInCommand(request.Email, request.Password);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("SignIn")
        .WithSummary("Sign in with an email address and a password")
        .RequireRateLimiting(RateLimitPolicies.SignIn)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/sign-out", async (
            SignOutHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SignOutCommand();
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("SignOut")
        .WithSummary("End Identity's own cookie session.")
        .Produces(StatusCodes.Status204NoContent);

        return api;
    }
}
