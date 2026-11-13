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
    IOptions<NotificationsClientOptions> notificationsOptions,
    ILogger<OpenIdRegistration> logger)
{
    public async Task RegisterAsync(CancellationToken cancellationToken)
    {
        await RegisterScopeAsync(
            new OpenIddictScopeDescriptor
            {
                Name = OpenIdNames.ApiScope,
                DisplayName = "ReactionLab APIs",
                Resources = { OpenIdNames.IdentityAudience, OpenIdNames.CatalogAudience }
            },
            cancellationToken);

        await RegisterScopeAsync(
            new OpenIddictScopeDescriptor
            {
                Name = OpenIdNames.ContactsScope,
                DisplayName = "Account contacts",
                Resources = { OpenIdNames.IdentityAudience }
            },
            cancellationToken);

        await RegisterScopeAsync(
            new OpenIddictScopeDescriptor
            {
                Name = OpenIdNames.SecurityLinksScope,
                DisplayName = "Security links",
                Resources = { OpenIdNames.IdentityAudience }
            },
            cancellationToken);

        await RegisterClientAsync(DescribeGateway(gatewayOptions.Value), cancellationToken);
        await RegisterClientAsync(DescribeNotifications(notificationsOptions.Value), cancellationToken);
    }

    private static OpenIddictApplicationDescriptor DescribeGateway(GatewayClientOptions gateway) =>
        new()
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

    private static OpenIddictApplicationDescriptor DescribeNotifications(NotificationsClientOptions notifications) =>
        new()
        {
            ClientId = OpenIdNames.NotificationsClientId,
            ClientSecret = notifications.Secret,
            ClientType = ClientTypes.Confidential,
            DisplayName = "ReactionLab notifications",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + OpenIdNames.ContactsScope,
                Permissions.Prefixes.Scope + OpenIdNames.SecurityLinksScope
            }
        };

    private async Task RegisterScopeAsync(OpenIddictScopeDescriptor descriptor, CancellationToken cancellationToken)
    {
        if (await scopeManager.FindByNameAsync(descriptor.Name!, cancellationToken) is { } scope)
        {
            await scopeManager.UpdateAsync(scope, descriptor, cancellationToken);
        }
        else
        {
            await scopeManager.CreateAsync(descriptor, cancellationToken);
        }

        logger.LogInformation("Registered the OpenID scope {Scope}.", descriptor.Name);
    }

    private async Task RegisterClientAsync(
        OpenIddictApplicationDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        if (await applicationManager.FindByClientIdAsync(descriptor.ClientId!, cancellationToken) is { } application)
        {
            await applicationManager.UpdateAsync(application, descriptor, cancellationToken);
        }
        else
        {
            await applicationManager.CreateAsync(descriptor, cancellationToken);
        }

        logger.LogInformation("Registered the OpenID client {ClientId}.", descriptor.ClientId);
    }
}
