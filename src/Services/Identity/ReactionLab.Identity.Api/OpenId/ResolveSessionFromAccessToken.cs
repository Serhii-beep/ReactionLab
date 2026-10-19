using Microsoft.IdentityModel.JsonWebTokens;
using OpenIddict.Abstractions;
using OpenIddict.Validation;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Validation.OpenIddictValidationEvents;

namespace ReactionLab.Identity.Api.OpenId;

internal sealed class ResolveSessionFromAccessToken : IOpenIddictValidationHandler<ValidateTokenContext>
{
    public static OpenIddictValidationHandlerDescriptor Descriptor { get; } =
        OpenIddictValidationHandlerDescriptor.CreateBuilder<ValidateTokenContext>()
            .UseSingletonHandler<ResolveSessionFromAccessToken>()
            .SetOrder(OpenIddictValidationHandlers.Protection.ValidateAuthorizationEntry.Descriptor.Order - 1)
            .SetType(OpenIddictValidationHandlerType.Custom)
            .Build();

    public ValueTask HandleAsync(ValidateTokenContext context)
    {
        if (!string.IsNullOrEmpty(context.AuthorizationId))
        {
            return ValueTask.CompletedTask;
        }

        if (context.Principal?.GetAuthorizationId() is not { Length: > 0 } sessionId
            || sessionId != context.Principal.GetClaim(JwtRegisteredClaimNames.Sid))
        {
            context.Reject(error: Errors.InvalidToken, description: "The access token is not tied to a session.");

            return ValueTask.CompletedTask;
        }

        context.AuthorizationId = sessionId;

        return ValueTask.CompletedTask;
    }
}
