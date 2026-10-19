using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using ReactionLab.Identity.Persistence.OpenId;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Migrator;

internal sealed class OpenIdRegistration(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    IOptions<GatewayClientOptions> gatewayOptions,
    ILogger<OpenIdRegistration> logger)
{
    public async Task RegisterAsync(CancellationToken cancellationToken)
    {
        await RegisterApiScopeAsync(cancellationToken);
        await RegisterGatewayAsync(cancellationToken);
    }

    private async Task RegisterApiScopeAsync(CancellationToken cancellationToken)
    {
        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = OpenIdNames.ApiScope,
            DisplayName = "ReactionLab APIs",
            Resources = { OpenIdNames.IdentityAudience, OpenIdNames.CatalogAudience }
        };

        if (await scopeManager.FindByNameAsync(OpenIdNames.ApiScope, cancellationToken) is { } scope)
        {
            await scopeManager.UpdateAsync(scope, descriptor, cancellationToken);
        }
        else
        {
            await scopeManager.CreateAsync(descriptor, cancellationToken);
        }

        logger.LogInformation("Registered the OpenID scope {Scope}.", OpenIdNames.ApiScope);
    }

    private async Task RegisterGatewayAsync(CancellationToken cancellationToken)
    {
        var gateway = gatewayOptions.Value;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = OpenIdNames.GatewayClientId,
            ClientSecret = gateway.Secret,
            ClientType = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "ReactionLab gateway",
            RedirectUris = { gateway.RedirectUri },
            PostLogoutRedirectUris = { gateway.PostLogoutRedirectUri },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.PushedAuthorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Revocation,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Profile,
                Permissions.Prefixes.Scope + OpenIdNames.ApiScope
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange,
                Requirements.Features.PushedAuthorizationRequests
            }
        };

        if (await applicationManager.FindByClientIdAsync(OpenIdNames.GatewayClientId, cancellationToken) is { } application)
        {
            await applicationManager.UpdateAsync(application, descriptor, cancellationToken);
        }
        else
        {
            await applicationManager.CreateAsync(descriptor, cancellationToken);
        }

        logger.LogInformation("Registered the OpenID client {ClientId}.", OpenIdNames.GatewayClientId);
    }
}
