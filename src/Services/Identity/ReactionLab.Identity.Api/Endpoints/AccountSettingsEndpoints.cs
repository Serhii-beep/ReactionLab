using System.Security.Claims;
using ReactionLab.Identity.Api.Features.Accounts.ChangePassword;
using ReactionLab.Identity.Api.Features.Accounts.Contracts;
using ReactionLab.Identity.Api.Features.Accounts.GetProfile;
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
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return api;
    }
}
