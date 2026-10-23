using System.Security.Claims;
using ReactionLab.Identity.Api.Features.Accounts.ChangePassword;
using ReactionLab.Identity.Api.Features.Accounts.Contracts;
using ReactionLab.Identity.Api.Features.Accounts.GetProfile;
using ReactionLab.Identity.Api.Features.Accounts.Reauthenticate;
using ReactionLab.Identity.Api.Features.Accounts.RemovePassword;
using ReactionLab.Identity.Api.Features.Accounts.SetPassword;
using ReactionLab.Identity.Api.Features.Accounts.UpdateProfile;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class AccountSettingsEndpoints
{
    public static RouteGroupBuilder MapAccountSettingsEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/account")
            .WithTags("Account")
            .RequireAuthorization(AuthorizationPolicies.AccessToken)
            .WithNoStore();

        group.MapGet("/profile", async (
            ClaimsPrincipal user,
            GetProfileHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProfileQuery(user.GetAccountId());
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("GetProfile")
        .WithSummary("Read the account's profile.")
        .Produces<ProfileResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/profile", async (
            UpdateProfileRequest request,
            ClaimsPrincipal user,
            UpdateProfileHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateProfileCommand(user.GetAccountId(), request.DisplayName, request.Locale);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("UpdateProfile")
        .WithSummary("Change the display name, the preferred locale, or both.")
        .Produces<ProfileResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/password-changes", async (
            ChangePasswordRequest request,
            ClaimsPrincipal user,
            ChangePasswordHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ChangePasswordCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                user.GetSessionSignIn(),
                request.CurrentPassword,
                request.NewPassword);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ChangePassword")
        .WithSummary("Change the password, and sign every other session out.")
        .RequireRateLimiting(RateLimitPolicies.PasswordChange)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/reauthentications", async (
            ReauthenticateRequest request,
            ClaimsPrincipal user,
            ReauthenticateHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ReauthenticateCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                user.GetSessionSignIn(),
                request.CurrentPassword);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("Reauthenticate")
        .WithSummary("Confirm the password in this session, which privileged actions accept for 10 minutes.")
        .RequireRateLimiting(RateLimitPolicies.Reauthentication)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPut("/password", async (
            SetPasswordRequest request,
            ClaimsPrincipal user,
            SetPasswordHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SetPasswordCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                user.GetSessionSignIn(),
                request.NewPassword);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("SetPassword")
        .WithSummary("Set a password on an account that has none.")
        .RequireRateLimiting(RateLimitPolicies.PasswordChange)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapDelete("/password", async (
            ClaimsPrincipal user,
            RemovePasswordHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RemovePasswordCommand(
                user.GetAccountId(),
                user.GetSessionId(),
                user.GetSessionSignIn());

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("RemovePassword")
        .WithSummary("Remove the password, keeping a way to sign in, and sign every other session out.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }
}
