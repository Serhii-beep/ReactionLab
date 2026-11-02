using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using ReactionLab.Identity.Api.OpenId;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Api.Features.OpenId;

internal sealed class TokenEndpointHandler(
    SessionClaims sessionClaims,
    SessionRevocation sessionRevocation,
    IOpenIddictScopeManager scopeManager,
    ILogger<TokenEndpointHandler> logger)
{
    public async Task<IResult> HandleAsync(HttpContext httpContext)
    {
        var request = httpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsClientCredentialsGrantType())
        {
            return await SignInClientAsync(request, httpContext.RequestAborted);
        }

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            throw new InvalidOperationException($"The grant type '{request.GrantType}' is not supported.");
        }

        var grant = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = grant.Principal
            ?? throw new InvalidOperationException("The grant's principal cannot be retrieved.");

        if (await sessionClaims.RenewAsync(principal) is not { } identity)
        {
            var sessionId = principal.GetAuthorizationId();

            if (sessionId is not null)
            {
                await sessionRevocation.RevokeAsync(sessionId, CancellationToken.None);
            }

            logger.LogInformation(
                "Ended session {SessionId}: its account can no longer sign in, or it reached its maximum lifetime.",
                sessionId);

            return OpenIdResults.Forbid(Errors.InvalidGrant, "The session is no longer valid.");
        }

        return Results.SignIn(
            new ClaimsPrincipal(identity),
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IResult> SignInClientAsync(OpenIddictRequest request, CancellationToken cancellationToken)
    {
        var scopes = request.GetScopes();
        var resources = await scopeManager.ListResourcesAsync(scopes, cancellationToken).ToListAsync(cancellationToken);

        if (request.HasScope(Scopes.OpenId) || resources.Count == 0)
        {
            return OpenIdResults.Forbid(Errors.InvalidScope, "A service asks only for the scopes of its calls.");
        }

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            Claims.Name,
            Claims.Role);

        identity.SetClaim(Claims.Subject, request.ClientId)
            .SetScopes(scopes)
            .SetResources(resources)
            .SetDestinations(static _ => [Destinations.AccessToken]);

        return Results.SignIn(
            new ClaimsPrincipal(identity),
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
