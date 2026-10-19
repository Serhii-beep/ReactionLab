using OpenIddict.Abstractions;

namespace ReactionLab.Identity.Api.OpenId;

internal sealed class SessionRevocation(
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictTokenManager tokenManager)
{
    public async Task RevokeAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (await authorizationManager.FindByIdAsync(sessionId, cancellationToken) is { } authorization)
        {
            await authorizationManager.TryRevokeAsync(authorization, cancellationToken);
        }

        await tokenManager.RevokeByAuthorizationIdAsync(sessionId, cancellationToken);
    }
}
