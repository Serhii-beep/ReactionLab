using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.Accounts;

internal static class IdentityCookieValidator
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<User>>();

        if (await signInManager.ValidateSecurityStampAsync(context.Principal) is null)
        {
            context.RejectPrincipal();

            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    }
}
