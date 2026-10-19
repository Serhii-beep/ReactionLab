using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;

namespace ReactionLab.Identity.Api.Http;

internal static class AuthorizationPolicies
{
    public const string AccessToken = "access-token";

    public static void Configure(AuthorizationOptions options) =>
        options.AddPolicy(AccessToken, policy => policy
            .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser());
}
