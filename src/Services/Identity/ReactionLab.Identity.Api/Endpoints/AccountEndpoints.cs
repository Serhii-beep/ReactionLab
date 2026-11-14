using ReactionLab.Identity.Api.Features.Accounts.Contracts;
using ReactionLab.Identity.Api.Features.Accounts.RegisterAccount;
using ReactionLab.Identity.Api.Features.Accounts.RequestEmailConfirmation;
using ReactionLab.Identity.Api.Features.Accounts.RequestPasswordReset;
using ReactionLab.Identity.Api.Features.Accounts.ResetPassword;
using ReactionLab.Identity.Api.Features.Accounts.SchoolSignIn;
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
                request.Locale ?? httpContext.ResolveLocale().Code,
                request.TimeZone);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("RegisterAccount")
        .WithSummary("Create an account.")
        .RequireRateLimiting(RateLimitPolicies.Registration)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/email-confirmation-requests", async (
            RequestEmailConfirmationRequest request,
            RequestEmailConfirmationHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RequestEmailConfirmationCommand(request.Email);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("RequestEmailConfirmation")
        .WithSummary("Send a new confirmation link to an address whose account isn't confirmed.")
        .RequireRateLimiting(RateLimitPolicies.EmailConfirmationRequest)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/password-reset-requests", async (
            RequestPasswordResetRequest request,
            RequestPasswordResetHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RequestPasswordResetCommand(request.Email);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("RequestPasswordReset")
        .WithSummary("Mail a link that sets a new password to the account at an address, if one is there.")
        .RequireRateLimiting(RateLimitPolicies.PasswordResetRequest)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/password-resets", async (
            ResetPasswordRequest request,
            HttpContext httpContext,
            ResetPasswordHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ResetPasswordCommand(
                request.Token,
                request.NewPassword,
                httpContext.Request.Headers.UserAgent.ToString());

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ResetPassword")
        .WithSummary("Set a new password through a mailed reset link, which signs every session out.")
        .WithNoStore()
        .RequireRateLimiting(RateLimitPolicies.PasswordReset)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/sign-in", async (
            SignInRequest request,
            SignInHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SignInCommand(request.Email, request.Password, request.EmailConfirmationToken);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("SignIn")
        .WithSummary("Sign in with an email address and a password")
        .RequireRateLimiting(RateLimitPolicies.SignIn)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/school-sign-in", async (
            SchoolSignInRequest request,
            SchoolSignInHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SchoolSignInCommand(request.SignInCode, request.Username, request.Password);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("SchoolSignIn")
        .WithSummary("Sign in to a school account with the school's sign-in code, a username, and a password")
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
