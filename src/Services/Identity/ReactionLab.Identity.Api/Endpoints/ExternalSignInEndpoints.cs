using ReactionLab.Identity.Api.Features.ExternalSignIn.CompleteExternalSignIn;
using ReactionLab.Identity.Api.Features.ExternalSignIn.Contracts;
using ReactionLab.Identity.Api.Features.ExternalSignIn.GetExternalIdentity;
using ReactionLab.Identity.Api.Features.ExternalSignIn.LinkExternalSignIn;
using ReactionLab.Identity.Api.Features.ExternalSignIn.ListSignInMethods;
using ReactionLab.Identity.Api.Features.ExternalSignIn.RegisterExternalAccount;
using ReactionLab.Identity.Api.Features.ExternalSignIn.StartExternalSignIn;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Web;

namespace ReactionLab.Identity.Api.Endpoints;

internal static class ExternalSignInEndpoints
{
    public static RouteGroupBuilder MapExternalSignInEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/account/external-sign-ins/{provider}/callback", (
            string provider,
            HttpContext httpContext,
            CompleteExternalSignInHandler handler) => handler.HandleAsync(httpContext, provider))
        .ExcludeFromDescription();

        var group = api.MapGroup("/account")
            .WithTags("Account")
            .WithNoStore();

        group.MapGet("/sign-in-methods", async (
            ListSignInMethodsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new ListSignInMethodsQuery(), cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("ListSignInMethods")
        .WithSummary("List the providers the sign-in page offers besides a password.")
        .Produces<SignInMethodsResponse>();

        group.MapGet("/external-sign-ins/{provider}", (
            string provider,
            string? returnUrl,
            StartExternalSignInHandler handler) => handler.Handle(provider, returnUrl))
        .WithName("StartExternalSignIn")
        .WithSummary("Send the browser to a sign-in provider.")
        .Produces(StatusCodes.Status302Found)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/external-identity", async (
            GetExternalIdentityHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new GetExternalIdentityQuery(), cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("GetExternalIdentity")
        .WithSummary("Read the provider's account that waits for a sign-up or a link.")
        .Produces<ExternalIdentityResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/external-registrations", async (
            RegisterExternalAccountRequest request,
            HttpContext httpContext,
            RegisterExternalAccountHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterExternalAccountCommand(
                request.DisplayName,
                request.BirthYear,
                request.Locale ?? httpContext.ResolveLocale().Code);

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("RegisterExternalAccount")
        .WithSummary("Create an account for the provider's account, and sign in.")
        .RequireRateLimiting(RateLimitPolicies.ExternalRegistration)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/external-links", async (
            LinkExternalSignInRequest request,
            LinkExternalSignInHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new LinkExternalSignInCommand(request.Password);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.ToHttpResult();
        })
        .WithName("LinkExternalSignIn")
        .WithSummary("Connect the provider's account to the account with its address, by that account's password.")
        .RequireRateLimiting(RateLimitPolicies.SignIn)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return api;
    }
}
