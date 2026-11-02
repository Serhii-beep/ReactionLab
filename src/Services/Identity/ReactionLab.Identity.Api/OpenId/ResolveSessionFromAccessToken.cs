using System.Security.Claims;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore;
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
        if (IsServiceCall(context.Transaction))
        {
            if (!IsClientToken(context.Principal))
            {
                context.Reject(error: Errors.InvalidToken, description: "A service call needs a service's own token.");
            }

            return ValueTask.CompletedTask;
        }

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

    private static bool IsServiceCall(OpenIddictValidationTransaction transaction) =>
        transaction.GetHttpRequest()?.HttpContext.GetEndpoint()?.Metadata.GetMetadata<GrpcMethodMetadata>() is not null;

    private static bool IsClientToken(ClaimsPrincipal? principal) =>
        principal is not null
        && principal.GetAuthorizationId() is null
        && principal.GetClaim(JwtRegisteredClaimNames.Sid) is null
        && principal.GetClaim(Claims.Subject) is { Length: > 0 } subject
        && subject == principal.GetClaim(Claims.ClientId);
}
