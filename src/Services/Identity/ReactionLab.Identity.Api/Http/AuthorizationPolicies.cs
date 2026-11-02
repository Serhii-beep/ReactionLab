using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.Identity.Persistence.PlatformRoles;

namespace ReactionLab.Identity.Api.Http;

internal static class AuthorizationPolicies
{
    public const string AccessToken = "access-token";
    public const string PlatformAdmin = "platform-admin";
    public const string ContactsReader = "contacts-reader";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(AccessToken, policy => policy
            .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser());

        options.AddPolicy(PlatformAdmin, policy => policy
            .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new PlatformRoleRequirement(PlatformRoleNames.Admin)));

        options.AddPolicy(ContactsReader, policy => policy
            .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireAssertion(context => context.User.HasScope(OpenIdNames.ContactsScope)));
    }
}
