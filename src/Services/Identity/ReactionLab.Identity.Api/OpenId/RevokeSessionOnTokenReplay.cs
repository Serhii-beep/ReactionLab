using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;
using static OpenIddict.Server.OpenIddictServerHandlerFilters;

namespace ReactionLab.Identity.Api.OpenId;

internal sealed class RevokeSessionOnTokenReplay(
    IOpenIddictTokenManager tokenManager,
    IOpenIddictAuthorizationManager authorizationManager,
    ILogger<RevokeSessionOnTokenReplay> logger) : IOpenIddictServerHandler<ValidateTokenContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ValidateTokenContext>()
            .AddFilter<RequireTokenIdResolved>()
            .UseScopedHandler<RevokeSessionOnTokenReplay>()
            .SetOrder(OpenIddictServerHandlers.Protection.ValidateTokenEntry.Descriptor.Order - 1)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    public async ValueTask HandleAsync(ValidateTokenContext context)
    {
        if (string.IsNullOrEmpty(context.AuthorizationId)
            || await tokenManager.FindByIdAsync(context.TokenId!) is not { } token
            || !await tokenManager.HasStatusAsync(token, Statuses.Redeemed)
            || await IsWithinReuseLeewayAsync(context, token)
            || await authorizationManager.FindByIdAsync(context.AuthorizationId) is not { } session
            || !await authorizationManager.HasStatusAsync(session, Statuses.Valid))
        {
            return;
        }

        if (await authorizationManager.TryRevokeAsync(session, CancellationToken.None))
        {
            logger.LogWarning(
                "Ended session {SessionId}: one of its tokens was used again after its redemption.",
                context.AuthorizationId);
        }
    }

    private async Task<bool> IsWithinReuseLeewayAsync(ValidateTokenContext context, object token) =>
        context.Principal!.HasTokenType(TokenTypeIdentifiers.RefreshToken)
        && context.Options.RefreshTokenReuseLeeway is { } leeway
        && (await tokenManager.GetRedemptionDateAsync(token) is not { } redeemedAt
            || context.Options.TimeProvider.GetUtcNow() < redeemedAt + leeway);
}
