using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using ReactionLab.Identity.Api.OpenId;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ReactionLab.Identity.Api.Features.OpenId;

internal sealed class EndSessionEndpointHandler(SessionRevocation sessionRevocation)
{
    public async Task<IResult> HandleAsync(HttpContext httpContext)
    {
        var hint = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (hint.Principal?.GetClaim(JwtRegisteredClaimNames.Sid) is not { } sessionId)
        {
            return Results.BadRequest();
        }

        await sessionRevocation.RevokeAsync(sessionId, CancellationToken.None);

        var signIn = await httpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (signIn.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) == hint.Principal.GetClaim(Claims.Subject))
        {
            await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }

        return Results.SignOut(authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
    }
}
