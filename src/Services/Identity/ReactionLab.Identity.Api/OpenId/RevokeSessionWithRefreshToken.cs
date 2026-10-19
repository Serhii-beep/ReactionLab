using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace ReactionLab.Identity.Api.OpenId;

internal sealed class RevokeSessionWithRefreshToken(SessionRevocation sessionRevocation)
    : IOpenIddictServerHandler<HandleRevocationRequestContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<HandleRevocationRequestContext>()
            .UseScopedHandler<RevokeSessionWithRefreshToken>()
            .SetOrder(OpenIddictServerHandlers.Revocation.RevokeToken.Descriptor.Order - 500)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    public async ValueTask HandleAsync(HandleRevocationRequestContext context)
    {
        if (context.GenericTokenPrincipal is { } principal
            && principal.GetTokenType() is TokenTypeIdentifiers.RefreshToken
            && principal.GetAuthorizationId() is { } sessionId)
        {
            await sessionRevocation.RevokeAsync(sessionId, CancellationToken.None);
        }
    }
}
