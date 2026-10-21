using System.Security.Claims;
using OpenIddict.Abstractions;
using ReactionLab.Identity.Api.Features.Accounts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Api.Http;

internal static class AccessTokenClaims
{
    public static Guid GetAccountId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.GetClaim(Claims.Subject), out var accountId)
            ? accountId
            : throw new InvalidOperationException("The access token carries no account id.");

    public static Guid GetSessionId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.GetAuthorizationId(), out var sessionId)
            ? sessionId
            : throw new InvalidOperationException("The access token carries no session id.");

    public static DateTimeOffset? GetSignedInAt(this ClaimsPrincipal principal) =>
        long.TryParse(principal.GetClaim(Claims.AuthenticationTime), out var signedInAt)
            ? DateTimeOffset.FromUnixTimeSeconds(signedInAt)
            : null;

    public static SessionSignIn GetSessionSignIn(this ClaimsPrincipal principal) =>
        new(
            principal.GetSignedInAt(),
            principal.HasClaim(Claims.AuthenticationMethodReference, AuthenticationMethods.Password));
}
