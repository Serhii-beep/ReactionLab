using Microsoft.AspNetCore.Authentication;
using OpenIddict.Server.AspNetCore;

namespace ReactionLab.Identity.Api.Features.OpenId;

internal static class OpenIdResults
{
    public static IResult Forbid(string error, string description) =>
        Results.Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }),
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
}
