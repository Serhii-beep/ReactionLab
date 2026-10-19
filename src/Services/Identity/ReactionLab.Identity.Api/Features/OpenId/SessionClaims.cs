using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Persistence.Accounts;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Api.Features.OpenId;

internal sealed class SessionClaims(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IOptions<IdentityOptions> identityOptions,
    TimeProvider timeProvider)
{
    private string SecurityStampClaimType => identityOptions.Value.ClaimsIdentity.SecurityStampClaimType;

    public async Task<ClaimsIdentity> CreateAsync(
        User user,
        string sessionId,
        DateTimeOffset signedInAt,
        ImmutableArray<string> authenticationMethods,
        ImmutableArray<string> scopes,
        ImmutableArray<string> resources)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id.ToString())
            .SetClaim(JwtRegisteredClaimNames.Sid, sessionId)
            .SetClaim(Claims.AuthenticationTime, signedInAt.ToUnixTimeSeconds())
            .SetClaims(Claims.AuthenticationMethodReference, authenticationMethods)
            .SetClaim(SecurityStampClaimType, await userManager.GetSecurityStampAsync(user))
            .SetScopes(scopes)
            .SetResources(resources)
            .SetAuthorizationId(sessionId);

        return Describe(identity, user);
    }

    public async Task<ClaimsIdentity?> RenewAsync(ClaimsPrincipal principal)
    {
        var remaining = RemainingLifetime(principal);

        if (remaining <= TimeSpan.Zero
            || await userManager.FindByIdAsync(principal.GetClaim(Claims.Subject) ?? string.Empty) is not { } user
            || !await signInManager.CanSignInAsync(user)
            || principal.GetClaim(SecurityStampClaimType) != await userManager.GetSecurityStampAsync(user))
        {
            return null;
        }

        var identity = new ClaimsIdentity(
            principal.Claims,
            TokenValidationParameters.DefaultAuthenticationType,
            Claims.Name,
            Claims.Role);

        identity.SetRefreshTokenLifetime(remaining < SessionPolicy.IdleTimeout ? remaining : null);

        return Describe(identity, user);
    }

    private TimeSpan RemainingLifetime(ClaimsPrincipal principal) =>
        long.TryParse(principal.GetClaim(Claims.AuthenticationTime), out var signedInAt)
            ? DateTimeOffset.FromUnixTimeSeconds(signedInAt) + SessionPolicy.MaximumLifetime - timeProvider.GetUtcNow()
            : TimeSpan.Zero;

    private static ClaimsIdentity Describe(ClaimsIdentity identity, User user) =>
        identity.SetClaim(Claims.Name, user.DisplayName)
            .SetClaim(Claims.Locale, user.PreferredLocale)
            .SetDestinations(GetDestinations);

    private static IEnumerable<string> GetDestinations(Claim claim) => claim.Type switch
    {
        Claims.Subject or JwtRegisteredClaimNames.Sid or Claims.AuthenticationMethodReference =>
            [Destinations.AccessToken, Destinations.IdentityToken],
        Claims.AuthenticationTime => [Destinations.IdentityToken],
        Claims.Name or Claims.Locale when claim.Subject?.HasScope(Scopes.Profile) is true =>
            [Destinations.IdentityToken],
        _ => []
    };
}
