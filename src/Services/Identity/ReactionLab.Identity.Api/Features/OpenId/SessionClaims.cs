using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.Identity.Persistence.PlatformRoles;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Api.Features.OpenId;

internal sealed class SessionClaims(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    AppDbContext context,
    IOptions<IdentityOptions> identityOptions,
    TimeProvider timeProvider)
{
    private const string PlatformRoleClaimType = "platform_role";

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

        return await DescribeAsync(identity, user);
    }

    public async Task<ClaimsIdentity?> RenewAsync(ClaimsPrincipal principal)
    {
        var remaining = RemainingLifetime(principal);

        if (remaining <= TimeSpan.Zero
            || await userManager.FindByIdAsync(principal.GetClaim(Claims.Subject) ?? string.Empty) is not { } user
            || !await signInManager.CanSignInAsync(user))
        {
            return null;
        }

        var securityStamp = await userManager.GetSecurityStampAsync(user);

        if (principal.GetClaim(SecurityStampClaimType) != securityStamp)
        {
            if (await CarryOverStampChangeAsync(principal, user.Id) is not { } carriedOverStamp)
            {
                return null;
            }

            securityStamp = carriedOverStamp;
        }

        var identity = new ClaimsIdentity(
            principal.Claims,
            TokenValidationParameters.DefaultAuthenticationType,
            Claims.Name,
            Claims.Role);

        identity.SetClaim(SecurityStampClaimType, securityStamp)
            .SetRefreshTokenLifetime(remaining < SessionPolicy.IdleTimeout ? remaining : null)
            .SetAccessTokenLifetime(remaining < SessionPolicy.AccessTokenLifetime ? remaining : null);

        return await DescribeAsync(identity, user);
    }

    private TimeSpan RemainingLifetime(ClaimsPrincipal principal) =>
        long.TryParse(principal.GetClaim(Claims.AuthenticationTime), out var signedInAt)
            ? DateTimeOffset.FromUnixTimeSeconds(signedInAt) + SessionPolicy.MaximumLifetime - timeProvider.GetUtcNow()
            : TimeSpan.Zero;

    private async Task<string?> CarryOverStampChangeAsync(ClaimsPrincipal principal, Guid userId)
    {
        if (!Guid.TryParse(principal.GetAuthorizationId(), out var sessionId)
            || principal.GetTokenId() is not { } tokenId)
        {
            return null;
        }

        return await context.CarrySessionOverStampChangeAsync(userId, sessionId, tokenId)
            ?? await context.CarrySessionOverStampChangeAsync(userId, sessionId, tokenId);
    }

    private async Task<ClaimsIdentity> DescribeAsync(ClaimsIdentity identity, User user)
    {
        ImmutableArray<string> platformRoles =
            [.. await context.ListPlatformRolesAsync(user.Id, CancellationToken.None)];

        return identity.SetClaim(Claims.Name, user.DisplayName)
            .SetClaim(Claims.Locale, user.PreferredLocale)
            .SetClaims(PlatformRoleClaimType, platformRoles)
            .SetDestinations(GetDestinations);
    }

    private static IEnumerable<string> GetDestinations(Claim claim) => claim.Type switch
    {
        Claims.Subject or JwtRegisteredClaimNames.Sid or Claims.AuthenticationMethodReference =>
            [Destinations.AccessToken, Destinations.IdentityToken],
        Claims.AuthenticationTime or PlatformRoleClaimType => [Destinations.AccessToken, Destinations.IdentityToken],
        Claims.Name or Claims.Locale when claim.Subject?.HasScope(Scopes.Profile) is true =>
            [Destinations.IdentityToken],
        _ => []
    };
}
