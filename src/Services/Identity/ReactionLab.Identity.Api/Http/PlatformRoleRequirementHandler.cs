using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.PlatformRoles;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Api.Http;

internal sealed class PlatformRoleRequirementHandler(AppDbContext context)
    : AuthorizationHandler<PlatformRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorization,
        PlatformRoleRequirement requirement)
    {
        if (Guid.TryParse(authorization.User.GetClaim(Claims.Subject), out var accountId)
            && await context.HoldsPlatformRoleAsync(accountId, requirement.Role, CancellationToken.None))
        {
            authorization.Succeed(requirement);
        }
    }
}
